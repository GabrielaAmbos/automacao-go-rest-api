# Casos de teste

A suíte tem **7 testes** distribuídos em 4 fixtures, mais a massa compartilhada do `Hooks`.
Todos passam contra a API v2.

## Massa de teste

### `Hooks` — usuário compartilhado somente leitura

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs)

| Hook | Ação |
| --- | --- |
| `[OneTimeSetUp]` | Cria o usuário "Leroy Borgan" via `UserFactory` e expõe em `Hooks.SharedUser` |
| `[OneTimeTearDown]` | Exclui esse usuário |

Consumido apenas por `GetBuscarUsuario`. O e-mail é gerado com sufixo `Guid`, então cada
execução usa um endereço novo.

### `UserFactory` — massa própria por fixture

[`tests/UserFactory.cs`](../Go%20Rest%20API%20Automation/tests/UserFactory.cs)

Fixtures que alteram estado criam e destroem o próprio usuário, sem tocar no compartilhado.

---

## GET — `GetBuscarUsuario`

[`tests/GetBuscarUsuario.cs`](../Go%20Rest%20API%20Automation/tests/GetBuscarUsuario.cs)

### `DeveBuscarUsuario`

Busca o usuário compartilhado e valida o corpo inteiro.

- **Endpoint:** `GET /users/{SharedUser.Id}`
- **Esperado:** `200 OK`
- **Asserções:** `id`, `name`, `email` iguais aos do usuário criado; `gender == "male"`;
  `status == "active"`

### `NaoDeveBuscarUsuario`

Busca um ID negativo, que a API trata como inexistente.

- **Endpoint:** `GET /users/-{SharedUser.Id}`
- **Esperado:** `404 Not Found`

---

## POST — `PostCriarUmUsuario`

[`tests/PostCriarUmUsuario.cs`](../Go%20Rest%20API%20Automation/tests/PostCriarUmUsuario.cs)

### `DeveCriarUmUsuario`

- **Endpoint:** `POST /users`
- **Corpo:**

  ```json
  {
    "name": "Tomas Oliver",
    "gender": "male",
    "email": "tomas.oliver.<guid>@example.test",
    "status": "active"
  }
  ```

- **Esperado:** `201 Created`
- **Asserções:** `id` positivo; `name`, `gender`, `email` e `status` refletem o corpo enviado

### `ExcluirUsuarioCriado` — `[TearDown]`

Exclui o usuário criado, usando o `id` guardado no teste. O guard `createdUserId != 0`
evita chamada inútil quando o teste falha antes de criar.

---

## PUT — `PutAlterarUsuario`

[`tests/PutAlterarUsuario.cs`](../Go%20Rest%20API%20Automation/tests/PutAlterarUsuario.cs)

Cria a própria massa no `[SetUp]` e limpa no `[TearDown]` — o teste altera estado e não
pode usar o usuário compartilhado.

### `DeveAlterarUsuarioExistente`

- **Endpoint:** `PUT /users/{id}`
- **Corpo:**

  ```json
  {
    "name": "Ronald McDonald Jr",
    "email": "ronald.mcdonald.<guid>@example.test",
    "status": "inactive"
  }
  ```

- **Esperado:** `200 OK`
- **Asserções:** `id` inalterado; `name`, `email` e `status` atualizados; **`gender`
  permanece `male`** — não foi enviado no corpo, então valida que a API preserva campos
  omitidos

O `status` muda para `inactive` de propósito: se o teste enviasse o mesmo valor que já
estava lá, a asserção passaria mesmo que o `PUT` não tivesse efeito.

### `NaoDeveAlterarUsuario`

- **Endpoint:** `PUT /users/-{id}`
- **Esperado:** `404 Not Found`

---

## DELETE — `DelExcluirUsuario`

[`tests/DelExcluirUsuario.cs`](../Go%20Rest%20API%20Automation/tests/DelExcluirUsuario.cs)

Também cria a própria massa no `[SetUp]`.

### `DeveExcluirUsuarioExistente`

- **Endpoint:** `DELETE /users/{id}`
- **Esperado:** `204 No Content` (corpo vazio — `response.Data` vem `null`)

### `NaoDeveExcluirUsuarioInexistente`

- **Endpoint:** `DELETE /users/-{id}`
- **Esperado:** `404 Not Found`

O `[TearDown]` roda mesmo depois do teste positivo já ter excluído o usuário: a segunda
chamada devolve `404`, e o `UserFactory.Delete` ignora o resultado de propósito.

---

## Matriz resumida

| Fixture | Teste | Verbo | Status esperado |
| --- | --- | --- | --- |
| `GetBuscarUsuario` | `DeveBuscarUsuario` | GET | `200 OK` |
| `GetBuscarUsuario` | `NaoDeveBuscarUsuario` | GET | `404 Not Found` |
| `PostCriarUmUsuario` | `DeveCriarUmUsuario` | POST | `201 Created` |
| `PutAlterarUsuario` | `DeveAlterarUsuarioExistente` | PUT | `200 OK` |
| `PutAlterarUsuario` | `NaoDeveAlterarUsuario` | PUT | `404 Not Found` |
| `DelExcluirUsuario` | `DeveExcluirUsuarioExistente` | DELETE | `204 No Content` |
| `DelExcluirUsuario` | `NaoDeveExcluirUsuarioInexistente` | DELETE | `404 Not Found` |
