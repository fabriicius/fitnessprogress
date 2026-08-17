# Project Context

FitProgress é uma API REST de fitness e nutrição criada como projeto de portfólio e laboratório de Spec-Driven Development com Claude Code e Spec Kit. O projeto evolui de forma incremental, começando pelo contexto de treino e adicionando novos módulos somente quando houver especificação.

# Stack

- C# / .NET 10 LTS
- ASP.NET Core Web API
- NuGet via .NET CLI
- xUnit para testes unitários
- DDD com arquitetura em camadas
- PostgreSQL hospedado na Neon
- Dapper para acesso a dados
- Autenticação e autorização com JWT
- Docker / OCI container image
- Deploy na Vercel via Vercel Functions / Fluid Compute
- Spec Kit + Claude Code

# Commands

```bash
# Restore
dotnet restore

# Run dev
dotnet run --project src/FitProgress.Api/FitProgress.Api.csproj

# Test
dotnet test

# Format / lint
dotnet format --verify-no-changes

# Compile validation
dotnet build --no-restore

# Build
dotnet build

# Docker
docker build -t fitprogress-api .
docker run --rm -p 8080:8080 --env-file .env fitprogress-api
```

# File Structure

```text
FitProgress/
├── src/
│   ├── FitProgress.Api/              # HTTP, JWT, DI e configuração
│   ├── FitProgress.Application/      # Casos de uso e contratos
│   ├── FitProgress.Domain/           # Entidades, VOs e regras de domínio
│   └── FitProgress.Infrastructure/   # PostgreSQL e integrações
├── tests/
│   └── FitProgress.UnitTests/
├── specs/
├── .specify/
├── .claude/
├── Dockerfile
└── CLAUDE.md
```

# Architectural Boundaries

- `Domain` não referencia nenhuma outra camada.
- `Application` pode depender de `Domain`.
- `Infrastructure` implementa persistência e integrações e pode usar `Application` e `Domain`.
- `Api` coordena `Application` e `Infrastructure` via injeção de dependência.
- Controllers/endpoints não contêm regras de negócio nem acessam PostgreSQL diretamente.
- O Domain não conhece PostgreSQL, SQL, ORM, JWT ou infraestrutura.
- Evite abstrações e dependências não exigidas pela especificação atual.

# Workflow Rules

- A especificação ativa é a fonte primária de requisitos.
- Não implemente comportamento não especificado ou claramente derivado dela.
- Requisitos ambíguos devem virar decisão pendente; não invente regras de negócio.
- Mantenha alterações pequenas e alinhadas à feature em execução.
- Para bugs, crie primeiro um teste que reproduza a falha quando viável.
- Toda regra de domínio relevante deve possuir teste unitário.
- Após alterações, execute `dotnet test`.
- Antes de concluir uma tarefa, execute `dotnet format --verify-no-changes` e `dotnet build --no-restore`.
- Nunca versione secrets, chaves JWT, connection strings ou credenciais.
- Configurações sensíveis devem vir de variáveis de ambiente.
- Não introduza cache, mensageria ou nova infraestrutura sem necessidade especificada.

# Persistence

- PostgreSQL é o banco relacional oficial.
- Produção usa PostgreSQL hospedado na Neon.
- Dapper é a biblioteca oficial de acesso a dados do projeto.
- Connection strings entram por configuração/variáveis de ambiente.
- Queries SQL e detalhes de persistência pertencem à `Infrastructure`.
- O Domain não conhece Dapper, SQL ou detalhes do banco.
- Repositórios devem expor contratos orientados ao domínio/aplicação, evitando vazar detalhes de persistência para camadas superiores.
- Queries devem utilizar parâmetros; nunca concatenar valores externos diretamente em SQL.

# Authentication

- A API utiliza JWT.
- Tokens devem ser validados pelo pipeline de autenticação do ASP.NET Core.
- Segredos de assinatura nunca ficam no código ou repositório.
- Endpoints públicos/protegidos e regras de roles/claims devem ser definidos pelas specs.
- Senhas, se armazenadas futuramente, nunca podem ser persistidas em texto puro.

# Deployment

- A aplicação deve possuir `Dockerfile` reproduzível e sem secrets.
- O container deve funcionar localmente antes do deploy.
- Produção será implantada na Vercel através de container image.
- Porta, banco e secrets devem ser obtidos do ambiente.
- Não dependa de armazenamento persistente no filesystem do container.

# Spec Kit Workflow

```text
/speckit.specify
      ↓
/speckit.clarify
      ↓
/speckit.plan
      ↓
/speckit.tasks
      ↓
/speckit.analyze
      ↓
/speckit.implement
```

- `specify`: defina o que construir e por quê.
- `clarify`: resolva ambiguidades e decisões pendentes.
- `plan`: defina arquitetura, tecnologias e estratégia técnica.
- `tasks`: quebre o plano em tarefas executáveis.
- `analyze`: valide consistência entre spec, plano e tarefas.
- `implement`: implemente somente após os artefatos estarem coerentes.

# Domain Direction

O primeiro contexto é treino. Nutrição e demais módulos entram apenas por novas especificações.

Possíveis evoluções: catálogo de exercícios, fichas, sessões, séries realizadas, histórico de evolução, alimentos, refeições, metas de macronutrientes e medidas corporais.

Esta lista é contexto de produto, não autorização para implementação antecipada.

# TDD Workflow — obrigatório para toda nova feature

Para cada nova funcionalidade, utilize obrigatoriamente a skill `.claude/skills/dotnet-testing` e siga o ciclo **Red → Green → Refactor**.

A implementação deve sempre seguir esta ordem:

1. **Red** — escrever primeiro os testes que representam o comportamento esperado e confirmar que eles falham.
2. **Green** — implementar somente o código necessário para fazer os testes passarem.
3. **Refactor** — melhorar a implementação sem alterar o comportamento e mantendo todos os testes verdes.

## Cobertura mínima por funcionalidade

- **Domain Models** — regras de domínio, validações, estados, Value Objects e comportamentos da entidade.
- **Services** — regras da aplicação, validações de fluxo, chamadas aos Repositories e tratamento dos resultados.
- **Helpers** — validações, conversões e comportamentos auxiliares relevantes.
- **Repositories** — comportamento esperado das operações de persistência quando houver testes aplicáveis, incluindo cenários de sucesso e falha.
- **Controllers** — status HTTP, contratos de entrada/saída e chamada correta do Service quando fizer parte do escopo da feature.
- **Casos de erro** — dados inválidos, recurso inexistente, conflito, exceções e demais cenários definidos pela spec.
- **Critérios de aceite** — cada critério de aceite da feature deve possuir pelo menos um teste que demonstre seu comportamento quando tecnicamente aplicável.

## Regras

- Não escrever a implementação antes dos testes da funcionalidade.
- Não alterar um teste apenas para fazê-lo passar quando o comportamento esperado estiver correto.
- Não remover testes existentes para concluir uma feature.
- Para bugs, primeiro criar um teste que reproduza a falha.
- Testes unitários não devem depender de Neon, internet ou APIs externas.
- Dependências externas devem ser isoladas/mocadas nos testes unitários quando necessário.
- Utilize `async/await` e `CancellationToken` também nos testes de fluxos assíncronos quando aplicável.

Só considere a funcionalidade concluída quando todos os testes relacionados estiverem verdes.

Ao final de cada funcionalidade execute:

```bash
dotnet test
dotnet format --verify-no-changes
dotnet build --no-restore

# Deeper Context

Criar somente quando houver conteúdo real:

- `agent_docs/business-rules.md` — regras de domínio permanentes.
- `agent_docs/security.md` — JWT, autorização, PII e segurança.
- `agent_docs/engineering-standards.md` — qualidade e testes.
- `agent_docs/architecture.md` — decisões arquiteturais e motivos.
- `.claude/rules/` — regras específicas por caminho.

# Keeping This File Current

Mantenha este arquivo curto. Specs de features pertencem a `specs/`; regras detalhadas e decisões permanentes devem ser movidas para os arquivos especializados acima.

# Response Language

Sempre responda em português brasileiro, independentemente do idioma usado no prompt.