# Arquitetura

## Estrutura de pastas

```
automacao-go-rest-api/
├── Go Rest API Automation/
│   ├── Go Rest API Automation.sln       Solution do Visual Studio
│   ├── Go Rest API Automation.csproj    Projeto e dependências NuGet
│   ├── client/
│   │   ├── ApiClient.cs                 Cliente HTTP genérico (RestSharp)
│   │   └── ApiResponse.cs               Status HTTP + corpo desserializado
│   ├── models/
│   │   └── User.cs                      DTO do usuário
│   ├── tests/
│   │   ├── Hooks.cs                     Massa compartilhada somente leitura
│   │   ├── UserFactory.cs               Criação/exclusão de massa por fixture
│   │   ├── GetBuscarUsuario.cs          Testes de GET
│   │   ├── PostCriarUmUsuario.cs        Testes de POST
│   │   ├── PutAlterarUsuario.cs         Testes de PUT
│   │   └── DelExcluirUsuario.cs         Testes de DELETE
│   └── utils/providers/
│       ├── UrlProvider.cs               URL base da API
│       ├── EndpointProvider.cs          Caminhos relativos dos recursos
│       └── TokenProvider.cs             Token vindo de GOREST_TOKEN
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
│  token          │    │  resposta            │
└─────────────────┘    └──────────────────────┘
```

O objetivo do desenho é que **um teste nunca monte uma requisição HTTP na mão** — ele
apenas informa endpoint, método e corpo, e recebe status e objeto já tipado.

## `ApiClient<T>` — o núcleo

[`client/ApiClient.cs`](../Go%20Rest%20API%20Automation/client/ApiClient.cs)

```csharp
public static ApiResponse<T> Request(string endpoint, Method method = Method.GET, JObject jObjectBody = null)
```

1. Cria o `RestClient` com a URL base do `UrlProvider`
2. Monta o `RestRequest` com o endpoint relativo e o verbo recebidos
3. Injeta o header `Authorization` a partir do `TokenProvider`
4. Anexa o corpo JSON quando houver
5. Executa a chamada
6. Devolve `ApiResponse<T>` com o `StatusCode` e o corpo desserializado

### Desserialização defensiva

A v1 da API foi descontinuada de um dia para o outro e passou a responder HTML — o cliente
antigo estourava um `JsonReaderException` críptico. O `Deserialize` privado agora trata os
dois casos que causam isso:

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

[`client/ApiResponse.cs`](../Go%20Rest%20API%20Automation/client/ApiResponse.cs) — wrapper
imutável que carrega o que os testes precisam:

```csharp
public HttpStatusCode StatusCode { get; }
public T Data { get; }
```

Existe porque a v2 comunica o resultado pelo status HTTP, e o `IRestResponse` cru
exporia detalhes do RestSharp aos testes sem necessidade.

### Uso nos testes

```csharp
// GET — método padrão, sem corpo
var response = ApiClient<User>.Request(EndpointProvider.User(id));

// POST/PUT — com corpo
var response = ApiClient<User>.Request(EndpointProvider.Users(), Method.POST, jObjectBody);

// DELETE — sem corpo, Data vem null (204)
var response = ApiClient<User>.Request(EndpointProvider.User(id), Method.DELETE);
```

## Providers

| Classe | Retorno |
| --- | --- |
| `UrlProvider.BaseUrl()` | `https://gorest.co.in/public/v2` |
| `EndpointProvider.Users()` | `/users` |
| `EndpointProvider.User(id)` | `/users/{id}` |
| `TokenProvider.BasicToken()` | `"Bearer " + $GOREST_TOKEN` |

Os endpoints são **relativos**: a URL base é responsabilidade exclusiva do `RestClient`.

`TokenProvider` lança `InvalidOperationException` com instrução de correção quando
`GOREST_TOKEN` não está definida — falha rápida e legível em vez de um `401` genérico.

## Modelo

```csharp
// models/User.cs
public class User
{
    [JsonProperty("id")]     public int Id { get; set; }
    [JsonProperty("name")]   public string Name { get; set; }
    [JsonProperty("email")]  public string Email { get; set; }
    [JsonProperty("gender")] public string Gender { get; set; }
    [JsonProperty("status")] public string Status { get; set; }
}
```

Um único DTO — a v2 não tem envelope. Os atributos `[JsonProperty]` fazem a ponte entre o
JSON da API e o `PascalCase` do C#.

## Gestão de massa de teste

Duas peças, com responsabilidades distintas:

### `UserFactory` — massa por fixture

[`tests/UserFactory.cs`](../Go%20Rest%20API%20Automation/tests/UserFactory.cs)

```csharp
public static User Create(string name)
public static void Delete(int id)
public static string UniqueEmail(string name)
```

`UniqueEmail` sufixa o endereço com um `Guid`. A API rejeita e-mail duplicado, então dados
fixos fariam a segunda execução falhar com `422` — o sufixo torna a suíte repetível.

`Delete` ignora o resultado: excluir um usuário já removido devolve `404` e não deve
quebrar o teardown.

### `Hooks` — massa compartilhada somente leitura

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs), anotada com
`[SetUpFixture]`, roda uma vez para todo o namespace `Go_Rest_API_Automation.tests`:

| Hook | Ação |
| --- | --- |
| `[OneTimeSetUp]` | Cria o usuário compartilhado e expõe em `Hooks.SharedUser` |
| `[OneTimeTearDown]` | Exclui esse usuário |

**Só `GetBuscarUsuario` usa `SharedUser`**, e apenas para leitura. Fixtures que alteram ou
removem estado (`PUT`, `DELETE`) criam a própria massa no `[SetUp]` e limpam no
`[TearDown]`.

Essa separação existe por um motivo concreto: na versão anterior, `DelExcluirUsuario`
apagava a massa compartilhada e — como o NUnit ordena fixtures alfabeticamente e `Del…`
vem antes de `Get…` e `Put…` — derrubava os dois.

## Fluxo de uma requisição

```
Teste
  │  var response = ApiClient<User>.Request(EndpointProvider.User(id), Method.PUT, body)
  ▼
ApiClient<User>.Request
  │
  ├─► UrlProvider.BaseUrl()      → RestClient
  ├─► TokenProvider.BasicToken() → header Authorization
  ├─► JObject body               → parameter application/json
  ▼
RestSharp executa a chamada HTTP
  │
  ▼
Deserialize: corpo vazio? não-JSON? → default(T) ou exceção explícita
  │
  ▼
ApiResponse<User> { StatusCode, Data }
  │
  ▼
Teste assere: response.StatusCode.Should().Be(HttpStatusCode.OK)
```

## Convenções de nomenclatura

- Classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarUmUsuario`
- Cenário positivo: `Deve<Ação>` — `DeveBuscarUsuario`
- Cenário negativo: `NaoDeve<Ação>` — `NaoDeveBuscarUsuario`
- Cenário negativo usa ID negativo (`/users/-{id}`), que a API trata como não encontrado

Namespaces seguem a estrutura de pastas: `client`, `models`, `tests`, `utils.providers`.
