---

description: "Task list for feature implementation"
---

# Tasks: Cadastro de Usuário

**Input**: Design documents from `/specs/001-user-registration/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/create-user.md`, `quickstart.md`, `agent_docs/architecture.md`, `.claude/rules/backend-stand.md`

**Tests**: Incluídos — `CLAUDE.md` e `.claude/rules/backend-stand.md` (§17, §23) exigem teste unitário para toda regra de domínio nova, então testes não são opcionais neste projeto.

**Organization**: Tarefas agrupadas por user story (spec.md) para permitir implementação e teste independentes de cada uma.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Pode rodar em paralelo (arquivos diferentes, sem dependência de tarefa incompleta)
- **[Story]**: US1, US2 ou US3, conforme `spec.md`
- Caminhos de arquivo exatos em cada descrição

## Path Conventions

Monólito em camadas já existente: `src/FitProgress.{Api,Application,Domain,Infrastructure}`, `tests/FitProgress.{UnitTests,IntegrationTests}` — conforme `plan.md` (Structure Decision) e `.claude/rules/backend-stand.md`.

---

## Phase 1: Setup

**Purpose**: Preparar o scaffold existente (pacotes NuGet, remoção de placeholders, troca de Minimal API por Controllers) para receber a feature.

- [X] T001 [P] Substituir o scaffold Minimal API por Controllers em `src/FitProgress.Api/Program.cs`: adicionar `builder.Services.AddControllers()` e `app.MapControllers()`, remover o endpoint de exemplo `/weatherforecast` e o `record WeatherForecast`
- [X] T002 [P] Adicionar `Dapper`, `Npgsql`, `Npgsql.DependencyInjection`, `Microsoft.Extensions.Identity.Core` e `dbup-postgresql` em `src/FitProgress.Infrastructure/FitProgress.Infrastructure.csproj`
- [X] T003 [P] Adicionar `Microsoft.AspNetCore.Mvc.Testing` em `tests/FitProgress.IntegrationTests/FitProgress.IntegrationTests.csproj`
- [X] T004 [P] Adicionar `Testcontainers.PostgreSql` em `tests/FitProgress.IntegrationTests/FitProgress.IntegrationTests.csproj`
- [X] T005 [P] Remover arquivos placeholder do scaffold: `src/FitProgress.Domain/Class1.cs`, `src/FitProgress.Application/Class1.cs`, `src/FitProgress.Infrastructure/Class1.cs`, `tests/FitProgress.UnitTests/UnitTest1.cs`, `tests/FitProgress.IntegrationTests/UnitTest1.cs`

**Checkpoint**: Projeto compila com o scaffold limpo, pronto para receber código de infraestrutura.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Infraestrutura de banco e DI que todas as user stories exigem.

**⚠️ CRITICAL**: Nenhuma user story pode começar antes desta fase estar completa.

- [X] T006 [P] Criar `ConnectionFactory` (wrapper fino sobre `NpgsqlDataSource`, método `CreateOpenConnectionAsync(CancellationToken)`) em `src/FitProgress.Infrastructure/Database/ConnectionFactory.cs`
- [X] T007 [P] Criar script de migração `0001_create_users_table.sql` (tabela `users` + índice único funcional `lower(email)`, conforme `data-model.md`) em `src/FitProgress.Infrastructure/Database/Scripts/0001_create_users_table.sql`
- [X] T008 Configurar DbUp para aplicar os scripts embutidos de `Database/Scripts/` no boot da aplicação em `src/FitProgress.Infrastructure/DependencyInjection/InfrastructureDependencyInjection.cs` (depende de T007)
- [X] T009 [P] Criar stub de extensão `AddApiDependencies` em `src/FitProgress.Api/DependencyInjection/ApiDependencyInjection.cs`
- [X] T010 [P] Criar stub de extensão `AddApplicationDependencies` em `src/FitProgress.Application/DependencyInjection/ApplicationDependencyInjection.cs`
- [X] T011 Registrar `AddNpgsqlDataSource`, `ConnectionFactory`, execução do DbUp e as três extensões de DI (`AddApiDependencies`, `AddApplicationDependencies`, `AddInfrastructureDependencies`) em `src/FitProgress.Api/Program.cs` (depende de T006, T008, T009, T010)

**Checkpoint**: Conexão com banco, migração de schema e esqueleto de DI prontos — user stories podem começar.

---

## Phase 3: User Story 1 - Criar conta com nome, e-mail e senha (Priority: P1) 🎯 MVP

**Goal**: Um novo usuário informa nome, e-mail e senha válidos e a conta é criada com sucesso; a senha nunca é armazenada em texto puro.

**Independent Test**: Enviar `POST /api/v1/users` com nome, e-mail e senha válidos e verificar `201 Created` com `UserResponse`, e que `password_hash` no banco não é igual à senha enviada.

### Domain (User Story 1)

- [X] T012 [P] [US1] Criar Value Object `PersonName` (`TryCreate`: trim, não vazio, máximo 200 caracteres) em `src/FitProgress.Domain/ValueObjects/PersonName.cs`
- [X] T013 [P] [US1] Criar Value Object `Email` (`TryCreate`: trim, formato válido, normalização para lowercase, igualdade case-insensitive) em `src/FitProgress.Domain/ValueObjects/Email.cs`
- [X] T014 [P] [US1] Criar Value Object `Password` transiente (`TryCreate`: 8–100 caracteres, exige maiúscula, minúscula e dígito) em `src/FitProgress.Domain/ValueObjects/Password.cs`
- [X] T015 [P] [US1] Criar Value Object `PasswordHash` (wrapper opaco, string não vazia) em `src/FitProgress.Domain/ValueObjects/PasswordHash.cs`
- [X] T016 [US1] Criar entidade `User` (construtor privado + factory `Create`, lança `DomainException` em estado inválido) em `src/FitProgress.Domain/Models/Users/User.cs` (depende de T012, T013, T014, T015)
- [X] T017 [P] [US1] Criar abstração `IUserRepository` (`ExistsByEmailAsync`, `AddAsync`) em `src/FitProgress.Domain/Abstractions/IUserRepository.cs`
- [X] T018 [P] [US1] Criar abstração `IPasswordHasher` (`Hash`) em `src/FitProgress.Domain/Abstractions/IPasswordHasher.cs`
- [X] T019 [P] [US1] Criar `CreateUserRequest` em `src/FitProgress.Domain/Contracts/V1/Users/Requests/CreateUserRequest.cs`
- [X] T020 [P] [US1] Criar `UserResponse` em `src/FitProgress.Domain/Contracts/V1/Users/Responses/UserResponse.cs`

### Testes de domínio (User Story 1)

- [X] T021 [P] [US1] Testes unitários de `PersonName` (vazio/só espaços, acima de 200 chars, trim, caso válido) em `tests/FitProgress.UnitTests/Domain/Users/PersonNameTests.cs`
- [X] T022 [P] [US1] Testes unitários de `Email` (formato inválido, normalização lowercase, igualdade case-insensitive, trim, caso válido) em `tests/FitProgress.UnitTests/Domain/Users/EmailTests.cs`
- [X] T023 [P] [US1] Testes unitários de `Password` (menor que 8, sem maiúscula, sem minúscula, sem dígito, caso válido) em `tests/FitProgress.UnitTests/Domain/Users/PasswordTests.cs`
- [X] T024 [P] [US1] Testes unitários de `User.Create` (instância válida a partir de VOs válidos; `DomainException` em estado inconsistente) em `tests/FitProgress.UnitTests/Domain/Users/UserTests.cs`

### Infrastructure (User Story 1)

- [X] T025 [P] [US1] Implementar `PasswordHasher : IPasswordHasher` usando `PasswordHasher<User>` em `src/FitProgress.Infrastructure/Security/PasswordHasher.cs` (depende de T018)
- [X] T026 [P] [US1] Implementar `UserRepository.AddAsync` (transação explícita `BeginTransaction`/`Commit`/`Rollback`, `INSERT` parametrizado via `CommandDefinition`) em `src/FitProgress.Infrastructure/Repositories/Users/UserRepository.cs` (depende de T006, T017)
- [X] T027 [US1] Registrar `IUserRepository`→`UserRepository` e `IPasswordHasher`→`PasswordHasher` em `src/FitProgress.Infrastructure/DependencyInjection/InfrastructureDependencyInjection.cs` (depende de T025, T026)

### Application (User Story 1)

- [X] T028 [P] [US1] Criar `CreateUserResult` (estados `Success`, `ValidationFailed`, `EmailAlreadyInUse`) em `src/FitProgress.Application/Results/CreateUserResult.cs`
- [X] T029 [P] [US1] Criar `IUserService` (`CreateUserAsync`) em `src/FitProgress.Application/IService/Users/IUserService.cs`
- [X] T030 [US1] Implementar `UserService.CreateUserAsync` (trim de nome/e-mail, validação agregada via `TryCreate` dos três VOs, hashing, persistência via `IUserRepository.AddAsync`, mapeamento para `Success`, `try/catch` com log e `throw;` para falhas inesperadas) em `src/FitProgress.Application/Services/Users/UserService.cs` (depende de T016, T025, T026, T028, T029)
- [X] T031 [US1] Registrar `IUserService`→`UserService` em `src/FitProgress.Application/DependencyInjection/ApplicationDependencyInjection.cs` (depende de T030)

### Api (User Story 1)

- [X] T032 [US1] Implementar `UsersController` com `POST /api/v1/users`, mapeamento exaustivo de `CreateUserResult` para `201/400/409/500` (`ProblemDetails`/`ValidationProblemDetails`, conforme `contracts/create-user.md`) em `src/FitProgress.Api/Controllers/V1/UsersController.cs` (depende de T030)

### Testes de aplicação e integração (User Story 1)

- [X] T033 [P] [US1] Testes unitários de `UserService.CreateUserAsync` — caminho de sucesso com `IUserRepository`/`IPasswordHasher` fake, confirmando que o repositório recebe o hash e nunca a senha em texto puro em `tests/FitProgress.UnitTests/Application/Services/Users/UserServiceTests.cs` (depende de T030)
- [X] T034 [P] [US1] Teste de integração: `POST /api/v1/users` com dados válidos retorna `201 Created` com `UserResponse`, e consulta direta ao banco confirma `password_hash` diferente da senha enviada em `tests/FitProgress.IntegrationTests/Users/CreateUserEndpointTests.cs` (depende de T032, T004)

**Checkpoint**: User Story 1 completa — cadastro com dados válidos funciona fim a fim (MVP demonstrável).

---

## Phase 4: User Story 2 - Validação dos dados informados (Priority: P2)

**Goal**: Quando nome, e-mail ou senha são inválidos — inclusive vários ao mesmo tempo — o sistema rejeita a criação e reporta todas as violações, sem afetar o caminho feliz da User Story 1.

**Independent Test**: Enviar `POST /api/v1/users` com combinações de dados inválidos (nome vazio, e-mail malformado, senha fraca) e confirmar `400 Bad Request` com **todas** as violações no corpo, não apenas a primeira.

> A validação em si já foi implementada nos Value Objects (T012–T014) e na agregação de `UserService.CreateUserAsync` (T030) como parte do desenho correto da User Story 1 — os VOs usam `TryCreate` justamente para permitir agregar múltiplas violações sem short-circuit. Esta fase adiciona a cobertura de teste que comprova esse comportamento e os casos de borda de tamanho/normalização que a User Story 1 não exercitou.

- [X] T035 [P] [US2] Teste unitário de `UserService.CreateUserAsync` retornando `ValidationFailed` com nome vazio + e-mail inválido + senha fraca simultaneamente, confirmando que as três violações aparecem juntas em `tests/FitProgress.UnitTests/Application/Services/Users/UserServiceTests.cs` (depende de T030)
- [X] T036 [P] [US2] Testes unitários de casos de borda dos VOs: espaços nas pontas de `PersonName`/`Email`, nome nos limites de 200/201 caracteres, senha nos limites de 100/101 caracteres em `tests/FitProgress.UnitTests/Domain/Users/PersonNameTests.cs` e `tests/FitProgress.UnitTests/Domain/Users/PasswordTests.cs` (depende de T021, T023)
- [X] T037 [US2] Teste de integração: `POST /api/v1/users` com nome vazio, e-mail inválido e senha fraca simultaneamente retorna `400 Bad Request` com as três violações no corpo, no formato `ValidationProblemDetails` de `contracts/create-user.md` em `tests/FitProgress.IntegrationTests/Users/CreateUserEndpointTests.cs` (depende de T032)

**Checkpoint**: User Story 2 completa — rejeição de dados inválidos comprovada por teste, incluindo o edge case de múltiplas violações simultâneas.

---

## Phase 5: User Story 3 - Impedir contas duplicadas (Priority: P3)

**Goal**: Duas contas não podem compartilhar o mesmo e-mail (comparação case-insensitive), inclusive sob corrida de concorrência entre duas requisições simultâneas.

**Independent Test**: Criar uma conta com um e-mail e, em seguida, tentar criar outra com o mesmo e-mail (inclusive com capitalização diferente); a segunda tentativa deve retornar `409 Conflict` com mensagem clara.

- [X] T038 [US3] Implementar `UserRepository.ExistsByEmailAsync` (consulta somente leitura, parametrizada, comparando `lower(email)`) em `src/FitProgress.Infrastructure/Repositories/Users/UserRepository.cs` (depende de T026)
- [X] T039 [US3] Atualizar `UserRepository.AddAsync` para capturar `PostgresException` com `SqlState == "23505"` e retornar `false` (conflito de unicidade) em vez de propagar, mantendo `throw;` para as demais exceções em `src/FitProgress.Infrastructure/Repositories/Users/UserRepository.cs` (depende de T038)
- [X] T040 [US3] Atualizar `UserService.CreateUserAsync` para checar `ExistsByEmailAsync` antes do hashing (retorno rápido de `EmailAlreadyInUse`) e tratar `AddAsync` retornando `false` como `EmailAlreadyInUse` também (proteção contra corrida) em `src/FitProgress.Application/Services/Users/UserService.cs` (depende de T039)
- [X] T041 [P] [US3] Testes unitários de `UserService.CreateUserAsync` para e-mail já existente (via `ExistsByEmailAsync` = true, sem chamar `AddAsync`) e para corrida de concorrência (`ExistsByEmailAsync` = false mas `AddAsync` = false) em `tests/FitProgress.UnitTests/Application/Services/Users/UserServiceTests.cs` (depende de T040)
- [X] T042 [P] [US3] Teste de integração: duas tentativas de cadastro com o mesmo e-mail, incluindo capitalização diferente (`Maria@Example.com` vs `maria@example.com`) — segunda tentativa retorna `409 Conflict` em `tests/FitProgress.IntegrationTests/Users/CreateUserEndpointTests.cs` (depende de T040)

**Checkpoint**: As três user stories completas e independentemente testáveis — feature pronta para revisão final.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validação final exigida por `CLAUDE.md` antes de considerar a feature concluída.

- [X] T043 [P] Rodar `dotnet format --verify-no-changes` e corrigir divergências de formatação
- [X] T044 Rodar `dotnet build --no-restore` e corrigir eventuais erros de compilação (depende de T001–T042)
- [ ] T045 Rodar `dotnet test` e confirmar que todos os testes unitários e de integração passam (depende de T044)
- [ ] T046 [P] Validar manualmente os três cenários de `quickstart.md` (`curl` contra a API rodando localmente) confirmando `201`, `400` com múltiplas violações e `409`
- [X] T047 [P] Atualizar `.claude/rules/backend-stand.md` (§5, §18) para refletir a localização confirmada de `IUserRepository`/`IPasswordHasher` em `Domain/Abstractions/` (ver `research.md` item 4 e `plan.md` — Constitution Check)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: sem dependências — pode começar imediatamente
- **Foundational (Phase 2)**: depende da conclusão do Setup — bloqueia todas as user stories
- **User Story 1 (Phase 3)**: depende só do Foundational
- **User Story 2 (Phase 4)**: depende do Foundational e reaproveita o código de US1 (T012–T014, T030) — não pode ser testada de forma significativa sem US1 implementada, mas não modifica nenhum arquivo de US1
- **User Story 3 (Phase 5)**: depende do Foundational e modifica arquivos criados em US1 (`UserRepository.cs`, `UserService.cs`) para adicionar a checagem de duplicidade
- **Polish (Phase 6)**: depende de todas as user stories desejadas estarem completas

### User Story Dependencies

- **US1 (P1)**: nenhuma dependência de outra story — é a base
- **US2 (P2)**: reaproveita o código de US1 sem alterá-lo; testável de forma independente assim que US1 existir
- **US3 (P3)**: estende arquivos de US1 (`UserRepository`, `UserService`) com a checagem de duplicidade; testável de forma independente (a lógica de duplicidade é isolável mesmo reaproveitando os mesmos arquivos)

### Dentro de cada User Story

- Value Objects antes da entidade `User`
- Abstrações (`IUserRepository`, `IPasswordHasher`) antes das implementações de Infrastructure
- Implementações de Infrastructure antes de `UserService`
- `UserService` antes de `UsersController`
- Implementação antes dos testes que a exercitam (testes de domínio podem ser escritos em paralelo aos VOs correspondentes)

### Parallel Opportunities

- Setup: T001–T005 em paralelo (arquivos independentes)
- Foundational: T006, T007, T009, T010 em paralelo
- US1: T012–T015 em paralelo; T017–T020 em paralelo; T021–T024 em paralelo; T025–T026 em paralelo; T028–T029 em paralelo; T033–T034 em paralelo
- US2: T035–T036 em paralelo
- US3: T041–T042 em paralelo
- Polish: T043, T046, T047 em paralelo

---

## Parallel Example: User Story 1

```bash
# Value Objects em paralelo:
Task: "Criar Value Object PersonName em src/FitProgress.Domain/ValueObjects/PersonName.cs"
Task: "Criar Value Object Email em src/FitProgress.Domain/ValueObjects/Email.cs"
Task: "Criar Value Object Password em src/FitProgress.Domain/ValueObjects/Password.cs"
Task: "Criar Value Object PasswordHash em src/FitProgress.Domain/ValueObjects/PasswordHash.cs"

# Testes de domínio em paralelo (após os VOs acima):
Task: "Testes unitários de PersonName em tests/FitProgress.UnitTests/Domain/Users/PersonNameTests.cs"
Task: "Testes unitários de Email em tests/FitProgress.UnitTests/Domain/Users/EmailTests.cs"
Task: "Testes unitários de Password em tests/FitProgress.UnitTests/Domain/Users/PasswordTests.cs"
Task: "Testes unitários de User.Create em tests/FitProgress.UnitTests/Domain/Users/UserTests.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 apenas)

1. Completar Fase 1: Setup
2. Completar Fase 2: Foundational (bloqueia todas as stories)
3. Completar Fase 3: User Story 1
4. **Parar e validar**: rodar `dotnet test` e o cenário 1 de `quickstart.md`
5. Nesse ponto já existe um endpoint de cadastro funcional (sem cobertura explícita de validação/duplicidade além do que os VOs e a constraint de banco já garantem)

### Entrega Incremental

1. Setup + Foundational → infraestrutura pronta
2. US1 → caminho feliz completo e testável (MVP)
3. US2 → cobertura de validação comprovada por teste (nenhuma mudança de código de produção sobre US1)
4. US3 → checagem de duplicidade adicionada a `UserRepository`/`UserService`, testável isoladamente
5. Polish → `dotnet format`/`build`/`test` + validação manual via `quickstart.md`

### Observação sobre reaproveitamento entre stories

Diferente do padrão onde cada story só adiciona arquivos novos, aqui **US2 não modifica nenhum arquivo de produção** (a validação agregada já nasce correta em US1, por exigência do desenho de VOs com `TryCreate`) e **US3 modifica dois arquivos de US1** (`UserRepository.cs`, `UserService.cs`) para adicionar a checagem de duplicidade. Isso é intencional — está documentado em `research.md` e `plan.md` — e não compromete a testabilidade independente de cada story.

---

## Notes

- `[P]` = arquivos diferentes, sem dependência de tarefa incompleta
- `[Story]` mapeia a tarefa à user story correspondente para rastreabilidade
- Verificar que os testes falham antes de implementar (quando aplicável ao fluxo TDD do time)
- Rodar `dotnet test`, `dotnet format --verify-no-changes` e `dotnet build --no-restore` antes de concluir a feature (`CLAUDE.md`)
- Parar em qualquer checkpoint para validar a story isoladamente
