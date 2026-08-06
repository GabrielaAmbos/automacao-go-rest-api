# Casos de teste

**68 testes** em 22 fixtures, cobrindo os 4 recursos da API v2 e os fluxos de erro.

| Grupo | Fixtures | Testes |
| --- | --- | --- |
| [users](#users) | 6 | 17 |
| [posts](#posts) | 5 | 13 |
| [comments](#comments) | 5 | 12 |
| [todos](#todos) | 5 | 13 |
| [errors](#errors) | 2 | 13 |

## Massa de teste

### `Hooks` — usuário compartilhado somente leitura

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs), `[SetUpFixture]` do namespace
`Go_Rest_API_Automation.tests` — vale também para os sub-namespaces (`tests.users`,
`tests.posts`, …).

| Hook | Ação |
| --- | --- |
| `[OneTimeSetUp]` | Cria o usuário "Leroy Borgan" e expõe em `Hooks.SharedUser` |
| `[OneTimeTearDown]` | Exclui esse usuário |

Consumido apenas por fixtures que **não alteram estado**.

### Factories — massa própria por fixture

`UserFactory`, `PostFactory`, `CommentFactory` e `TodoFactory` criam e destroem a massa das
fixtures que mutam dados. Todo e-mail é gerado com sufixo `Guid`, o que torna a suíte
repetível.

Regra aplicada em todas as fixtures:

- massa **somente leitura** → `[OneTimeSetUp]` / `[OneTimeTearDown]`
- massa que o teste **altera ou remove** → `[SetUp]` / `[TearDown]`

---

# users

## `GetBuscarUsuario` — 2 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveBuscarUsuario` | `GET /users/{id}` | `200` + corpo completo do usuário compartilhado |
| `NaoDeveBuscarUsuario` | `GET /users/-{id}` | `404` + `{"message":"Resource not found"}` |

## `GetListarUsuarios` — 6 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveListarUsuarios` | `GET /users` | `200`, lista não vazia, `x-pagination-total` positivo |
| `DeveRespeitarOTamanhoDaPagina` | `GET /users?page=1&per_page=5` | 5 itens, `x-pagination-limit` = 5 |
| `DeveNavegarEntrePaginas` | `GET /users?page=1` e `?page=2` | páginas sem IDs em comum |
| `DeveFiltrarPorStatus` | `GET /users?status=inactive` | todos os itens `inactive` |
| `DeveFiltrarPorGenero` | `GET /users?gender=female` | todos os itens `female` |
| `DeveFiltrarPorEmail` | `GET /users?email={email}` | exatamente 1 resultado |

## `PostCriarUmUsuario` — 2 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveCriarUmUsuario` | `POST /users` | `201`, `id` positivo, corpo ecoado |
| `DeveCriarUsuarioInativoDoGeneroFeminino` | `POST /users` (`female`/`inactive`) | `201` com os valores enviados |

## `PutAlterarUsuario` — 2 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveAlterarUsuarioExistente` | `PUT /users/{id}` sem `gender` | `200`, campos atualizados, `gender` **preservado** |
| `NaoDeveAlterarUsuario` | `PUT /users/-{id}` | `404` |

## `PatchAlterarUsuario` — 2 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveAlterarApenasOStatus` | `PATCH /users/{id}` só com `status` | `200`, `status` novo, `name`/`email`/`gender` intactos |
| `NaoDeveAlterarUsuarioInexistente` | `PATCH /users/-{id}` | `404` |

## `DelExcluirUsuario` — 3 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveExcluirUsuarioExistente` | `DELETE /users/{id}` | `204`, corpo vazio (`Data` null) |
| `NaoDeveExcluirUsuarioInexistente` | `DELETE /users/-{id}` | `404` |
| `NaoDeveBuscarUsuarioDepoisDeExcluir` | `DELETE` e depois `GET` | `404` — confirma que a exclusão teve efeito |

---

# posts

Posts são criados pela rota aninhada `POST /users/{userId}/posts`.

## `GetBuscarPost` — 2 testes

`DeveBuscarPost` (`200`, valida `user_id`, `title` e `body`) e `NaoDeveBuscarPost` (`404`).

## `GetListarPosts` — 4 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveListarPosts` | `GET /posts` | `200` + paginação |
| `DeveRespeitarOTamanhoDaPagina` | `GET /posts?per_page=5` | 5 itens |
| `DeveListarPostsDoUsuario` | `GET /users/{id}/posts` | exatamente o post criado |
| `DeveRetornarListaVaziaParaUsuarioSemPosts` | `GET /users/{id}/posts` | `200` com lista vazia |

## `PostCriarPost` — 3 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveCriarPostParaUsuario` | `POST /users/{id}/posts` | `201`, `user_id` correto |
| `NaoDeveCriarPostSemCamposObrigatorios` | corpo `{}` | `422` com `title` e `body` em branco |
| `NaoDeveCriarPostParaUsuarioInexistente` | `POST /users/-{id}/posts` | `422` com erro no campo `user` |

## `PutAlterarPost` — 2 testes

`DeveAlterarPostExistente` (`200`, altera só `title` e valida que `body` é preservado) e
`NaoDeveAlterarPostInexistente` (`404`).

## `DelExcluirPost` — 2 testes

`DeveExcluirPostExistente` (`204`) e `NaoDeveExcluirPostInexistente` (`404`).

---

# comments

Comentários são criados pela rota aninhada `POST /posts/{postId}/comments`, o que exige
uma cadeia usuário → post → comentário na massa.

## `GetBuscarComentario` — 2 testes

`DeveBuscarComentario` (`200`, valida `post_id`, `name`, `email` e `body`) e
`NaoDeveBuscarComentario` (`404`).

## `GetListarComentarios` — 3 testes

`DeveListarComentarios` (`200` + paginação), `DeveRespeitarOTamanhoDaPagina` e
`DeveListarComentariosDoPost` (`GET /posts/{id}/comments` devolve só o comentário criado).

## `PostCriarComentario` — 3 testes

| Teste | Esperado |
| --- | --- |
| `DeveCriarComentarioNoPost` | `201`, `post_id` correto |
| `NaoDeveCriarComentarioComEmailInvalido` | `422` com `email` → `is invalid` |
| `NaoDeveCriarComentarioSemCamposObrigatorios` | `422` com `name` e `body` em branco |

## `PutAlterarComentario` — 2 testes

`DeveAlterarComentarioExistente` (`200`, altera só `body` e valida que `name` é preservado)
e `NaoDeveAlterarComentarioInexistente` (`404`).

## `DelExcluirComentario` — 2 testes

`DeveExcluirComentarioExistente` (`204`) e `NaoDeveExcluirComentarioInexistente` (`404`).

---

# todos

Tarefas são criadas pela rota aninhada `POST /users/{userId}/todos`.

## `GetBuscarTarefa` — 2 testes

`DeveBuscarTarefa` (`200`, valida `user_id`, `title`, `status` e `due_on`) e
`NaoDeveBuscarTarefa` (`404`).

## `GetListarTarefas` — 4 testes

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `DeveListarTarefas` | `GET /todos` | `200` + paginação |
| `DeveRespeitarOTamanhoDaPagina` | `GET /todos?per_page=5` | 5 itens |
| `DeveFiltrarPorStatus` | `GET /todos?status=completed` | todos os itens `completed` |
| `DeveListarTarefasDoUsuario` | `GET /users/{id}/todos` | exatamente a tarefa criada |

## `PostCriarTarefa` — 3 testes

| Teste | Esperado |
| --- | --- |
| `DeveCriarTarefaParaUsuario` | `201`, `user_id` e `due_on` corretos |
| `NaoDeveCriarTarefaComStatusInvalido` | `422` no campo `status` |
| `NaoDeveCriarTarefaSemCamposObrigatorios` | `422` com `title` em branco |

## `PutAlterarTarefa` — 2 testes

`DeveConcluirTarefa` (`pending` → `completed`, valida que `title` é preservado) e
`NaoDeveAlterarTarefaInexistente` (`404`).

## `DelExcluirTarefa` — 2 testes

`DeveExcluirTarefaExistente` (`204`) e `NaoDeveExcluirTarefaInexistente` (`404`).

---

# errors

## `AutenticacaoApi` — 8 testes

Cobrem uma regra da API que não é óbvia: **recursos criados pelo seu token ficam restritos
a ele**. Sem token a API responde `404`, não `401`, para não vazar a existência do
registro. O `401` só aparece em escrita na coleção.

| Teste | Requisição | Esperado |
| --- | --- | --- |
| `NaoDeveCriarUsuarioSemToken` | `POST /users` sem header | `401` + `Authentication failed` |
| `NaoDeveCriarUsuarioComTokenInvalido` | `POST /users` com token falso | `401` + `Invalid token` |
| `DeveListarUsuariosSemToken` | `GET /users` | `200` — a coleção de seed é pública |
| `NaoDeveBuscarUsuarioSemToken` | `GET /users/{id}` sem header | `404` |
| `NaoDeveAlterarUsuarioSemToken` | `PUT /users/{id}` sem header | `404` + usuário intacto na verificação seguinte |
| `NaoDeveExcluirUsuarioSemToken` | `DELETE /users/{id}` sem header | `404` + usuário intacto |
| `NaoDeveBuscarPostCriadoPeloTokenSemAutenticacao` | `GET /posts/{id}` sem header | `404` |
| `DeveBuscarPostPublicoSemToken` | `GET /posts/{id}` de um post de seed | `200` |

Os dois testes de escrita não param no status: confirmam com token que a tentativa não
teve efeito colateral. Isso os mantém independentes da ordem de execução.

## `ValidacaoUsuario` — 5 testes

| Teste | Corpo enviado | Esperado |
| --- | --- | --- |
| `NaoDeveCriarUsuarioSemCamposObrigatorios` | `{}` | `422` com exatamente 4 erros: `email`, `name`, `gender`, `status` |
| `NaoDeveCriarUsuarioComEmailDuplicado` | e-mail já cadastrado | `422` + `has already been taken` |
| `NaoDeveCriarUsuarioComEmailInvalido` | `nao-eh-email` | `422` + `is invalid` |
| `NaoDeveCriarUsuarioComGeneroInvalido` | `gender: "outro"` | `422` no campo `gender` |
| `NaoDeveCriarUsuarioComStatusInvalido` | `status: "quase-ativo"` | `422` no campo `status` |
