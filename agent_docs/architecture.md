# Decisões Técnicas — Feature 001: Cadastro de Usuário

Este documento traduz `specs/001-user-registration/spec.md` em decisões técnicas concretas, respeitando os limites de camada do `CLAUDE.md` e as convenções obrigatórias de `.claude/rules/backend-stand.md`. Ele existe para alimentar `/speckit.plan` e `/speckit.tasks` — não é um relatório de pesquisa, é a base de decisão da feature.

Fora de escopo (reforçando a spec): login/emissão de JWT, "esqueci minha senha", verificação de e-mail. Nada abaixo implementa esses fluxos.

Todas as pendências e riscos identificados durante a preparação deste documento foram revisados e confirmados pelo dono do produto em 2026-08-16 (ver seção 9 para o registro de cada decisão). Nenhum item permanece em aberto — este documento está pronto para alimentar `/speckit.plan`.

---

## 1. Modelagem de domínio

**Decisão**: `User` é uma entidade rica (não anêmica), com construtor privado e factory estático `User.Create(name, email, passwordHash)`. Não é modelada como `AggregateRoot` genérico com eventos de domínio — a spec não pede nada que justifique esse mecanismo (sem side effects, sem outros agregados reagindo à criação de conta). Introduzir uma base `AggregateRoot<TId>` agora seria abstração não exigida pela especificação atual.

Local: `FitProgress.Domain/Models/Users/User.cs`.

Propriedades: `Id` (`Guid`, gerado pelo próprio Domain via `Guid.NewGuid()` no `Create` — evita depender de `gen_random_uuid()`/extensão `pgcrypto` no Postgres), `Name` (VO `PersonName`), `Email` (VO `Email`), `PasswordHash` (VO `PasswordHash`), `CreatedAt` (`DateTimeOffset`, `UtcNow` atribuído no `Create`).

**Value Objects** (`FitProgress.Domain/ValueObjects/`):

- `PersonName` — não vazio após trim, tamanho máximo (ver lacuna na seção 9). Normaliza removendo espaços nas pontas (edge case da spec).
- `Email` — formato válido, trim, normalizado para lowercase no próprio construtor. A normalização para lowercase é o que garante que duas instâncias de `Email` com capitalização diferente sejam iguais — a igualdade de unicidade "de negócio" vive aqui; a garantia física contra corrida de concorrência vive no banco (seção 5).
- `Password` — **não é persistido**. Representa a senha em texto puro apenas durante a validação de política (8+ caracteres, maiúscula, minúscula, número, tamanho máximo contra input gigante — edge case da spec). É construído, validado e descartado dentro do fluxo do `UserService`; nunca chega ao repositório nem a logs.
- `PasswordHash` — wrapper opaco do hash já calculado (string não vazia). É o que de fato fica no `User` e é persistido. Não conhece o algoritmo de hashing.

Todos os VOs usam um padrão `TryCreate(valorBruto, out vo, out erro)` (não lança exceção para violação esperada de regra de negócio) — necessário porque a spec exige reportar **todas** as violações de uma vez, não a primeira encontrada (edge case explícito). Lançar exceção por VO impediria agregação de múltiplos erros sem um try/catch por campo. `User.Create` continua lançando `DomainException` se for chamado com VOs já inválidos — é uma checagem defensiva de invariante, não o caminho principal de validação de entrada.

**Invariantes no Domain**: formato de e-mail, normalização de e-mail, política de senha, nome não vazio/tamanho máximo, "um `User` sempre tem nome+email+hash válidos". **Invariantes na Application**: unicidade de e-mail (depende de estado externo/repositório, não é algo que um VO isolado consiga validar), orquestração/agregação de erros, coordenação entre hashing e persistência.

**Contracts** (`FitProgress.Domain/Contracts/V1/Users/`): `Requests/CreateUserRequest.cs` (Name, Email, Password como `string`) e `Responses/UserResponse.cs` (Id, Name, Email, CreatedAt — **nunca** hash ou senha). Contracts são tipos de borda HTTP; não reutilizar `User` como response.

---

## 2. Hashing de senha

**Decisão**: `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` (pacote `Microsoft.Extensions.Identity.Core`), usado de forma standalone — **sem** adotar o restante do ASP.NET Core Identity (sem `UserManager`, sem tabelas de Identity, sem cookies). `TUser` pode ser o próprio `FitProgress.Domain.Models.Users.User`; o hasher só usa o tipo genérico como parâmetro, não exige interfaces do Identity.

Por quê:
- PBKDF2 com parametrização e versionamento de formato corretos "de fábrica" — evita reimplementar iterações/salt manualmente (existe uma versão hand-rolled documentada na skill `dotnet-api` com PBKDF2 + 600k iterações, mas é código extra para manter sem necessidade).
- É um pacote Microsoft oficial, não uma dependência de terceiros (BCrypt.Net-Next, Konscious Argon2) — menor risco de manutenção para um pacote com esse escopo.
- `PasswordHasher<TUser>.VerifyHashedPassword` já retorna sinal de "precisa re-hash" (`SuccessRehashNeeded`), útil quando o fluxo de login for especificado — mas isso é forward-looking, não implementar agora.

Onde vive: a implementação concreta (`Infrastructure/Security/PasswordHasher.cs`, implementando `IPasswordHasher` — ver seção 9 sobre onde a interface é declarada) fica na Infrastructure, porque é a camada que conhece bibliotecas/algoritmos concretos. O Domain só enxerga a abstração e o VO `Password`/`PasswordHash`; nunca importa `Microsoft.AspNetCore.Identity`.

Alternativa avaliada e descartada por ora: BCrypt.Net-Next (API mais simples, mas terceiro) e Argon2 via Konscious (mais resistente a GPU, mas exige gerenciar parâmetros de memória/paralelismo manualmente). Se no futuro houver requisito específico de resistência a ataques de hardware dedicado, Argon2id é o caminho — trocar a implementação por trás de `IPasswordHasher` sem tocar Domain/Application.

---

## 3. Validação

**Decisão**: validação manual, sem FluentValidation nem DataAnnotations como mecanismo primário — os VOs do Domain (seção 1) fazem a validação de formato/regra e retornam erro via `TryCreate`; a Application agrega os resultados dos três campos e decide se segue ou retorna falha.

Por quê não FluentValidation: são apenas 3 campos com regras simples (nome, e-mail, senha) — adicionar uma dependência nova para isso vai contra "evite abstrações e dependências não exigidas pela especificação atual" do `CLAUDE.md`. Se o projeto crescer (nutrição, fichas de treino, etc.) e a validação começar a ter regras cruzadas/condicionais complexas, FluentValidation volta a ser candidato natural — mas não agora.

Por quê não só DataAnnotations no Contract: DataAnnotations valida o shape do request (`[Required]`, `[EmailAddress]`) mas não consegue expressar a política de senha (maiúscula+minúscula+número) de forma limpa nem a normalização (trim/lowercase) — isso é regra de domínio, não anotação de DTO. Usar DataAnnotations *só* para os "for free" checks (campo ausente no JSON) é opcional e redundante com o que os VOs já cobrem; não recomendo empilhar as duas abordagens para os mesmos 3 campos.

Fluxo de agregação de erros: `UserService.CreateUserAsync` chama `PersonName.TryCreate`, `Email.TryCreate`, `Password.TryCreate` — nenhum deles lança exceção nesse ponto — acumula os erros dos três em uma lista de `(Campo, Mensagem)`. Se a lista não estiver vazia, retorna imediatamente um resultado de "validação falhou" com **todas** as violações (atende ao edge case da spec). Só então segue para hashing + verificação de unicidade + persistência.

Como as mensagens chegam à API: o `UserService` não lança exceção para esse caminho esperado (ver seção 4 — padrão de resultado). O Controller traduz o resultado de validação em `400 Bad Request` com um corpo listando campo + mensagem por violação (formato compatível com `ValidationProblemDetails` do ASP.NET Core, para não inventar um formato de erro próprio).

---

## 4. Camada Application

**Decisão**: serviço de aplicação simples (`IUserService`/`UserService`), **não** command/handler (Mediator/Wolverine) — a spec tem um único caso de uso ("criar conta"), e a estrutura de `backend-stand.md` já define o padrão Service+Repository como convenção do projeto (`Application/IService/`, `Application/Services/`). Introduzir um pipeline de mediator para um único handler seria complexidade não exigida.

Local: `FitProgress.Application/IService/Users/IUserService.cs` e `Application/Services/Users/UserService.cs`.

Assinatura: `Task<CreateUserResult> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken)`. Reutiliza o próprio Contract (`CreateUserRequest`, `UserResponse` — Domain/Contracts) como entrada/saída da Application, em vez de criar um "Command" paralelo — os dois tipos já vivem em Domain e a Application pode depender de Domain; duplicar em um Command redundante é abstração desnecessária para este caso de uso único.

**Padrão de resultado, não exceção, para fluxos esperados**: `CreateUserResult` é um pequeno tipo de resultado com três estados possíveis — `Success(UserResponse)`, `ValidationFailed(IReadOnlyList<(string Campo, string Mensagem)>)`, `EmailAlreadyInUse`. Validação inválida e e-mail duplicado são **desfechos de negócio esperados** (a própria spec os define como cenários de aceite), não bugs — não devem custar uma exceção + stack trace. Exceções continuam reservadas para falhas inesperadas de infraestrutura (regra 12 do `backend-stand.md`: try/catch + `throw;`, nunca engolir). Isso é uma pequena adição estrutural sobre o que `backend-stand.md` documenta explicitamente — proponho `Application/Results/CreateUserResult.cs` como local (pasta nova, mas dentro da Application, não viola nenhuma regra existente).

Dependências do `UserService` via construtor (DI, nunca `new`): `IUserRepository`, `IPasswordHasher`, `ILogger<UserService>`. Ver seção 9 sobre **onde** essas duas interfaces são declaradas — é o ponto crítico deste documento.

Fluxo do `CreateUserAsync`:
1. Trim de `Name`/`Email` do request (edge case: espaços nas pontas).
2. `PersonName.TryCreate`, `Email.TryCreate`, `Password.TryCreate` — acumula erros.
3. Se houver erros → retorna `ValidationFailed` com a lista completa.
4. `IUserRepository.ExistsByEmailAsync(email, ct)` — checagem otimista para dar feedback rápido (`EmailAlreadyInUse`) sem tocar hashing/transação à toa.
5. `IPasswordHasher.Hash(password.Value)` → `PasswordHash`.
6. `User.Create(name, email, passwordHash)`.
7. `IUserRepository.AddAsync(user, ct)` — internamente pode sinalizar conflito (constraint única) mesmo após o passo 4 ter passado, por causa de corrida entre duas requisições simultâneas com o mesmo e-mail (TOCTOU). Repository comunica esse conflito de volta (ver seção 5) e o Service traduz para `EmailAlreadyInUse` também aqui — o passo 4 é otimização de UX, não a garantia real.
8. Sucesso → mapeia `User` para `UserResponse`, retorna `Success`.

Try/catch: envolve os passos de I/O (repositório) conforme regra 12 — captura, loga (**nunca** logar `request.Password` ou o hash), `throw;`. Erros inesperados sobem para a Api, que precisa de um exception handler global (seção 6) para não vazar detalhes internos.

---

## 5. Camada Infrastructure

**Tabela `users` (PostgreSQL / Neon)**:

```sql
CREATE TABLE users (
    id            UUID PRIMARY KEY,
    name          VARCHAR(200) NOT NULL,
    email         VARCHAR(320) NOT NULL,
    password_hash TEXT NOT NULL,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email));
```

Decisões dentro da tabela:
- `id UUID` gerado na aplicação (`User.Create`), não `DEFAULT gen_random_uuid()` — evita depender da extensão `pgcrypto` estar habilitada no banco Neon; o Domain já garante um identificador antes de qualquer INSERT.
- Unicidade case-insensitive via **índice único funcional** `lower(email)`, não `citext`. `citext` exigiria habilitar uma extensão no banco; o índice funcional não exige extensão nenhuma e é portátil. Isso é a garantia física contra duplicidade (FR-004) — o VO `Email` normaliza para lowercase antes de persistir, então na prática os valores em `email` já chegam normalizados, mas o índice é a fonte de verdade contra corrida de concorrência e qualquer inserção fora do caminho normal.
- `email VARCHAR(320)`: limite teórico de RFC 5321 para endereços de e-mail.
- `password_hash TEXT`: o formato do `PasswordHasher<TUser>` é Base64 de tamanho variável conforme versão do algoritmo; `TEXT` evita truncar por engano se o formato mudar.
- Sem `updated_at`: não há caso de uso de atualização de usuário nesta spec — não adicionar coluna especulativa.

**Repository**: `IUserRepository` com dois métodos, o mínimo que a spec pede — não introduzir `GetByIdAsync`/`UpdateAsync` especulativos:
- `Task<bool> ExistsByEmailAsync(Email email, CancellationToken ct)`
- `Task<bool> AddAsync(User user, CancellationToken ct)` — retorna `false` (não lança) especificamente quando a inserção falha por violação da constraint única (`PostgresException.SqlState == "23505"`), porque essa é uma condição de negócio esperada (corrida de e-mail duplicado), não uma falha de infraestrutura. Qualquer outra exceção continua sendo logada e relançada (`throw;`), conforme regra 12.

Implementação (`Infrastructure/Repositories/Users/UserRepository.cs`) segue à risca as regras 13/14 de `backend-stand.md`: conexão via `ConnectionFactory` (`Infrastructure/Database/ConnectionFactory.cs`, wrapper fino sobre `NpgsqlDataSource` registrado via `AddNpgsqlDataSource` — isso evita `new NpgsqlConnection()` manual espalhado pelo código, mantendo o pooling gerenciado pelo Npgsql, e ainda expõe a assinatura `CreateOpenConnectionAsync(cancellationToken)` que o exemplo de `backend-stand.md` já usa), `BeginTransaction`/`Commit`/`Rollback` explícitos no INSERT, `CommandDefinition` com `CancellationToken`, SQL parametrizado.

```sql
-- INSERT (dentro de transação explícita)
INSERT INTO users (id, name, email, password_hash, created_at)
VALUES (@Id, @Name, @Email, @PasswordHash, @CreatedAt);
```

**Migração de schema**: `backend-stand.md` já reserva a pasta `Infrastructure/Database/Scripts/` para scripts SQL, mas `CLAUDE.md` não define ferramenta. Recomendo **DbUp** (`dbup-postgresql`) executando scripts SQL versionados (`0001_create_users_table.sql`, ...) embutidos como recursos no assembly, aplicados no início do processo (ver seção 6) contra uma tabela de controle própria (`SchemaVersions`, criada pelo próprio DbUp). Justificativa: o deploy é container/Vercel sem acesso manual ao servidor e sem storage persistente — alguma automação de schema é necessidade real, não infraestrutura por capricho. DbUp é a opção mais leve das listadas nas referências técnicas (mais simples que FluentMigrator, que é orientado a classes C# fluentes; e não traz EF Core, que contradiz a decisão de Dapper). Alternativa mais simples (rodar o `.sql` manualmente contra o Neon a cada mudança) fica descartada por não ser reproduzível em CI/outro ambiente — mas está registrada como opção caso o time prefira zero dependência nova agora (ver seção 9).

---

## 6. Camada Api

**Decisão**: Controllers (`ControllerBase`), não Minimal API — é o padrão explícito de `backend-stand.md` (`UsersController`) e da seção "15. Controllers". O `Program.cs` atual do scaffold usa Minimal API (endpoint `/weatherforecast` de exemplo) e **precisa ser ajustado** como parte da implementação desta feature: adicionar `builder.Services.AddControllers()` / `app.MapControllers()`, e remover o endpoint de exemplo — isso é trabalho de setup, não desta decisão de arquitetura, mas fica registrado para não ser esquecido em `/speckit.tasks`.

Local: `FitProgress.Api/Controllers/V1/UsersController.cs`.

Rota: `POST /api/v1/users` (versionamento explícito por convenção de `backend-stand.md`, alinhado com `Domain/Contracts/V1/`).

Request/Response: `CreateUserRequest` / `UserResponse` (seção 1) — Controller não faz mapeamento de negócio, só recebe o request, chama `IUserService.CreateUserAsync`, e traduz o `CreateUserResult` em `IActionResult`:

| Resultado do Service | HTTP | Corpo |
|---|---|---|
| `Success` | 201 Created | `UserResponse` |
| `ValidationFailed` | 400 Bad Request | lista de campo+mensagem (`ValidationProblemDetails`) |
| `EmailAlreadyInUse` | 409 Conflict | `ProblemDetails` com mensagem clara ("e-mail já está em uso") |
| exceção não tratada | 500 Internal Server Error | `ProblemDetails` genérico, sem detalhes internos |

Sobre 201 e `Location`: não há endpoint `GET /api/v1/users/{id}` nesta spec (fora de escopo), então não há para onde apontar um header `Location` real ainda. Retornar `201 Created` com o corpo é suficiente agora; adicionar `Location` fica pendente para quando existir uma rota de consulta.

O erro 500 genérico exige um exception handler global (`IExceptionHandler`, ASP.NET Core 8+) registrado uma vez no `Program.cs` — não é infraestrutura nova "não pedida": é a forma padrão de cumprir FR-007 ("mensagens de erro compreensíveis") sem cada Controller reimplementar tratamento de exceção, e sem vazar stack trace/detalhes internos por acidente.

---

## 7. Pacotes NuGet por camada

| Camada | Pacote | Motivo |
|---|---|---|
| Domain | — | nenhum pacote externo; só BCL |
| Application | — | nenhum pacote externo; só BCL (ver seção 3 sobre não usar FluentValidation agora) |
| Infrastructure | `Dapper` | acesso a dados oficial do projeto |
| Infrastructure | `Npgsql` | driver PostgreSQL |
| Infrastructure | `Npgsql.DependencyInjection` | registra `NpgsqlDataSource` via `AddNpgsqlDataSource`, pooling gerenciado |
| Infrastructure | `Microsoft.Extensions.Identity.Core` | `PasswordHasher<TUser>` (seção 2) |
| Infrastructure | `dbup-postgresql` | migração de schema versionada (seção 5) |
| Api | *(nenhum novo)* | `Microsoft.AspNetCore.OpenApi` já presente; Controllers vêm do `Microsoft.NET.Sdk.Web` sem pacote extra |
| tests/FitProgress.UnitTests | *(nenhum novo)* | xUnit já presente é suficiente para os testes desta feature |
| tests/FitProgress.IntegrationTests | `Microsoft.AspNetCore.Mvc.Testing` | necessário para `WebApplicationFactory<Program>` |
| tests/FitProgress.IntegrationTests | `Testcontainers.PostgreSql` *(proposto, ver seção 9)* | Postgres real e isolado para validar a constraint única de e-mail sem depender do Neon de produção/dev |

---

## 8. Estratégia de testes

**Unitários obrigatórios (`tests/FitProgress.UnitTests/Domain/Users/`)** — toda regra de domínio nova precisa de teste (regra 23 de `backend-stand.md`):

- `PersonName`: rejeita vazio/só espaços; rejeita acima do tamanho máximo; aceita nome válido; remove espaços nas pontas.
- `Email`: rejeita formato inválido; aceita formato válido; normaliza para lowercase; dois `Email` com capitalização diferente são iguais entre si; remove espaços nas pontas.
- `Password`: rejeita menor que 8 caracteres; rejeita sem maiúscula; rejeita sem minúscula; rejeita sem dígito; rejeita acima do tamanho máximo (edge case de input gigante); aceita senha que cumpre a política.
- `User.Create`: cria instância válida a partir de VOs válidos; lança `DomainException` se chamado defensivamente com estado inconsistente (teste de invariante, não o caminho principal de validação).

**Unitários da Application (`tests/FitProgress.UnitTests/Application/Services/Users/`)**, com `IUserRepository`/`IPasswordHasher` fake/mockado (sem Postgres, sem `PasswordHasher<TUser>` real — regra: testes unitários não dependem de infraestrutura real):

- Dados válidos e e-mail inédito → `Success`, repositório chamado com hash (nunca com a senha em texto puro).
- Múltiplos campos inválidos simultaneamente → `ValidationFailed` contendo **todas** as violações, não só a primeira (cobre o edge case da spec diretamente).
- E-mail já existente (via `ExistsByEmailAsync` = true) → `EmailAlreadyInUse`, sem chamar `AddAsync`.
- Corrida de e-mail duplicado (via `AddAsync` retornando `false` mesmo após `ExistsByEmailAsync` = false) → `EmailAlreadyInUse`.

**Integração (`tests/FitProgress.IntegrationTests/`)** — justificado porque a garantia real de unicidade e o comportamento de hashing só são verificáveis fim a fim, contra HTTP e banco reais:

- `POST /api/v1/users` com payload válido → 201, corpo no formato de `UserResponse`, e consulta direta ao banco confirma que `password_hash` não é igual à senha enviada (nunca texto puro — SC-002).
- Duas tentativas de cadastro com o mesmo e-mail (incluindo capitalização diferente) → segunda tentativa retorna 409 com mensagem clara (valida a constraint `ux_users_email_lower` de fato, não uma simulação).
- Payload com nome vazio + e-mail inválido + senha fraca ao mesmo tempo → 400 com as três violações reportadas.

---

## 9. Pendências e riscos — decisões confirmadas em 2026-08-16

### 9.1. Conflito estrutural crítico: onde vivem `IUserRepository` e `IPasswordHasher`

**Confirmado**: interfaces em `FitProgress.Domain/Abstractions/`, implementações na Infrastructure, conforme proposto abaixo.

`backend-stand.md` diz textualmente: *"Interfaces de Repository ficam exclusivamente em `Infrastructure/IRepositories`"* e mostra `UserService(IUserRepository userRepository, ...)`, ou seja, a Application dependeria de um tipo declarado na Infrastructure.

Isso **não compila** com os `.csproj` já existentes no repositório:

```
FitProgress.Application.csproj  → referencia apenas FitProgress.Domain
FitProgress.Infrastructure.csproj → referencia FitProgress.Domain e FitProgress.Application
```

Ou seja, a Infrastructure depende da Application (não o contrário). Se `IUserRepository` for declarado na Infrastructure, a Application não consegue enxergar o tipo para o construtor de `UserService` — e inverter a referência (Application → Infrastructure) criaria dependência circular (a Infrastructure já referencia a Application), além de violar o próprio `CLAUDE.md`: *"`Application` pode depender de `Domain`"* (só Domain, nada mais).

**Decisão tomada neste documento**: declarar `IUserRepository` e `IPasswordHasher` no **Domain**, em `FitProgress.Domain/Abstractions/` (pasta nova). A implementação concreta continua na Infrastructure (`Infrastructure/Repositories/Users/UserRepository.cs`, `Infrastructure/Security/PasswordHasher.cs`), exatamente como `backend-stand.md` pede para as implementações. Isso é o padrão clássico de Dependency Inversion Principle (contrato na camada interna, implementação na camada externa) e é literalmente o que a skill `domain-modeling.md` recomenda ("Repository interfaces belong in the domain layer") — só diverge do texto de `backend-stand.md` quanto à *pasta da interface*, não quanto à pasta da implementação.

É o padrão clássico de Dependency Inversion Principle (contrato na camada interna, implementação na camada externa) — diverge só da *pasta* sugerida em `backend-stand.md` para a interface, não da regra de onde vive a implementação. `backend-stand.md` deve ser atualizado para refletir essa divergência (vale para toda feature futura que precisar de repositório).

### 9.2. Números que a spec não define

- **Tamanho máximo de `Name`**: **Confirmado — 200 caracteres.**
- **Tamanho máximo de `Password`**: **Confirmado — 100 caracteres.**

### 9.3. Migração de schema — DbUp confirmado

**Confirmado**: DbUp (`dbup-postgresql`), conforme seção 5 — opção mais leve compatível com Dapper e com deploy em container sem acesso manual ao servidor.

### 9.4. Testcontainers para integração — confirmado

**Confirmado**: `Testcontainers.PostgreSql` para os testes de integração que dependem de comportamento real do Postgres (constraint única). Docker precisa estar disponível no ambiente de CI/execução dos testes.

### 9.5. Ajuste necessário no `Program.cs` do scaffold

Fora do escopo de "decisão de arquitetura", mas necessário para a feature funcionar: trocar o scaffold de Minimal API (`MapGet("/weatherforecast", ...)`) por `AddControllers()`/`MapControllers()`, e remover o endpoint de exemplo. Incluir como tarefa explícita em `/speckit.tasks`.
