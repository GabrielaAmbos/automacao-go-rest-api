# Casos de teste

A suíte tem **7 testes** distribuídos em 4 fixtures, mais o setup/teardown global.

## Setup e teardown globais — `Hooks`

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs)

### `CriarUsuario()` — `[OneTimeSetUp]`

Executa **uma vez antes de qualquer teste** do namespace. Cria a massa compartilhada:

| Campo | Valor |
| --- | --- |
| `name` | `Leroy Borgan` |
| `gender` | `Male` |
| `email` | `leroyborgan123@outlook.com` |
| `status` | `Active` |

O `id` retornado fica em `LastUserId` e é exposto pelo `GetId()`.

### `DeveExcluirUsuario()` — `[OneTimeTearDown]`

Executa **uma vez após todos os testes**, excluindo o usuário criado no setup.

---

## GET — `GetBuscarUsuario`

[`tests/GetBuscarUsuario.cs`](../Go%20Rest%20API%20Automation/tests/GetBuscarUsuario.cs)

### `DeveBuscarUsuario`

Busca o usuário compartilhado e valida o corpo inteiro.

- **Endpoint:** `/users/{LastUserId}`
- **Método:** `GET`
- **Esperado:** `code == 200`
- **Asserções:** `name`, `gender`, `email` e `status` iguais aos valores criados no `Hooks`

### `NaoDeveBuscarUsuario`

Busca um ID inválido — o teste concatena um `-` antes do ID, produzindo algo como
`/users/-4392`.

- **Endpoint:** `/users/-{LastUserId}`
- **Método:** `GET`
- **Esperado:** `code == 404`

---

## POST — `PostCriarUmUsuario`

[`tests/PostCriarUmUsuario.cs`](../Go%20Rest%20API%20Automation/tests/PostCriarUmUsuario.cs)

### `DeveCriarUmUsuario`

Único teste da suíte que cria a própria massa, em vez de usar a do `Hooks`.

- **Endpoint:** `/users`
- **Método:** `POST`
- **Corpo:**

  ```json
  {
    "name": "Tomas Oliver",
    "gender": "Male",
    "email": "tomasoliver@outlook.com",
    "status": "Active"
  }
  ```

- **Esperado:** `code == 201`
- **Asserções:** `name`, `gender`, `email` e `status` refletem o corpo enviado

### `DeveExcluirUsuarioExistente` — `[TearDown]`

Limpeza local da fixture: exclui o usuário criado no teste acima usando o `LastUserId`
guardado no campo de instância.

> Esse teardown roda após **cada** teste da fixture e não valida o resultado da exclusão.
> Veja [Pontos de atenção](pontos-de-atencao.md#4-concatenação-de-endpoint-sem-separador-no-teardown-do-post).

---

## PUT — `PutAlterarUsuario`

[`tests/PutAlterarUsuario.cs`](../Go%20Rest%20API%20Automation/tests/PutAlterarUsuario.cs)

### `DeveAlterarUsuarioExistente`

Altera nome e e-mail do usuário compartilhado.

- **Endpoint:** `/users/{LastUserId}`
- **Método:** `PUT`
- **Corpo:**

  ```json
  {
    "name": "Ronald McDonald",
    "email": "ronaldmcdonald@hotmail.com",
    "status": "Active"
  }
  ```

- **Esperado:** `code == 200`
- **Asserções:** `name`, `email` e `status` atualizados; `gender` permanece `Male`
  (não foi enviado no corpo, valida que a API preserva o campo)

### `NaoDeveAlterarUsuario`

Tenta alterar um ID inválido.

- **Endpoint:** `/users/-{LastUserId}`
- **Método:** `PUT`
- **Esperado:** `code == 404`

---

## DELETE — `DelExcluirUsuario`

[`tests/DelExcluirUsuario.cs`](../Go%20Rest%20API%20Automation/tests/DelExcluirUsuario.cs)

### `DeveExcluirUsuarioExistente`

Exclui o usuário compartilhado criado pelo `Hooks`.

- **Endpoint:** `/users/{LastUserId}`
- **Método:** `DELETE`
- **Esperado:** `code == 204`

> Este teste **destrói a massa usada pelas outras fixtures**. Veja
> [Pontos de atenção](pontos-de-atencao.md#5-acoplamento-entre-fixtures-pela-massa-compartilhada).

### `NaoDeveExcluirUsuarioInexistente`

- **Endpoint:** `/users/-{LastUserId}`
- **Método:** `DELETE`
- **Esperado:** `code == 404`

---

## Matriz resumida

| Fixture | Teste | Verbo | `code` esperado |
| --- | --- | --- | --- |
| `GetBuscarUsuario` | `DeveBuscarUsuario` | GET | 200 |
| `GetBuscarUsuario` | `NaoDeveBuscarUsuario` | GET | 404 |
| `PostCriarUmUsuario` | `DeveCriarUmUsuario` | POST | 201 |
| `PutAlterarUsuario` | `DeveAlterarUsuarioExistente` | PUT | 200 |
| `PutAlterarUsuario` | `NaoDeveAlterarUsuario` | PUT | 404 |
| `DelExcluirUsuario` | `DeveExcluirUsuarioExistente` | DELETE | 204 |
| `DelExcluirUsuario` | `NaoDeveExcluirUsuarioInexistente` | DELETE | 404 |
