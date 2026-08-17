# Data Model: Cadastro de Usuário

Derivado de `specs/001-user-registration/spec.md` (Key Entities, Functional Requirements, Edge Cases) e das decisões de `agent_docs/architecture.md` §1 e §5.

## Entidade: User

**Local**: `FitProgress.Domain/Models/Users/User.cs`

Representa uma pessoa cadastrada na plataforma (Key Entity da spec). Entidade rica, criada exclusivamente via factory `User.Create(PersonName name, Email email, PasswordHash passwordHash)`; construtor privado.

| Campo | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `Id` | `Guid` | sim | Gerado pelo próprio Domain (`Guid.NewGuid()`) dentro de `Create` — nunca recebido de fora. |
| `Name` | `PersonName` (VO) | sim | Ver Value Objects abaixo. |
| `Email` | `Email` (VO) | sim | Ver Value Objects abaixo. Identificador único de acesso (FR-004). |
| `PasswordHash` | `PasswordHash` (VO) | sim | Nunca a senha em texto puro (FR-006). |
| `CreatedAt` | `DateTimeOffset` | sim | `DateTimeOffset.UtcNow`, atribuído dentro de `Create`. |

**Invariante de criação**: `User.Create` lança `DomainException` se receber qualquer VO em estado inválido — checagem defensiva; o caminho principal de validação acontece antes, via `TryCreate` de cada VO (ver Application, seção "Fluxo" em `agent_docs/architecture.md` §4).

**Sem transições de estado**: esta spec cobre apenas criação. Não há campos de status/edição nesta versão (fora de escopo: login, atualização de perfil, exclusão).

## Value Objects

**Local**: `FitProgress.Domain/ValueObjects/`

Todos seguem o padrão `TryCreate(valorBruto, out vo, out erro)` — não lançam exceção para violação de regra de negócio esperada, permitindo agregar múltiplas violações no mesmo request (edge case da spec: "múltiplos campos inválidos simultaneamente").

### PersonName

| Regra | Origem |
|---|---|
| Não vazio após `Trim()` | FR-002 |
| Máximo de 200 caracteres | FR-002 + `agent_docs/architecture.md` §9.2 (confirmado) |
| Espaços nas pontas removidos antes da validação | Edge case da spec |

### Email

| Regra | Origem |
|---|---|
| Formato de e-mail válido | FR-003 |
| Normalizado para lowercase no construtor | FR-004 (unicidade case-insensitive) |
| Espaços nas pontas removidos antes da validação | Edge case da spec |
| Dois `Email` com capitalização de origem diferente são iguais entre si (`Equals`/`GetHashCode` sobre o valor normalizado) | FR-004 + edge case da spec |

### Password (transiente — nunca persistido)

| Regra | Origem |
|---|---|
| Mínimo de 8 caracteres | FR-005 |
| Ao menos uma letra maiúscula | FR-005 |
| Ao menos uma letra minúscula | FR-005 |
| Ao menos um dígito | FR-005 |
| Máximo de 100 caracteres | Edge case da spec (proteção contra input gigante) + `agent_docs/architecture.md` §9.2 (confirmado) |

Existe apenas durante o fluxo de criação (`UserService.CreateUserAsync`): validado, hasheado, descartado. Nunca chega ao repositório, a logs ou a qualquer resposta HTTP.

### PasswordHash (persistido)

| Regra | Origem |
|---|---|
| String não vazia | FR-006 |
| Opaco — não conhece o algoritmo de hashing usado para gerá-lo | `agent_docs/architecture.md` §1 |

## Contracts (borda HTTP)

**Local**: `FitProgress.Domain/Contracts/V1/Users/`

### CreateUserRequest (Requests/CreateUserRequest.cs)

| Campo | Tipo |
|---|---|
| `Name` | `string` |
| `Email` | `string` |
| `Password` | `string` |

### UserResponse (Responses/UserResponse.cs)

| Campo | Tipo |
|---|---|
| `Id` | `Guid` |
| `Name` | `string` |
| `Email` | `string` |
| `CreatedAt` | `DateTimeOffset` |

Nunca inclui `PasswordHash` nem qualquer derivado da senha (FR-006, SC-002).

## Resultado de aplicação (não é modelo de dados, mas parte do contrato interno)

**Local**: `FitProgress.Application/Results/CreateUserResult.cs`

Três estados possíveis, todos desfechos de negócio esperados (não exceção):

- `Success(UserResponse)`
- `ValidationFailed(IReadOnlyList<(string Campo, string Mensagem)>)` — contém **todas** as violações encontradas, não apenas a primeira.
- `EmailAlreadyInUse`

## Persistência (PostgreSQL)

**Tabela**: `users` (schema completo e rationale em `agent_docs/architecture.md` §5)

| Coluna | Tipo | Constraint |
|---|---|---|
| `id` | `UUID` | `PRIMARY KEY` |
| `name` | `VARCHAR(200)` | `NOT NULL` |
| `email` | `VARCHAR(320)` | `NOT NULL` |
| `password_hash` | `TEXT` | `NOT NULL` |
| `created_at` | `TIMESTAMPTZ` | `NOT NULL DEFAULT now()` |

Índice: `CREATE UNIQUE INDEX ux_users_email_lower ON users (lower(email));` — garantia física de FR-004, inclusive contra corrida de concorrência (duas requisições simultâneas com o mesmo e-mail).

## Relacionamentos

Nenhum nesta feature. `User` é uma entidade isolada; futuras features (treino, histórico) referenciarão `User.Id`, mas isso está fora do escopo de `001-user-registration`.
