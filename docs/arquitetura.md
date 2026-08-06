# Arquitetura

## Estrutura de pastas

```
automacao-go-rest-api/
├── Go Rest API Automation/
│   ├── Go Rest API Automation.sln       Solution do Visual Studio
│   ├── Go Rest API Automation.csproj    Projeto e dependências NuGet
│   ├── client/
│   │   ├── ApiClient.cs                 Cliente HTTP genérico (RestSharp)
│   │   └── ApiResponse.cs               Status HTTP + corpo + headers
│   ├── models/
│   │   ├── User.cs                      DTO do usuário
│   │   ├── Post.cs                      DTO do post
│   │   ├── Comment.cs                   DTO do comentário
│   │   ├── Todo.cs                      DTO da tarefa
│   │   ├── ValidationError.cs           Item do corpo de erro 422
│   │   └── ErrorMessage.cs              Corpo de erro 401/404
│   ├── tests/
│   │   ├── Hooks.cs                     Massa compartilhada somente leitura
│   │   ├── UserFactory.cs               ┐
│   │   ├── PostFactory.cs               │ criação/exclusão de massa
│   │   ├── CommentFactory.cs            │ por fixture
│   │   ├── TodoFactory.cs               ┘
│   │   ├── users/                       6 fixtures
│   │   ├── posts/                       5 fixtures
│   │   ├── comments/                    5 fixtures
│   │   ├── todos/                       5 fixtures
│   │   └── errors/                      autenticação e validação
│   └── utils/
│       ├── PaginationHeader.cs          Nomes dos headers x-pagination-*
│       ├── Query.cs                     Montagem de query string
│       └── providers/
│           ├── UrlProvider.cs           URL base da API
│           ├── EndpointProvider.cs      Caminhos relativos dos recursos
│           └── TokenProvider.cs         Token vindo de GOREST_TOKEN
├── Postman/
│   └── Go Rest Desafio.postman_collection.json
├── docs/
└── README.md
```

## Camadas

```
┌─────────────────────────────────────────────┐
│  tests/          Cenários e asserções       │
│                  (o que deve acontecer)     │
├─────────────────────────────────────────────┤
│  tests/*Factory  Massa de teste             │
└───────────────────────┬─────────────────────┘
                        │
┌───────────────────────▼─────────────────────┐
│  client/         ApiClient<T>               │
│                  (como a chamada é feita)   │
└───────┬───────────────────────────┬─────────┘
        │                           │
┌───────▼─────────┐    ┌────────────▼─────────┐
│  utils/         │    │  models/             │
│  URL, endpoint, │    │  Desserialização da  │
│  token, query   │    │  resposta            │
└─────────────────┘    └──────────────────────┘
```

Um teste nunca monta uma requisição HTTP na mão — informa endpoint, método, corpo e query,
e recebe status, headers e objeto já tipado.

## `ApiClient<T>` — o núcleo

[`client/ApiClient.cs`](../Go%20Rest%20API%20Automation/client/ApiClient.cs)

Três entradas públicas, todas delegando ao mesmo `Send` privado:

```csharp
// caminho normal: token vem do TokenProvider
ApiResponse<T> Request(string endpoint, Method method = Method.GET,
                       JObject jObjectBody = null,
                       IDictionary<string, string> queryParameters = null)

// cenários de autenticação
ApiResponse<T> RequestWithToken(string endpoint, string authorization, ...)
ApiResponse<T> RequestWithoutToken(string endpoint, ...)
```

`RequestWithToken` e `RequestWithoutToken` existem porque os testes de `401` precisam
enviar um header errado ou nenhum. Métodos nomeados deixam a intenção explícita no ponto
de chamada — melhor do que um parâmetro `token = null` cujo significado seria ambíguo.

O `Send` monta o `RestClient` com a URL base, injeta `Authorization` quando houver,
adiciona os query parameters, anexa o corpo JSON e devolve `ApiResponse<T>`.

### Desserialização defensiva

A v1 da API foi descontinuada de um dia para o outro e passou a responder HTML — o cliente
antigo estourava um `JsonReaderException` críptico. O `Deserialize` privado trata os dois
casos que causam isso:

```csharp
if (string.IsNullOrWhiteSpace(restResponse.Content))
{
    return default(T);          // 204 No Content do DELETE
}

if (restResponse.ContentType == null || !restResponse.ContentType.Contains("json"))
{
    throw new InvalidOperationException(
        "Expected a JSON response but got " + restResponse.StatusCode +
        " (" + restResponse.ContentType + ") from " + restResponse.ResponseUri);
}
```

Se a API mudar de novo, o erro aponta status, content-type e URL efetiva.

### `ApiResponse<T>`

[`client/ApiResponse.cs`](../Go%20Rest%20API%20Automation/client/ApiResponse.cs)

```csharp
public HttpStatusCode StatusCode { get; }
public T Data { get; }
public string Header(string name)
public int IntHeader(string name)
```

`Header`/`IntHeader` existem para os testes de paginação, que asseram sobre
`x-pagination-*`. O dicionário é case-insensitive.

### Tipando o `T` conforme a resposta

O mesmo cliente atende quatro formatos, só variando o parâmetro genérico:

```csharp
ApiClient<User>.Request(...)                    // item
ApiClient<List<User>>.Request(...)              // coleção
ApiClient<ErrorMessage>.Request(...)            // {"message": "..."}  → 401/404
ApiClient<List<ValidationError>>.Request(...)   // [{"field","message"}] → 422
```

## Providers e utilitários

| Classe | Papel |
| --- | --- |
| `UrlProvider.BaseUrl()` | `https://gorest.co.in/public/v2` |
| `EndpointProvider` | Caminhos **relativos** de todos os recursos |
| `TokenProvider.BasicToken()` | `"Bearer " + $GOREST_TOKEN` |
| `PaginationHeader` | Constantes `x-pagination-page/limit/pages/total` |
| `Query.Page(page, perPage)` / `Query.Filter(field, value)` | Query string |

### `EndpointProvider`

```csharp
Users()              /users            Posts()            /posts
User(id)             /users/{id}       Post(id)           /posts/{id}
UserPosts(userId)    /users/{id}/posts UserTodos(userId)  /users/{id}/todos
Comments()           /comments         Todos()            /todos
Comment(id)          /comments/{id}    Todo(id)           /todos/{id}
PostComments(postId) /posts/{id}/comments
```

`TokenProvider` lança `InvalidOperationException` com instrução de correção quando
`GOREST_TOKEN` não está definida — falha rápida e legível em vez de um `401` genérico.

## Modelos

Um DTO por recurso, sem envelope (a v2 devolve o payload direto), mais dois modelos de
erro:

```csharp
User    { Id, Name, Email, Gender, Status }
Post    { Id, UserId, Title, Body }
Comment { Id, PostId, Name, Email, Body }
Todo    { Id, UserId, Title, DueOn, Status }

ErrorMessage    { Message }           // 401 e 404
ValidationError { Field, Message }    // itens do array de 422
```

Os atributos `[JsonProperty]` fazem a ponte entre o `snake_case` da API (`user_id`,
`due_on`) e o `PascalCase` do C#.

## Gestão de massa de teste

O ponto mais delicado da suíte, porque a API é compartilhada e persistente.

### Factories

Uma por recurso, todas com a mesma forma:

```csharp
UserFactory.Create(name)                  UserFactory.Create(name, gender, status)
PostFactory.Create(userId)                PostFactory.Create(userId, title, body)
CommentFactory.Create(postId)             CommentFactory.Create(postId, name, body)
TodoFactory.Create(userId)                TodoFactory.Create(userId, title, status)

<X>Factory.Delete(id)
<X>Factory.Body(...)                      // JObject reutilizável nos testes
UserFactory.UniqueEmail(name)
```

Dois detalhes que sustentam a repetibilidade:

- **`UniqueEmail` sufixa com `Guid`.** A API rejeita e-mail duplicado; dados fixos fariam a
  segunda execução falhar com `422`.
- **`Delete` ignora o resultado.** Excluir algo já removido devolve `404` e não deve
  quebrar o teardown.

Como posts, comentários e tarefas são criados por rotas aninhadas, a massa vem em cadeia:
usuário → post → comentário.

### Escopo por tipo de teste

| Situação | Onde a massa é criada |
| --- | --- |
| Teste só lê | `[OneTimeSetUp]` da fixture (ou `Hooks.SharedUser`) |
| Teste altera ou remove | `[SetUp]`, com limpeza no `[TearDown]` |

### `Hooks` — massa compartilhada

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs) é um `[SetUpFixture]` do
namespace `Go_Rest_API_Automation.tests`, o que o faz valer também para os sub-namespaces
(`tests.users`, `tests.posts`, …). Cria um usuário no `[OneTimeSetUp]` e o exclui no
`[OneTimeTearDown]`.

Só fixtures que **não alteram estado** o consomem. Essa separação existe por um motivo
concreto: numa versão anterior, `DelExcluirUsuario` apagava a massa compartilhada e — como
o NUnit ordena fixtures alfabeticamente e `Del…` vem antes de `Get…` e `Put…` — derrubava
os dois.

## Fluxo de uma requisição

```
Teste
  │  var response = ApiClient<List<User>>.Request(
  │      EndpointProvider.Users(), queryParameters: Query.Page(1, 5))
  ▼
ApiClient<List<User>>.Request → Send
  │
  ├─► UrlProvider.BaseUrl()      → RestClient
  ├─► TokenProvider.BasicToken() → header Authorization
  ├─► Query.Page(1, 5)           → ?page=1&per_page=5
  ├─► JObject body               → parameter application/json
  ▼
RestSharp executa a chamada HTTP
  │
  ▼
Deserialize: corpo vazio? não-JSON? → default(T) ou exceção explícita
  │
  ▼
ApiResponse<List<User>> { StatusCode, Data, Headers }
  │
  ▼
Teste assere: response.IntHeader(PaginationHeader.Limit).Should().Be(5)
```

## Convenções de nomenclatura

- Namespace espelha a pasta: `tests.users`, `tests.posts`, `tests.comments`, `tests.todos`,
  `tests.errors`
- Classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarPost`, `PutAlterarTarefa`
- Cenário positivo: `Deve<Ação>` / negativo: `NaoDeve<Ação>`
- Cenário de recurso inexistente usa ID negativo (`/users/-{id}`), que a API trata como
  não encontrado
