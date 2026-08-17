---
paths:
  - "src/**/*.cs"
  - "tests/**/*.cs"
---

# Backend C# Rules

Estas regras são obrigatórias ao criar, mover ou alterar código C# no projeto FitProgress.

# 1. Estrutura oficial do projeto

Respeite sempre a camada e a pasta responsáveis pelo arquivo.

Não criar arquivos diretamente na raiz de uma camada quando existir uma pasta específica para sua responsabilidade.

```text
src/
├── FitProgress.Api/
│   ├── Controllers/
│   │   └── V1/
│   │       └── UsersController.cs
│   └── DependencyInjection/
│       └── ApiDependencyInjection.cs
│
├── FitProgress.Application/
│   ├── IService/
│   │   └── Users/
│   │       └── IUserService.cs
│   ├── Services/
│   │   └── Users/
│   │       └── UserService.cs
│   ├── HttpRequest/
│   │   └── ExternalProviders/
│   │       ├── IExternalApiClient.cs
│   │       └── ExternalApiClient.cs
│   ├── Helpers/
│   │   ├── ValidationHelper.cs
│   │   └── ConversionHelper.cs
│   └── DependencyInjection/
│       └── ApplicationDependencyInjection.cs
│
├── FitProgress.Domain/
│   ├── Models/
│   │   ├── Users/
│   │   │   └── User.cs
│   │   ├── Workouts/
│   │   │   └── Workout.cs
│   │   └── Exercises/
│   │       └── Exercise.cs
│   ├── Contracts/
│   │   └── V1/
│   │       └── Users/
│   │           ├── Requests/
│   │           │   └── CreateUserRequest.cs
│   │           └── Responses/
│   │               └── UserResponse.cs
│   ├── ValueObjects/
│   ├── Abstractions/
│   │   └── Users/
│   │       └── IUserRepository.cs
│   └── Enums/
│
└── FitProgress.Infrastructure/
    ├── Database/
    │   ├── ConnectionFactory.cs
    │   └── Scripts/
    ├── Repositories/
    │   └── Users/
    │       └── UserRepository.cs
    └── DependencyInjection/
        └── InfrastructureDependencyInjection.cs
```

# 2. Regras de localização de arquivos

## API

A camada `FitProgress.Api` contém somente responsabilidades relacionadas à exposição HTTP da aplicação.

Permitido:

- Controllers.
- configuração da API;
- autenticação/autorização;
- versionamento;
- Dependency Injection;
- middlewares e filtros relacionados à borda HTTP.

Não colocar na API:

- Models de domínio;
- Requests e Responses;
- Services;
- Repositories;
- interfaces de Repository;
- Dapper;
- SQL;
- regras de negócio.

Controllers da versão 1:

```text
FitProgress.Api/Controllers/V1/
```

## Application

A camada `FitProgress.Application` contém orquestração dos casos de uso.

Estrutura obrigatória:

```text
FitProgress.Application/
├── IService/
├── Services/
├── HttpRequest/
├── Helpers/
└── DependencyInjection/
```

Não criar Repository ou interface de Repository nesta camada.

## Domain

Entidades e modelos ficam exclusivamente em:

```text
FitProgress.Domain/Models/<Dominio>/
```

Exemplo:

```text
FitProgress.Domain/Models/Users/User.cs
FitProgress.Domain/Models/Workouts/Workout.cs
FitProgress.Domain/Models/Exercises/Exercise.cs
```

Nunca criar modelos diretamente em:

```text
FitProgress.Domain/Models/
```

Cada contexto deve possuir sua própria pasta.

Contratos da API ficam exclusivamente em:

```text
FitProgress.Domain/Contracts/V{N}/<Dominio>/
```

Requests:

```text
FitProgress.Domain/Contracts/V1/Users/Requests/
```

Responses:

```text
FitProgress.Domain/Contracts/V1/Users/Responses/
```

## Infrastructure

> **Exceção documentada (feature `001-user-registration`)**: com os `.csproj` atuais do projeto, `FitProgress.Application` referencia apenas `FitProgress.Domain`, e `FitProgress.Infrastructure` referencia `FitProgress.Application` (não o inverso). Declarar a interface de Repository em `Infrastructure/IRepositories` torna o tipo inacessível para Services na Application, o que não compila. Por isso, interfaces de Repository (e outras abstrações que a Application precisa consumir, como `IPasswordHasher`) ficam em `FitProgress.Domain/Abstractions/<Dominio ou Cross-Cutting>/` — a implementação continua exclusivamente na Infrastructure, como descrito abaixo. Ver `specs/001-user-registration/research.md` (item 4) e `specs/001-user-registration/plan.md` (Constitution Check) para o registro completo da decisão.

Interfaces de Repository ficam em:

```text
FitProgress.Domain/Abstractions/<Dominio>/
```

Implementações ficam exclusivamente em:

```text
FitProgress.Infrastructure/Repositories/<Dominio>/
```

Acesso a banco, Dapper, SQL, conexões e transações pertencem à Infrastructure.

# 3. Convenções de nomes

O código deve utilizar nomes em inglês.

Exemplos:

```text
User.cs
IUserService.cs
UserService.cs
IUserRepository.cs
UserRepository.cs
CreateUserRequest.cs
UpdateUserRequest.cs
UserResponse.cs
ValidationHelper.cs
ConversionHelper.cs
```

Regras:

- interfaces começam com `I`;
- Services terminam com `Service`;
- Repositories terminam com `Repository`;
- Requests terminam com `Request`;
- Responses terminam com `Response`;
- Helpers terminam com `Helper`;
- métodos assíncronos terminam com `Async`;
- uma classe pública por arquivo;
- nome do arquivo deve ser igual ao nome da classe/interface pública;
- namespace deve acompanhar o caminho lógico da pasta.

# 4. IService e Services

Toda implementação de Service deve possuir uma interface correspondente.

Interface:

```text
Application/IService/<Dominio>/I<Nome>Service.cs
```

Implementação:

```text
Application/Services/<Dominio>/<Nome>Service.cs
```

Exemplo:

```text
Application/
├── IService/
│   └── Users/
│       └── IUserService.cs
└── Services/
    └── Users/
        └── UserService.cs
```

Nunca criar `UserService` sem `IUserService`.

Controllers devem depender da interface.

```csharp
public sealed class UsersController(
    IUserService userService) : ControllerBase
{
    private readonly IUserService _userService = userService;
}
```

Services podem:

- coordenar regras da aplicação;
- chamar Repositories;
- chamar integrações externas;
- utilizar Helpers;
- coordenar fluxo entre componentes.

Services não podem:

- conter SQL;
- usar Dapper diretamente;
- abrir conexão com PostgreSQL;
- abrir transação de banco;
- conter código específico de Controller.

# 5. Abstractions (Domain) e Repositories (Infrastructure)

A interface fica no `Domain`, em `Abstractions/<Dominio>/`; a implementação fica na `Infrastructure`, em `Repositories/<Dominio>/`. Isso diverge do padrão "tudo na Infrastructure" por necessidade de compilação: ver a nota no início da seção "Infrastructure" acima.

```text
FitProgress.Domain/
└── Abstractions/
    └── Users/
        └── IUserRepository.cs

FitProgress.Infrastructure/
└── Repositories/
    └── Users/
        └── UserRepository.cs
```

Regras:

- interface fica em `Domain/Abstractions/<Dominio>`;
- implementação fica em `Infrastructure/Repositories/<Dominio>`;
- nunca colocar Repository na Application;
- nunca colocar interface de Repository na Application;
- Service depende de `IUserRepository`;
- Service nunca depende diretamente de `UserRepository`;
- Repository contém somente persistência e consultas;
- regra de negócio não pertence ao Repository.

# 6. HttpRequest

Toda integração HTTP externa deve ficar em:

```text
FitProgress.Application/HttpRequest/<ProvedorOuContexto>/
```

Exemplo:

```text
Application/
└── HttpRequest/
    └── NutritionProvider/
        ├── INutritionApiClient.cs
        └── NutritionApiClient.cs
```

Regras:

- não realizar chamadas HTTP externas diretamente em Controllers;
- não realizar chamadas HTTP externas diretamente em Repositories;
- integrações devem possuir pasta específica por provedor ou contexto;
- utilizar injeção de dependência;
- preferir `HttpClientFactory`;
- utilizar `async/await`;
- propagar `CancellationToken`;
- validar status HTTP antes de considerar uma chamada bem-sucedida;
- URLs, tokens e secrets devem vir de configuração/variáveis de ambiente;
- não armazenar secrets no código;
- tratar exceções com `try/catch`;
- não engolir exceções.

Modelos específicos de uma integração externa podem permanecer dentro da pasta da integração quando não fizerem parte do contrato público da API.

# 7. Helpers

Métodos auxiliares reutilizáveis devem ficar em:

```text
FitProgress.Application/Helpers/
```

Exemplos:

```text
ValidationHelper.cs
ConversionHelper.cs
DateHelper.cs
StringHelper.cs
```

Helpers são apropriados para:

- validações auxiliares;
- conversões;
- parsing;
- normalização;
- formatação;
- métodos isolados e reutilizáveis.

Helpers não podem:

- conter regra de negócio principal;
- acessar Repository;
- acessar Dapper;
- executar SQL;
- abrir transações;
- executar chamadas HTTP externas;
- funcionar como depósito genérico de código sem responsabilidade definida.

Preferir métodos puros e sem estado.

Não criar Helpers antecipadamente sem necessidade real.

Se um Helper crescer, passar a possuir dependências ou representar um caso de uso, movê-lo para um Service apropriado.

# 8. Dependency Injection

Toda classe que dependa de outro componente deve receber essa dependência por injeção de dependência via construtor.

Aplica-se principalmente a:

- Controllers;
- Services;
- Repositories;
- HttpRequest clients;
- componentes de infraestrutura;
- componentes auxiliares com dependências.

Nunca instanciar manualmente dependências de aplicação usando `new` dentro de Controllers, Services ou Repositories.

Exemplo:

```csharp
public sealed class UserService(
    IUserRepository userRepository,
    ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<UserService> _logger = logger;
}
```

Ao criar uma implementação injetável:

1. criar ou utilizar sua interface;
2. receber dependências pelo construtor;
3. registrar implementação no container;
4. consumir abstração nas camadas superiores.

Exemplo:

```csharp
services.AddScoped<IUserService, UserService>();
services.AddScoped<IUserRepository, UserRepository>();
```

Registros devem ficar em arquivos de extensão dentro das pastas:

```text
Api/DependencyInjection/
Application/DependencyInjection/
Infrastructure/DependencyInjection/
```

Models, Requests, Responses, DTOs simples e Value Objects não devem ser registrados no container.

# 9. API Versioning

Toda rota pública deve possuir versão explícita.

Versão inicial:

```text
/api/v1/users
/api/v1/workouts
/api/v1/exercises
```

Controllers:

```text
FitProgress.Api/Controllers/V1/
```

Contratos:

```text
FitProgress.Domain/Contracts/V1/
```

Regras:

- alterações compatíveis permanecem na versão atual;
- breaking changes exigem nova versão;
- não misturar contratos entre versões;
- não criar `V2` antecipadamente sem requisito;
- versão da rota e versão dos Contracts devem permanecer alinhadas.

# 10. Async/Await obrigatório

Operações de I/O devem ser assíncronas.

Utilizar `async/await` em:

- banco de dados;
- Dapper;
- HTTP;
- filesystem;
- integrações externas;
- Services que dependam dessas operações;
- Repositories.

Não utilizar:

```csharp
.Result
.Wait()
.GetAwaiter().GetResult()
```

Métodos assíncronos devem terminar com `Async`.

Exemplo:

```csharp
Task<User?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken);
```

# 11. CancellationToken obrigatório

Todo método assíncrono de:

- Controller;
- Service;
- Repository;
- HttpRequest;

deve receber `CancellationToken`.

O `CancellationToken` deve ser sempre o último parâmetro.

Exemplo:

```csharp
public async Task<User?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken)
```

Propagação obrigatória:

```text
Controller
   ↓
Service
   ↓
Repository / HttpRequest
   ↓
Dapper / HttpClient
```

Nunca substituir um token recebido por:

```csharp
CancellationToken.None
```

No Dapper, utilizar `CommandDefinition` para propagar o token.

```csharp
var command = new CommandDefinition(
    commandText: sql,
    parameters: parameters,
    transaction: transaction,
    cancellationToken: cancellationToken);

await connection.ExecuteAsync(command);
```

# 12. Try/Catch obrigatório

Métodos públicos assíncronos de:

- Services;
- Repositories;
- HttpRequest;

devem utilizar `try/catch`.

Regras:

- nunca deixar `catch` vazio;
- nunca engolir exceção;
- não retornar sucesso após falha;
- ao registrar e propagar, usar `throw;`;
- nunca usar `throw ex;`;
- não expor secrets, tokens, senhas ou connection strings em logs.

Exemplo:

```csharp
try
{
    // operação
}
catch (Exception ex)
{
    logger.LogError(
        ex,
        "Erro ao executar operação.");

    throw;
}
```

# 13. Transações nos Repositories

Toda operação de escrita deve possuir transação explícita.

Aplica-se a:

- `INSERT`;
- `UPDATE`;
- `DELETE`;
- múltiplas operações de persistência;
- fluxos compostos de escrita.

Fluxo obrigatório:

```text
Open Connection
      ↓
BeginTransaction
      ↓
Execute
      ↓
Commit
```

Em caso de erro:

```text
catch
  ↓
Rollback
  ↓
Log
  ↓
throw
```

Exemplo:

```csharp
await using var connection =
    await connectionFactory.CreateOpenConnectionAsync(
        cancellationToken);

await using var transaction =
    await connection.BeginTransactionAsync(
        cancellationToken);

try
{
    var command = new CommandDefinition(
        commandText: sql,
        parameters: parameters,
        transaction: transaction,
        cancellationToken: cancellationToken);

    await connection.ExecuteAsync(command);

    await transaction.CommitAsync(
        cancellationToken);
}
catch (Exception ex)
{
    await transaction.RollbackAsync(
        cancellationToken);

    logger.LogError(
        ex,
        "Erro ao persistir dados.");

    throw;
}
```

Regras:

- nunca executar escrita fora da transação iniciada para o fluxo;
- `Commit` somente após todas as operações concluírem com sucesso;
- `Rollback` obrigatório no `catch`;
- consultas simples somente leitura não precisam abrir transação explícita;
- uma regra específica da feature pode exigir transação em leitura quando houver necessidade de consistência transacional.

# 14. Dapper

Dapper é a biblioteca oficial de acesso a dados.

Regras:

- Dapper pertence à Infrastructure;
- SQL pertence à Infrastructure;
- sempre utilizar parâmetros;
- nunca concatenar entrada externa diretamente no SQL;
- utilizar `CommandDefinition` quando houver `CancellationToken`;
- reutilizar conexão e transação durante um fluxo transacional;
- não retornar tipos internos do Dapper para camadas superiores.

Proibido:

```csharp
var sql =
    $"SELECT * FROM users WHERE email = '{email}'";
```

Correto:

```csharp
const string sql = """
    SELECT id, name, email
    FROM users
    WHERE email = @Email;
    """;

var command = new CommandDefinition(
    commandText: sql,
    parameters: new { Email = email },
    cancellationToken: cancellationToken);

return await connection
    .QuerySingleOrDefaultAsync<User>(command);
```

# 15. Controllers

Controllers devem ser finos.

Controllers podem:

- receber request HTTP;
- receber `CancellationToken`;
- chamar `IService`;
- retornar resposta HTTP.

Controllers não podem:

- conter SQL;
- utilizar Dapper;
- acessar banco diretamente;
- abrir transações;
- implementar regra de negócio;
- acessar Repository diretamente;
- criar Service com `new`;
- criar Repository com `new`;
- realizar integração HTTP externa diretamente.

# 16. Models e Contracts

## Models

Models representam entidades ou estruturas do domínio.

Local:

```text
Domain/Models/<Dominio>/
```

Exemplo:

```text
Domain/Models/Users/User.cs
```

## Contracts

Contracts representam entrada e saída da API.

Local:

```text
Domain/Contracts/V{N}/<Dominio>/
```

Exemplo:

```text
Domain/Contracts/V1/Users/
├── Requests/
│   └── CreateUserRequest.cs
└── Responses/
    └── UserResponse.cs
```

Não utilizar a mesma classe como entidade de domínio e contrato HTTP quando as responsabilidades forem diferentes.

# 17. Testes

Toda regra de domínio nova ou alterada deve possuir teste unitário.

Estrutura:

```text
tests/
└── FitProgress.UnitTests/
    ├── Domain/
    │   └── Users/
    └── Application/
        └── Services/
            └── Users/
```

Padrão recomendado para nomes:

```text
Metodo_Cenario_ResultadoEsperado
```

Exemplo:

```text
CreateUser_EmailInvalido_DeveRetornarErro
```

Testes unitários não devem depender de:

- Neon;
- internet;
- API externa;
- infraestrutura real.

# 18. Regras arquiteturais obrigatórias

- `Domain` não referencia `Application`, `Infrastructure` ou `Api`.
- `Application` não contém Repository.
- `Application` não contém interface de Repository.
- Interfaces de Service pertencem a `Application/IService`.
- Implementações de Service pertencem a `Application/Services`.
- Integrações HTTP externas pertencem a `Application/HttpRequest`.
- Helpers pertencem a `Application/Helpers`.
- Interfaces de Repository pertencem a `Domain/Abstractions/<Dominio>` (exceção documentada — ver seção "Infrastructure").
- Implementações de Repository pertencem a `Infrastructure/Repositories`.
- Models pertencem a `Domain/Models/<Dominio>`.
- Contracts pertencem a `Domain/Contracts/V{N}/<Dominio>`.
- API não armazena Models nem Contracts.
- Controllers dependem de Services.
- Services podem depender de Repositories por interface.
- SQL e Dapper nunca saem da Infrastructure.
- Dependências concretas não devem ser instanciadas manualmente quando pertencem ao container de DI.

# 19. Checklist obrigatório ao criar uma feature

Antes de considerar uma feature concluída, validar:

1. Arquivos estão na camada correta.
2. Models estão em `FitProgress.Domain/Models/<Dominio>/`.
3. Requests estão em `FitProgress.Domain/Contracts/V{N}/<Dominio>/Requests/`.
4. Responses estão em `FitProgress.Domain/Contracts/V{N}/<Dominio>/Responses/`.
5. Interface de Service está em `FitProgress.Application/IService/<Dominio>/`.
6. Service está em `FitProgress.Application/Services/<Dominio>/`.
7. Integrações externas estão em `FitProgress.Application/HttpRequest/<ProvedorOuContexto>/`.
8. Helpers estão em `FitProgress.Application/Helpers/`.
9. Interface de Repository está em `FitProgress.Domain/Abstractions/<Dominio>/`.
10. Repository está em `FitProgress.Infrastructure/Repositories/<Dominio>/`.
11. Implementações injetáveis estão registradas na DI.
12. Nenhuma dependência de aplicação foi instanciada manualmente.
13. Operações de I/O usam `async/await`.
14. Métodos assíncronos terminam com `Async`.
15. Métodos assíncronos recebem e propagam `CancellationToken`.
16. Services, Repositories e HttpRequest possuem `try/catch`.
17. Escritas utilizam `BeginTransaction`.
18. Escritas executam `Commit` somente em sucesso.
19. `catch` de persistência executa `Rollback`.
20. SQL utiliza parâmetros.
21. Dapper permanece somente na Infrastructure.
22. Rotas e Contracts respeitam a versão da API.
23. Regras de domínio possuem testes unitários.
24. `dotnet test` passa.
25. `dotnet format --verify-no-changes` passa.
26. `dotnet build --no-restore` passa.