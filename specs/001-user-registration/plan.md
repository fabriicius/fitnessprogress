# Implementation Plan: Cadastro de Usuário

**Branch**: `001-user-registration` | **Date**: 2026-08-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-user-registration/spec.md`

**Fonte obrigatória das decisões técnicas**: `agent_docs/architecture.md` (decisões arquiteturais desta feature, confirmadas com o dono do produto em 2026-08-16), além de `CLAUDE.md` e `.claude/rules/backend-stand.md`. Este plano não altera nenhum requisito funcional da spec — apenas traduz as decisões já tomadas em artefatos de design (Fase 0 e Fase 1).

## Summary

Permitir que um novo usuário crie uma conta informando nome, e-mail e senha (FR-001), com validação de todos os campos reportando todas as violações simultaneamente (FR-002/003/005, edge case), e-mail único e case-insensitive (FR-004), senha nunca persistida em texto puro (FR-006) e mensagens de erro compreensíveis (FR-007/008).

Abordagem técnica: endpoint `POST /api/v1/users` (Controller ASP.NET Core) → `UserService` (Application) orquestra validação via Value Objects do Domain (`PersonName`, `Email`, `Password`), hashing via `PasswordHasher<TUser>` e persistência via `UserRepository` (Dapper/PostgreSQL) com constraint de unicidade `lower(email)`. Nenhum fluxo de login/autenticação é implementado nesta feature (fora de escopo, conforme Assumptions da spec).

## Technical Context

**Language/Version**: C# / .NET 10 LTS

**Primary Dependencies**: ASP.NET Core Web API (Controllers), Dapper, Npgsql + `Npgsql.DependencyInjection`, `Microsoft.Extensions.Identity.Core` (`PasswordHasher<TUser>`), `dbup-postgresql` (migração de schema)

**Storage**: PostgreSQL (Neon em produção; qualquer instância Postgres compatível em dev), acesso via Dapper

**Testing**: xUnit (unitário: Domain + Application com fakes); `Microsoft.AspNetCore.Mvc.Testing` + `Testcontainers.PostgreSql` (integração: fluxo HTTP completo contra Postgres real)

**Target Platform**: Linux container (Docker), deploy na Vercel via container image / Fluid Compute

**Project Type**: web-service — monólito modular em camadas (Api / Application / Domain / Infrastructure), conforme `CLAUDE.md` e `.claude/rules/backend-stand.md`

**Performance Goals**: Sem meta de throughput explícita na spec. Requisição de cadastro é interativa (SC-001: usuário conclui em menos de 1 minuto do ponto de vista de uso, não é uma meta de latência de API).

**Constraints**: senha nunca em texto puro em nenhum momento além do fluxo transiente de hashing (FR-006); SQL sempre parametrizado (`backend-stand.md` §14); nenhum secret/connection string versionado (`CLAUDE.md`); Domain não referencia Postgres/Dapper/JWT/bibliotecas de hashing.

**Scale/Scope**: MVP de um único endpoint (`POST /api/v1/users`), uma tabela (`users`), sem paginação/listagem/consulta nesta feature.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

`.specify/memory/constitution.md` está no estado de template (placeholders não preenchidos) — não há princípios de projeto formalmente ratificados ainda. Por instrução explícita do usuário para este plano, os documentos vinculantes usados como gate são: `CLAUDE.md` (regras de projeto), `.claude/rules/backend-stand.md` (regras de camada C#) e `agent_docs/architecture.md` (decisões desta feature).

| Gate | Verificação | Status |
|---|---|---|
| Limites de camada (`CLAUDE.md`) | Domain não referencia Application/Infrastructure/Api; Application só referencia Domain; Infrastructure pode usar Application+Domain; Api coordena via DI | ✅ Respeitado pelo desenho em `agent_docs/architecture.md` §1–§6 |
| Sem regra de negócio em Controller/Repository | Validação e orquestração ficam em Domain (VOs) e Application (`UserService`); Controller só traduz `CreateUserResult` → HTTP; Repository só persiste | ✅ |
| Senha nunca em texto puro (`CLAUDE.md`) | `Password` é VO transiente; só `PasswordHash` é persistido | ✅ |
| Sem infraestrutura não exigida pela spec (`CLAUDE.md`) | Nenhum cache, mensageria, ou serviço externo introduzido | ✅ |
| Evitar dependências não exigidas (`CLAUDE.md`) | Duas exceções justificadas e aprovadas: DbUp (migração reproduzível, necessária pelo modelo de deploy sem acesso manual ao servidor) e Testcontainers.PostgreSql (única forma de validar a constraint de unicidade contra Postgres real em teste automatizado) — ver Complexity Tracking | ⚠️ Exceções documentadas e aprovadas |
| Estrutura de pastas (`backend-stand.md` §1–§2) | Models em `Domain/Models/Users/`, Contracts em `Domain/Contracts/V1/Users/`, Service em `Application/Services/Users/` + `IService/Users/`, Repository em `Infrastructure/Repositories/Users/` | ✅ |
| Interface de Repository em `Infrastructure/IRepositories` (`backend-stand.md` §5, §18) | **Divergência documentada**: `IUserRepository`/`IPasswordHasher` ficam em `Domain/Abstractions/` — a regra literal não compila com os `.csproj` já existentes (Application não referencia Infrastructure). Ver `research.md` item 4 e `agent_docs/architecture.md` §9.1 | ⚠️ Exceção documentada e aprovada |
| Teste unitário para toda regra de domínio (`backend-stand.md` §17, §23) | Cobertura planejada em `agent_docs/architecture.md` §8 | ✅ |
| Async/await + CancellationToken obrigatórios (`backend-stand.md` §10–§11) | Toda assinatura de Service/Repository segue o padrão | ✅ |
| Transação explícita em escrita (`backend-stand.md` §13) | `UserRepository.AddAsync` usa `BeginTransaction`/`Commit`/`Rollback` | ✅ |

Nenhuma violação sem justificativa. As duas exceções (⚠️) estão detalhadas em Complexity Tracking.

## Project Structure

### Documentation (this feature)

```text
specs/001-user-registration/
├── plan.md              # Este arquivo
├── research.md          # Fase 0 — decisões consolidadas
├── data-model.md         # Fase 1 — entidade User, Value Objects, Contracts, schema
├── contracts/
│   └── create-user.md    # Fase 1 — contrato POST /api/v1/users
├── quickstart.md         # Fase 1 — guia de validação manual
└── tasks.md              # Fase 2 — gerado por /speckit-tasks (não criado aqui)
```

### Source Code (repository root)

```text
src/
├── FitProgress.Api/
│   ├── Controllers/
│   │   └── V1/
│   │       └── UsersController.cs
│   └── DependencyInjection/
│       └── ApiDependencyInjection.cs
│   # Program.cs: trocar Minimal API do scaffold por AddControllers()/MapControllers()
│
├── FitProgress.Application/
│   ├── IService/
│   │   └── Users/
│   │       └── IUserService.cs
│   ├── Services/
│   │   └── Users/
│   │       └── UserService.cs
│   ├── Results/
│   │   └── CreateUserResult.cs
│   └── DependencyInjection/
│       └── ApplicationDependencyInjection.cs
│
├── FitProgress.Domain/
│   ├── Models/
│   │   └── Users/
│   │       └── User.cs
│   ├── ValueObjects/
│   │   ├── PersonName.cs
│   │   ├── Email.cs
│   │   ├── Password.cs
│   │   └── PasswordHash.cs
│   ├── Contracts/
│   │   └── V1/
│   │       └── Users/
│   │           ├── Requests/
│   │           │   └── CreateUserRequest.cs
│   │           └── Responses/
│   │               └── UserResponse.cs
│   └── Abstractions/
│       ├── IUserRepository.cs
│       └── IPasswordHasher.cs
│
└── FitProgress.Infrastructure/
    ├── Database/
    │   ├── ConnectionFactory.cs
    │   └── Scripts/
    │       └── 0001_create_users_table.sql
    ├── Repositories/
    │   └── Users/
    │       └── UserRepository.cs
    ├── Security/
    │   └── PasswordHasher.cs
    └── DependencyInjection/
        └── InfrastructureDependencyInjection.cs

tests/
├── FitProgress.UnitTests/
│   ├── Domain/
│   │   └── Users/
│   │       ├── PersonNameTests.cs
│   │       ├── EmailTests.cs
│   │       ├── PasswordTests.cs
│   │       └── UserTests.cs
│   └── Application/
│       └── Services/
│           └── Users/
│               └── UserServiceTests.cs
└── FitProgress.IntegrationTests/
    └── Users/
        └── CreateUserEndpointTests.cs
```

**Structure Decision**: monólito em camadas já existente (`src/FitProgress.{Api,Application,Domain,Infrastructure}`, `tests/FitProgress.{UnitTests,IntegrationTests}`), sem novo projeto/serviço. Toda a feature é implementada dentro dessa estrutura, seguindo `.claude/rules/backend-stand.md` — com a única divergência documentada da localização de `IUserRepository`/`IPasswordHasher` (Domain/Abstractions em vez de Infrastructure/IRepositories, ver Constitution Check e `research.md` item 4).

## Complexity Tracking

> Exceções ao princípio de "evitar abstrações/dependências não exigidas" (`CLAUDE.md`), ambas avaliadas e aprovadas explicitamente pelo dono do produto durante a preparação de `agent_docs/architecture.md`.

| Violation | Why Needed | Simpler Alternative Rejected Because |
|---|---|---|
| Nova dependência: DbUp (`dbup-postgresql`) | Deploy em container/Vercel sem acesso manual ao servidor Neon exige aplicar o schema de forma automatizada e reproduzível no boot | Rodar `.sql` manualmente contra o Neon funciona uma vez, mas não é reproduzível em CI nem escala para os próximos módulos (treino, nutrição) |
| Nova dependência de teste: `Testcontainers.PostgreSql` | A garantia real de unicidade case-insensitive (índice funcional `lower(email)`) só é verificável contra um Postgres real, não com fake/mock | Testes unitários com repositório fake não verificam a constraint do banco; apontar para uma branch dedicada no Neon acopla os testes a um recurso externo compartilhado e mais lento |
| Divergência de `backend-stand.md` §5/§18: `IUserRepository`/`IPasswordHasher` no Domain em vez de Infrastructure | A regra literal não compila com os `.csproj` já existentes (Application não referencia Infrastructure) — ver `research.md` item 4 | Inverter a referência de projeto (Application → Infrastructure) criaria dependência circular, já que Infrastructure referencia Application |
