[![Typing SVG](https://readme-typing-svg.demolab.com?font=Fira+Code&size=18&pause=1000&color=DF62F7&width=435&lines=Projeto+de+Automa%C3%A7%C3%A3o+Go+Rest+API)](https://git.io/typing-svg)

# Go REST API Automation

End-to-end test automation for the [Go REST](https://gorest.co.in) public API, written in
**C#** with **RestSharp** and **NUnit**.

**68 tests across 22 fixtures**, covering all four API resources plus pagination, filtering,
authentication and validation. The suite runs against the live API — there are no mocks.

```
Test Run Successful.
Total tests: 68
     Passed: 68
 Total time: 1.2106 Minutes
```

> Detailed documentation lives in [`docs/`](docs/) and is written in Portuguese.

---

## Tech stack

| Component | Version | Role |
| --- | --- | --- |
| .NET Core | 3.1 | Target framework |
| NUnit | 3.12.0 | Test framework and lifecycle |
| RestSharp | 106.11.7 | HTTP client |
| Newtonsoft.Json | transitive | JSON serialization |
| FluentAssertions | 5.10.3 | Readable assertions |

---

## Getting started

### 1. Install the .NET Core 3.1 SDK

.NET Core 3.1 reached end of support in December 2022 and is no longer available through
package managers (Homebrew only ships 6, 8 and 9). Use the official script — it installs
into `~/.dotnet` without `sudo` and without touching the rest of your system:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 3.1 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

> On Apple Silicon there is no native 3.1 build — run under Rosetta or retarget the project.

### 2. Get an API token

1. Sign in at [gorest.co.in](https://gorest.co.in)
2. Copy your access token
3. Export it (**without** the `Bearer ` prefix — `TokenProvider` adds it):

```bash
export GOREST_TOKEN=<your-token>
```

The token is **never stored in the repository**. If the variable is missing, the suite fails
immediately with an actionable message instead of a generic `401`:

```
System.InvalidOperationException : Environment variable GOREST_TOKEN is not set.
Get a token at https://gorest.co.in and export it before running the tests.
```

### 3. Run the tests

```bash
cd "Go Rest API Automation"
dotnet restore
dotnet build
dotnet test
```

---

## Running specific tests

```bash
# One resource group
dotnet test --filter "FullyQualifiedName~tests.posts"
dotnet test --filter "FullyQualifiedName~tests.errors"

# One fixture
dotnet test --filter "FullyQualifiedName~GetBuscarUsuario"

# One test
dotnet test --filter "Name=DeveBuscarUsuario"

# Verbose output
dotnet test --logger "console;verbosity=detailed"

# Machine-readable report (TestResults/execucao.trx)
dotnet test --logger "trx;LogFileName=execucao.trx"
```

Running from Visual Studio or Rider works too — just make sure `GOREST_TOKEN` is set in the
environment used to launch the test runner.

---

## Test coverage

| Group | Fixtures | Tests | What it covers |
| --- | --- | --- | --- |
| `users` | 6 | 17 | Full CRUD, `PATCH`, listing, pagination, three filters |
| `posts` | 5 | 13 | CRUD via nested route, empty list, `422` validation |
| `comments` | 5 | 12 | CRUD via nested route, invalid e-mail handling |
| `todos` | 5 | 13 | CRUD via nested route, status filtering |
| `errors` | 2 | 13 | `401` scenarios, token scoping, five `422` validation rules |

### Naming convention

Test names are in Portuguese and follow a consistent pattern:

| Prefix | Meaning | Example |
| --- | --- | --- |
| `Deve…` | *should* — happy path | `DeveBuscarUsuario` — should fetch the user |
| `NaoDeve…` | *should not* — failure path | `NaoDeveBuscarUsuario` — should not fetch a missing user |

Fixtures are named `<Verb><Action>`: `GetBuscarUsuario`, `PostCriarPost`, `PutAlterarTarefa`,
`DelExcluirComentario`.

### Highlighted tests

**`users/GetListarUsuarios`** — pagination and filtering, asserting on the `x-pagination-*`
response headers:

```csharp
[Test]
public void DeveRespeitarOTamanhoDaPagina()
{
    var response = ApiClient<List<User>>.Request(
        EndpointProvider.Users(), queryParameters: Query.Page(1, 5));

    response.StatusCode.Should().Be(HttpStatusCode.OK);
    response.Data.Should().HaveCount(5);
    response.IntHeader(PaginationHeader.Limit).Should().Be(5);
    response.IntHeader(PaginationHeader.Page).Should().Be(1);
}
```

**`users/PutAlterarUsuario`** — sends a `PUT` without `gender` and asserts the API preserves
the omitted field rather than nulling it out. Inline comments in this codebase are written in
Portuguese:

```csharp
response.Data.Status.Should().Be("inactive");
// gender nao foi enviado no corpo, a API deve preservar o valor original
response.Data.Gender.Should().Be("male");
```

**`errors/ValidacaoUsuario`** — asserts the exact `422` payload, not just the status code:

```csharp
response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
response.Data[0].Field.Should().Be("email");
response.Data[0].Message.Should().Be("has already been taken");
```

**`errors/AutenticacaoApi`** — covers a non-obvious API rule described [below](#api-behavior-worth-knowing).

---

## Project structure

```
Go Rest API Automation/
├── client/
│   ├── ApiClient.cs         Generic HTTP client — every request goes through it
│   └── ApiResponse.cs       StatusCode + Data + response headers
├── models/                  User, Post, Comment, Todo, ErrorMessage, ValidationError
├── tests/
│   ├── Hooks.cs             [SetUpFixture] — shared READ-ONLY fixture data
│   ├── *Factory.cs          User/Post/Comment/Todo test-data factories
│   ├── users/  posts/  comments/  todos/  errors/
├── utils/
│   ├── PaginationHeader.cs  x-pagination-* header names
│   ├── Query.cs             Query-string builders
│   └── providers/           UrlProvider, EndpointProvider, TokenProvider
Postman/                     Equivalent manual collection
docs/                        Technical documentation (Portuguese)
```

### Design notes

**Every request goes through `ApiClient<T>`.** Tests never instantiate `RestClient`
directly. The generic parameter adapts to the shape of the response:

```csharp
ApiClient<User>.Request(...)                    // single item
ApiClient<List<User>>.Request(...)              // collection
ApiClient<ErrorMessage>.Request(...)            // {"message": "..."} → 401 / 404
ApiClient<List<ValidationError>>.Request(...)   // [{"field","message"}] → 422
```

**Test data is scoped by intent.** Read-only data is created once per fixture in
`[OneTimeSetUp]`; data a test mutates or deletes is created per test in `[SetUp]` and cleaned
up in `[TearDown]`. This keeps fixtures independent — NUnit runs them alphabetically, and an
earlier fixture deleting shared data would break later ones.

**Every generated e-mail is unique.** `UserFactory.UniqueEmail()` appends a `Guid` because
the API rejects duplicates with `422`. This is what makes the suite repeatable.

---

## API behavior worth knowing

The API is `https://gorest.co.in/public/v2`. Resources are related:

```
user ─┬─► post ──► comment
      └─► todo
```

Posts, comments and todos **can only be created through their nested route**
(`POST /users/{id}/posts`, `POST /posts/{id}/comments`, `POST /users/{id}/todos`) — that is
what establishes the parent link.

### Status codes

`GET` → `200` · `POST` → `201` · `PUT` / `PATCH` → `200` · `DELETE` → `204` (empty body) ·
missing resource → `404` · invalid payload → `422` · unauthenticated collection write → `401`

`gender` and `status` are lowercase (`male`, `female`, `active`, `inactive`); todo `status` is
`pending` or `completed`.

### Resources you create are private to your token

This one is easy to get wrong. **Without a token the API returns `404`, not `401`** — it does
not leak whether the record exists. `401` only appears on collection writes.

| Request without a token | Response |
| --- | --- |
| `GET /users` or `GET /posts` (collection) | `200` |
| `GET /users/{id}` for a user you created | `404` |
| `GET /posts/{id}` for a seed post | `200` |
| `GET /posts/{id}` for a post you created | `404` |
| `POST /users` | `401 Authentication failed` |
| `POST /users` with an invalid token | `401 Invalid token` |

### Rate limit

`x-ratelimit-limit: 300` per minute. A full run makes roughly 150 calls, so two runs in quick
succession get close to the ceiling.

---

## Troubleshooting

| Symptom | Likely cause |
| --- | --- |
| `InvalidOperationException: Environment variable GOREST_TOKEN is not set` | Variable not exported in the shell running the tests |
| Everything fails with `401 Unauthorized` | Expired or invalid token — generate a new one |
| `InvalidOperationException: Expected a JSON response but got …` | The API returned HTML. The message includes status, content-type and effective URL |
| `DeveCriarUmUsuario` fails with `422` | Validation error. E-mails are unique per run, so check `gender`/`status` values (they must be lowercase) |

To inspect the API directly:

```bash
API=https://gorest.co.in/public/v2

# reads are public
curl -s -w "\nHTTP %{http_code}\n" "$API/users"

# token check without creating data: 401 = invalid, 422 = valid
curl -s -w "\nHTTP %{http_code}\n" -X POST \
  -H "Authorization: Bearer $GOREST_TOKEN" \
  -H "Content-Type: application/json" -d '{}' "$API/users"
```

---

## Postman collection

`Postman/Go Rest Desafio.postman_collection.json` mirrors the suite for manual exploration.
Import it and fill in the collection variables:

| Variable | Value |
| --- | --- |
| `url` | `https://gorest.co.in` (pre-filled) |
| `token` | your Go REST token — **intentionally empty**, so no credential is committed |
| `LastUserId` | ID of an existing user |

---

## Documentation

Full technical documentation is in [`docs/`](docs/) — written in Portuguese.

| Document | Contents |
| --- | --- |
| [Visão geral](docs/visao-geral.md) | Project, API contract, authentication, coverage |
| [Arquitetura](docs/arquitetura.md) | Layers, `ApiClient`, factories, request flow |
| [Casos de teste](docs/casos-de-teste.md) | All 68 scenarios in detail |
| [Como executar](docs/como-executar.md) | Setup, execution, Postman, common failures |
| [Histórico de execuções](docs/historico-de-execucoes.md) | Every suite run, failures and root causes |
| [Pontos de atenção](docs/pontos-de-atencao.md) | Open technical debt |

---

## License

[MIT](LICENSE)
