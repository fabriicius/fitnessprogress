---
name: backend-stand-vs-csproj-conflict
description: backend-stand.md says repository/service interfaces consumed by Application live in Infrastructure, but that does not compile against the existing .csproj reference graph — pending human confirmation.
metadata:
  type: project
---

`.claude/rules/backend-stand.md` (mandatory path-scoped rule for `src/**/*.cs`, `tests/**/*.cs`) documents interfaces like `IUserRepository` as living exclusively in `FitProgress.Infrastructure/IRepositories/<Domain>/`, with `Application` services (e.g. `UserService`) taking them as constructor dependencies.

This is impossible to compile against the actual `.csproj` graph already in the repo:
- `FitProgress.Application.csproj` references only `FitProgress.Domain.csproj`.
- `FitProgress.Infrastructure.csproj` references `FitProgress.Domain.csproj` **and** `FitProgress.Application.csproj`.

So Infrastructure depends on Application, not the reverse — Application cannot see a type declared in Infrastructure. Flipping the reference (Application → Infrastructure) would create a circular project reference (Infrastructure already → Application) and would violate `CLAUDE.md`'s explicit boundary ("Application pode depender de Domain" — Domain only).

**Resolution proposed in `agent_docs/architecture.md` (feature 001, user registration)**: declare interfaces like `IUserRepository` and `IPasswordHasher` in `FitProgress.Domain/Abstractions/` instead (new folder), keeping their concrete implementations in Infrastructure exactly where `backend-stand.md` says (`Infrastructure/Repositories/...`, `Infrastructure/Security/...`). This is the classic DDD/Clean Architecture pattern (port in the inner layer, adapter in the outer layer) and compiles cleanly with the existing project graph. Only the *interface's folder* diverges from the literal text of `backend-stand.md`; everything else about the rule file is followed.

**Why this matters**: this is not resolved yet — it was surfaced as a "Pendências e riscos" item (section 9.1) in `agent_docs/architecture.md` for the user to confirm before `/speckit.tasks`, not silently decided. `backend-stand.md` itself has not been updated to reflect this.

**How to apply**: until the user explicitly confirms a different resolution, default to putting any interface that Application-layer services need to consume (repository contracts, technical/infra ports like password hashing, future external API client contracts) in `FitProgress.Domain/Abstractions/`, with implementations in Infrastructure as `backend-stand.md` describes. Every future feature that introduces a new repository or infra port will hit this same conflict — check whether the user has since amended `backend-stand.md` or the `.csproj` graph before assuming this default still applies. See [[user-registration-architecture-doc]].
