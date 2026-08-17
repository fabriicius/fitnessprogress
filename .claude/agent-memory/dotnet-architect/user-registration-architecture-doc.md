---
name: user-registration-architecture-doc
description: Technical decisions doc for feature 001 (user registration) lives at agent_docs/architecture.md — feeds /speckit.plan.
metadata:
  type: project
---

For feature `001-user-registration` (spec at `specs/001-user-registration/spec.md`), the technical implementation decisions (domain modeling, password hashing via `PasswordHasher<TUser>`, hand-rolled validation instead of FluentValidation, Service+Repository pattern per `backend-stand.md`, Dapper + DbUp migration strategy, Controllers not Minimal API, NuGet packages per layer, test strategy) are written up in `agent_docs/architecture.md`.

**Why**: this was produced specifically to feed `/speckit.plan` and `/speckit.tasks` for this feature — read it (not just this memory) before planning, since it contains full rationale and trade-offs, plus a "Pendências e riscos" section with open items that need human confirmation: the `IUserRepository`/`IPasswordHasher` interface-location conflict (see [[backend-stand-vs-csproj-conflict]]), undefined max lengths for `Name` (proposed 200) and `Password` (proposed 100) that the spec left as gaps, whether to adopt DbUp for schema migrations, and whether to use Testcontainers.PostgreSql for integration tests.

**How to apply**: when asked to plan, implement, or review this feature, treat `agent_docs/architecture.md` as the authoritative technical-decisions source layered on top of `CLAUDE.md` and `backend-stand.md` — but verify its open pendências were actually resolved by the user (check for updates to the doc or explicit confirmation in conversation) before assuming e.g. DbUp or Testcontainers were adopted as-is.
