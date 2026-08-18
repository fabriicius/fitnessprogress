# Changelog

## [2026-08-18] - Develop
**PR:** #4 por @fabriicius

### O que mudou
Foram configuradas automações que usam o Claude para ajudar na gestão do repositório: quando alguém abre uma nova issue, o Claude analisa e posta um comentário organizando o pedido (tipo, urgência, possíveis arquivos afetados e sugestões de tarefas). Quando um Pull Request é aberto, o Claude faz uma revisão automática do código e sugere testes para partes que ainda não têm cobertura. E quando um PR é mergeado, o Claude gera automaticamente uma nota de lançamento explicando o que mudou.

### Detalhes técnicos
- Adicionado `.claude/settings.json` com permissões para as Actions (Write, Edit, `git add/commit/push`).
- Adicionado workflow `.github/workflows/claude-code-issues.yml`: dispara em `issues: opened`, roda `anthropics/claude-code-action@v1` para triagem estruturada (classificação, arquivos afetados, subtarefas, issues relacionadas, observações).
- Adicionado workflow `.github/workflows/claude-code-review.yml` com três jobs:
  - `claude-review`: revisão automática de PRs (código aberto por membros/owners/colaboradores), usando o plugin `code-review@claude-code-plugins` com prompt em português focado em segurança (OWASP), N+1 e aderência ao `CLAUDE.md`.
  - Geração automática de testes xUnit para métodos novos sem cobertura, com commit direto na branch do PR.
  - `release-notes`: ao mergear o PR, gera/atualiza este `CHANGELOG.md` e faz push na branch base.

---
