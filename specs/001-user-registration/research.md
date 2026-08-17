# Research: Cadastro de Usuário

**Fonte primária das decisões**: `agent_docs/architecture.md` (decisões técnicas já tomadas e confirmadas com o dono do produto em 2026-08-16). Este documento consolida cada decisão no formato Decision/Rationale/Alternatives exigido pela Fase 0, sem reabrir nenhuma delas. Nenhum item ficou como `NEEDS CLARIFICATION`.

## 1. Modelagem de domínio (User + Value Objects)

- **Decision**: `User` como entidade rica (construtor privado + factory `User.Create`), com VOs `PersonName`, `Email` (normalizado para lowercase), `Password` (transiente, nunca persistido) e `PasswordHash` (persistido). VOs usam `TryCreate` para permitir agregação de múltiplas violações em vez de lançar exceção na primeira encontrada.
- **Rationale**: FR-002/003/005 exigem validação de formato/regra por campo; o edge case de "múltiplos campos inválidos simultaneamente" exige que a validação não pare no primeiro erro — `TryCreate` viabiliza isso sem try/catch por campo. `User.Create` mantém a checagem de invariante defensiva.
- **Alternatives considered**: entidade anêmica com validação só na Application (rejeitada — regra de negócio pertence ao Domain, per `CLAUDE.md`); VOs lançando exceção por violação (rejeitada — impediria agregação de erros no mesmo request).
- **Referência**: `agent_docs/architecture.md` §1.

## 2. Hashing de senha

- **Decision**: `Microsoft.AspNetCore.Identity.PasswordHasher<TUser>` (pacote `Microsoft.Extensions.Identity.Core`), standalone (sem `UserManager`/Identity completo), implementado na Infrastructure atrás de `IPasswordHasher`.
- **Rationale**: PBKDF2 parametrizado corretamente "de fábrica", pacote oficial Microsoft, evita reimplementar hashing manual. Atende FR-006 (senha nunca em texto puro).
- **Alternatives considered**: BCrypt.Net-Next e Argon2/Konscious — ambas descartadas por serem dependências de terceiros sem necessidade adicional comprovada agora; ficam registradas como caminho futuro se houver requisito específico de resistência a hardware dedicado.
- **Referência**: `agent_docs/architecture.md` §2.

## 3. Validação de entrada

- **Decision**: validação manual via VOs do Domain (sem FluentValidation, sem DataAnnotations como mecanismo primário). Application agrega os erros dos três campos antes de prosseguir.
- **Rationale**: apenas 3 campos com regras simples — introduzir FluentValidation violaria a diretriz do `CLAUDE.md` de evitar dependências não exigidas pela especificação atual.
- **Alternatives considered**: FluentValidation (reavaliar se o projeto crescer com regras cruzadas/condicionais complexas); DataAnnotations no Contract (insuficiente para política de senha e normalização, que são regra de domínio).
- **Referência**: `agent_docs/architecture.md` §3.

## 4. Onde vivem `IUserRepository` e `IPasswordHasher`

- **Decision**: interfaces em `FitProgress.Domain/Abstractions/`; implementações concretas na Infrastructure (`Infrastructure/Repositories/Users/`, `Infrastructure/Security/`).
- **Rationale**: `.claude/rules/backend-stand.md` manda declarar a interface de repositório em `Infrastructure/IRepositories`, mas isso não compila com os `.csproj` já existentes no repositório — `FitProgress.Application.csproj` referencia somente `FitProgress.Domain.csproj`, e `FitProgress.Infrastructure.csproj` referencia `FitProgress.Application.csproj` (não o inverso). Declarar a interface na Infrastructure tornaria o tipo inacessível para o construtor de `UserService` na Application. Confirmado via inspeção direta dos `.csproj` durante o planejamento desta feature.
- **Alternatives considered**: inverter a referência de projeto (Application → Infrastructure) — rejeitada, cria dependência circular (Infrastructure já referencia Application) e viola `CLAUDE.md` ("`Application` pode depender de `Domain`", só isso); manter a interface na Infrastructure e mover `UserService` para lá — rejeitada, descaracteriza a camada Application inteira.
- **Status**: divergência pontual e documentada em relação à letra de `backend-stand.md` (que fala em `Infrastructure/IRepositories`); a regra de onde vive a *implementação* é respeitada integralmente. Recomendação registrada em `agent_docs/architecture.md` §9.1 para atualizar `backend-stand.md`.
- **Referência**: `agent_docs/architecture.md` §4, §9.1.

## 5. Camada Application (caso de uso único)

- **Decision**: `IUserService`/`UserService` (Service+Repository, não Mediator/Command), método `CreateUserAsync(CreateUserRequest, CancellationToken)`, retornando `CreateUserResult` (union de `Success`/`ValidationFailed`/`EmailAlreadyInUse`) em vez de lançar exceção para fluxos de negócio esperados.
- **Rationale**: `backend-stand.md` já define Service+Repository como convenção; um único caso de uso não justifica pipeline de mediator. Validação inválida e e-mail duplicado são desfechos previstos pela própria spec (US2, US3), não bugs — exceção seria custo desnecessário e contraria a regra 12 de `backend-stand.md` (exceção reservada para falha inesperada).
- **Alternatives considered**: exceções de domínio para cada caso de rejeição — rejeitada por tratar fluxo de negócio esperado como exceção.
- **Referência**: `agent_docs/architecture.md` §4.

## 6. Persistência (PostgreSQL/Dapper)

- **Decision**: tabela `users` (`id UUID` gerado em código, `name VARCHAR(200)`, `email VARCHAR(320)`, `password_hash TEXT`, `created_at TIMESTAMPTZ`) com índice único funcional `lower(email)` (sem extensão `citext`/`pgcrypto`). Repositório Dapper com transação explícita, `ConnectionFactory` sobre `NpgsqlDataSource`.
- **Rationale**: índice funcional garante FR-004 (unicidade case-insensitive) sem depender de extensões habilitadas manualmente no Neon. `id` gerado em código evita depender de `pgcrypto`. Segue à risca as regras 13/14 de `backend-stand.md` (transação obrigatória em escrita, SQL parametrizado).
- **Alternatives considered**: coluna `citext` — rejeitada por exigir extensão; `DEFAULT gen_random_uuid()` — rejeitado pela mesma razão.
- **Referência**: `agent_docs/architecture.md` §5.

## 7. Migração de schema

- **Decision**: DbUp (`dbup-postgresql`), scripts SQL versionados embutidos como recursos, aplicados no boot da aplicação contra tabela de controle própria.
- **Rationale**: deploy em container/Vercel sem acesso manual ao servidor Neon exige alguma automação de schema reproduzível. Confirmado com o dono do produto como a única dependência nova de infraestrutura desta feature.
- **Alternatives considered**: aplicar `.sql` manualmente contra o Neon — descartada por não ser reproduzível em CI/outros ambientes.
- **Referência**: `agent_docs/architecture.md` §5, §9.3 (confirmado).

## 8. Camada Api (endpoint HTTP)

- **Decision**: Controllers (`ControllerBase`), rota `POST /api/v1/users`, mapeamento de `CreateUserResult` para `201/400/409/500` via `ProblemDetails`/`ValidationProblemDetails`. Exige trocar o scaffold atual de Minimal API para `AddControllers()`/`MapControllers()` em `Program.cs`.
- **Rationale**: Controllers é o padrão explícito de `backend-stand.md` (regra 15, `UsersController`). Formato de erro padrão do ASP.NET Core evita inventar contrato de erro próprio.
- **Alternatives considered**: manter Minimal API do scaffold — rejeitada por contrariar `backend-stand.md`.
- **Referência**: `agent_docs/architecture.md` §6.

## 9. Estratégia de testes

- **Decision**: unitários (xUnit) obrigatórios para todas as regras de domínio (VOs, `User.Create`) e para `UserService` com dependências fake/mock; testes de integração com `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` para validar a constraint de unicidade fim a fim e confirmar que a senha nunca é persistida em texto puro.
- **Rationale**: regra 23 de `backend-stand.md` exige teste unitário para toda regra de domínio nova. A garantia real de unicidade só é verificável contra Postgres real — Testcontainers isola isso sem depender do Neon de produção/dev. Confirmado com o dono do produto.
- **Alternatives considered**: apontar testes de integração para uma branch dedicada no Neon — descartada por acoplar testes a um recurso externo compartilhado e ser mais lenta.
- **Referência**: `agent_docs/architecture.md` §8, §9.4 (confirmado).

## 10. Limites numéricos

- **Decision**: `Name` até 200 caracteres; `Password` até 100 caracteres.
- **Rationale**: a spec (FR-002 e edge case de input gigante) exige limites sem definir números; valores padrão de mercado confirmados com o dono do produto.
- **Referência**: `agent_docs/architecture.md` §9.2 (confirmado).

## Unknowns remanescentes

Nenhum. Todos os itens de Technical Context do `plan.md` estão resolvidos por este documento e por `agent_docs/architecture.md`.
