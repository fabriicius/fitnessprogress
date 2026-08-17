# Feature Specification: Cadastro de Usuário

**Feature Branch**: `001-user-registration`

**Created**: 2026-08-16

**Status**: Draft

**Input**: User description: "O usuario ira poder criar um login , com nome , email , senha"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Criar conta com nome, e-mail e senha (Priority: P1)

Um novo usuário acessa o sistema pela primeira vez e cria uma conta informando nome, e-mail e senha, para que possa futuramente acessar a plataforma com essas credenciais.

**Why this priority**: Sem a criação de conta não existe nenhuma outra funcionalidade possível na plataforma. É a base de todo o restante do sistema (treinos, histórico, etc.).

**Independent Test**: Pode ser testado isoladamente enviando nome, e-mail e senha válidos e verificando que a conta é criada com sucesso e que a senha não é armazenada em texto puro.

**Acceptance Scenarios**:

1. **Given** que o usuário ainda não possui conta, **When** ele informa nome, e-mail e senha válidos, **Then** a conta é criada com sucesso e o sistema confirma a criação.
2. **Given** que a conta foi criada, **When** os dados armazenados são inspecionados, **Then** a senha nunca está salva em texto puro (apenas de forma segura/hash).

---

### User Story 2 - Validação dos dados informados (Priority: P2)

Como usuário criando uma conta, quero receber uma mensagem clara quando algum dado informado (nome, e-mail ou senha) for inválido, para que eu possa corrigir e concluir o cadastro.

**Why this priority**: Garante a qualidade dos dados cadastrados e evita contas inconsistentes, mas depende da funcionalidade básica de criação (US1) já existir.

**Independent Test**: Pode ser testado isoladamente enviando combinações de dados inválidos (nome vazio, e-mail em formato inválido, senha fora da política mínima) e verificando que a conta não é criada e uma mensagem de erro compreensível é retornada.

**Acceptance Scenarios**:

1. **Given** que o usuário está criando uma conta, **When** o campo nome é enviado vazio, **Then** o sistema rejeita a criação e informa que o nome é obrigatório.
2. **Given** que o usuário está criando uma conta, **When** o e-mail informado não possui formato válido, **Then** o sistema rejeita a criação e informa que o e-mail é inválido.
3. **Given** que o usuário está criando uma conta, **When** a senha informada não atende à política mínima de segurança, **Then** o sistema rejeita a criação e informa os critérios exigidos.

---

### User Story 3 - Impedir contas duplicadas (Priority: P3)

Como sistema, preciso impedir que dois cadastros usem o mesmo e-mail, para que cada e-mail identifique uma única conta de forma inequívoca.

**Why this priority**: Importante para a integridade dos dados e para o futuro fluxo de login (que usará o e-mail como identificador), mas só é relevante depois que a criação básica (US1) e a validação (US2) já funcionam.

**Independent Test**: Pode ser testado isoladamente criando uma conta com um e-mail e, em seguida, tentando criar outra conta com o mesmo e-mail, verificando que a segunda tentativa é rejeitada com mensagem clara.

**Acceptance Scenarios**:

1. **Given** que já existe uma conta cadastrada com um determinado e-mail, **When** um novo cadastro é tentado com o mesmo e-mail, **Then** o sistema rejeita a criação e informa que o e-mail já está em uso.

---

### Edge Cases

- O que acontece quando o usuário envia o mesmo e-mail com capitalização diferente (ex.: `Usuario@Email.com` vs `usuario@email.com`)? O sistema deve tratar como o mesmo e-mail para fins de duplicidade.
- Como o sistema trata espaços em branco extras no início/fim do nome ou do e-mail? Devem ser removidos antes da validação.
- O que acontece quando a senha enviada é muito longa (ataque de negação de serviço via input gigante)? O sistema deve limitar o tamanho máximo aceito.
- Como o sistema responde quando múltiplos campos são inválidos ao mesmo tempo? Todas as violações relevantes devem ser reportadas, não apenas a primeira encontrada.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema DEVE permitir que um novo usuário crie uma conta informando nome, e-mail e senha.
- **FR-002**: O sistema DEVE validar que o nome não está vazio e não excede um tamanho máximo razoável.
- **FR-003**: O sistema DEVE validar que o e-mail informado possui um formato válido.
- **FR-004**: O sistema DEVE impedir a criação de mais de uma conta com o mesmo e-mail (comparação sem diferenciar maiúsculas/minúsculas).
- **FR-005**: O sistema DEVE exigir que a senha atenda a uma política mínima de segurança (mínimo de 8 caracteres, contendo letras maiúsculas, minúsculas e números).
- **FR-006**: O sistema NUNCA DEVE armazenar a senha em texto puro, apenas de forma segura (hash).
- **FR-007**: O sistema DEVE rejeitar a criação da conta e informar mensagens de erro compreensíveis quando qualquer dado obrigatório estiver ausente ou inválido.
- **FR-008**: O sistema DEVE confirmar ao usuário quando a conta for criada com sucesso.

### Key Entities

- **Usuário**: representa uma pessoa cadastrada na plataforma. Atributos principais: nome, e-mail (identificador único de acesso) e senha (armazenada apenas de forma segura). Servirá de base para futuras funcionalidades de treino associadas a essa conta.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Um novo usuário consegue concluir a criação de conta em menos de 1 minuto.
- **SC-002**: 100% das senhas armazenadas estão em formato seguro (nunca texto puro).
- **SC-003**: 100% das tentativas de cadastro com e-mail duplicado ou dados inválidos são rejeitadas com mensagem de erro compreensível.
- **SC-004**: 95% dos usuários conseguem concluir o cadastro na primeira tentativa sem precisar de suporte.

## Assumptions

- Esta especificação cobre apenas a criação da conta (cadastro). O fluxo de autenticação/login propriamente dito (validar credenciais, emitir token de acesso, manter sessão) será tratado em uma especificação futura.
- Confirmação de e-mail (verificação por link/código) não é exigida nesta primeira versão — a conta fica ativa imediatamente após a criação.
- Política de senha adotada como padrão: mínimo de 8 caracteres, contendo ao menos uma letra maiúscula, uma minúscula e um número.
- Redefinição de senha ("esqueci minha senha") está fora do escopo desta especificação e será tratada futuramente.
- Cada e-mail pode estar associado a no máximo uma conta.
