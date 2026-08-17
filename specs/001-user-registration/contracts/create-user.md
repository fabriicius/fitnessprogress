# Contrato HTTP: Criar Usuário

Único endpoint público desta feature. Versionamento e formato de erro seguem `.claude/rules/backend-stand.md` §9 (`ProblemDetails`/`ValidationProblemDetails` padrão do ASP.NET Core, sem contrato de erro customizado).

## `POST /api/v1/users`

### Request

`Content-Type: application/json`

```json
{
  "name": "string",
  "email": "string",
  "password": "string"
}
```

Corresponde a `FitProgress.Domain.Contracts.V1.Users.Requests.CreateUserRequest` (ver `data-model.md`).

### Respostas

| Cenário (spec) | Status | Corpo |
|---|---|---|
| US1: dados válidos, e-mail inédito | `201 Created` | `UserResponse` (ver abaixo) |
| US2: nome vazio, e-mail inválido e/ou senha fora da política — uma ou mais violações simultâneas | `400 Bad Request` | `ValidationProblemDetails` com uma entrada por campo violado |
| US3: e-mail já cadastrado (comparação case-insensitive) | `409 Conflict` | `ProblemDetails` com mensagem indicando e-mail já em uso |
| Falha inesperada de infraestrutura | `500 Internal Server Error` | `ProblemDetails` genérico, sem detalhes internos |

#### 201 — Corpo (`UserResponse`)

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "name": "Maria Silva",
  "email": "maria.silva@example.com",
  "createdAt": "2026-08-16T14:32:00Z"
}
```

Nunca inclui senha ou hash (FR-006, SC-002).

#### 400 — Corpo (`ValidationProblemDetails`, exemplo com múltiplas violações — edge case da spec)

```json
{
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "name": ["O nome é obrigatório."],
    "email": ["O e-mail informado é inválido."],
    "password": ["A senha deve conter ao menos 8 caracteres, uma letra maiúscula, uma minúscula e um número."]
  }
}
```

#### 409 — Corpo (`ProblemDetails`)

```json
{
  "title": "E-mail já está em uso.",
  "status": 409
}
```

### Regras de validação (aplicadas antes de qualquer persistência)

| Campo | Regra | FR |
|---|---|---|
| `name` | não vazio após trim; máximo 200 caracteres | FR-002 |
| `email` | formato válido; trim; comparação de unicidade case-insensitive | FR-003, FR-004 |
| `password` | mínimo 8 caracteres; máximo 100; ao menos 1 maiúscula, 1 minúscula, 1 dígito | FR-005 |

Todas as violações encontradas em um mesmo request são reportadas juntas — não interrompe na primeira (edge case da spec).

### Sem endpoint de consulta nesta feature

Não existe `GET /api/v1/users/{id}` nesta spec (fora de escopo). Por isso a resposta `201 Created` não inclui header `Location`. Referência: `agent_docs/architecture.md` §6.
