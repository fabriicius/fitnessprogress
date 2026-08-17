# Quickstart: Validar Cadastro de Usuário

Guia para rodar e validar manualmente a feature `001-user-registration` fim a fim. Não é um guia de implementação — para desenho de dados e contrato completo, ver `data-model.md` e `contracts/create-user.md`.

## Pré-requisitos

- .NET 10 SDK instalado.
- Docker disponível (necessário para os testes de integração com `Testcontainers.PostgreSql` — `agent_docs/architecture.md` §9.4).
- Uma instância PostgreSQL acessível para rodar a API localmente (Neon dev branch ou Postgres local) e sua connection string em variável de ambiente (nunca em arquivo versionado — `CLAUDE.md`).
- Schema `users` aplicado via DbUp (roda automaticamente no boot da aplicação — `agent_docs/architecture.md` §5, §9.3).

## Setup

```bash
dotnet restore

export ConnectionStrings__Postgres="Host=...;Database=...;Username=...;Password=..."

dotnet build --no-restore
```

## Rodar a API localmente

```bash
dotnet run --project src/FitProgress.Api/FitProgress.Api.csproj
```

A migração DbUp aplica o script de criação da tabela `users` (ver `data-model.md`) no boot, se ainda não aplicado.

## Cenários de validação (mapeados às Acceptance Scenarios da spec)

### 1. Criar conta com dados válidos (US1)

```bash
curl -i -X POST http://localhost:5000/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria Silva","email":"maria.silva@example.com","password":"Senha123"}'
```

**Esperado**: `201 Created`, corpo `UserResponse` com `id`, `name`, `email`, `createdAt` — sem hash/senha (ver `contracts/create-user.md`).

Verificação adicional (SC-002): consultar a tabela `users` diretamente e confirmar que `password_hash` não é igual a `"Senha123"` nem a nenhuma variação em texto puro.

### 2. Rejeitar dados inválidos, todos os erros juntos (US2, edge case de múltiplas violações)

```bash
curl -i -X POST http://localhost:5000/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"name":"","email":"nao-e-email","password":"123"}'
```

**Esperado**: `400 Bad Request` com violações de `name`, `email` e `password` simultaneamente no corpo (não apenas a primeira).

### 3. Rejeitar e-mail duplicado, inclusive com capitalização diferente (US3, edge case de case-insensitivity)

```bash
# primeira criação
curl -i -X POST http://localhost:5000/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Maria Silva","email":"maria.silva@example.com","password":"Senha123"}'

# segunda tentativa, mesmo e-mail com capitalização diferente
curl -i -X POST http://localhost:5000/api/v1/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Outra Pessoa","email":"Maria.Silva@Example.com","password":"OutraSenha1"}'
```

**Esperado**: primeira chamada `201 Created`; segunda chamada `409 Conflict`.

## Rodar os testes automatizados

```bash
dotnet test
```

Cobre (ver `agent_docs/architecture.md` §8 para a lista completa):
- Regras de domínio dos Value Objects e `User.Create` (unitário).
- `UserService.CreateUserAsync` com dependências fake (unitário, sem Postgres real).
- Fluxo HTTP completo via `WebApplicationFactory` + `Testcontainers.PostgreSql` (integração), incluindo a verificação real da constraint `ux_users_email_lower`.

## Checklist de conclusão (antes de considerar a feature pronta)

Conforme `CLAUDE.md`:

```bash
dotnet test
dotnet format --verify-no-changes
dotnet build --no-restore
```
