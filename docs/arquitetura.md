# Arquitetura

## Estrutura de pastas

```
automacao-go-rest-api/
├── Go Rest API Automation/
│   ├── Go Rest API Automation.sln       Solution do Visual Studio
│   ├── Go Rest API Automation.csproj    Projeto e dependências NuGet
│   ├── client/
│   │   └── ApiClient.cs                 Cliente HTTP genérico (RestSharp)
│   ├── models/
│   │   ├── User.cs                      Envelope da resposta (code, meta, data)
│   │   └── Data.cs                      Dados do usuário
│   ├── tests/
│   │   ├── Hooks.cs                     Setup/teardown global da suíte
│   │   ├── GetBuscarUsuario.cs          Testes de GET
│   │   ├── PostCriarUmUsuario.cs        Testes de POST
│   │   ├── PutAlterarUsuario.cs         Testes de PUT
│   │   └── DelExcluirUsuario.cs         Testes de DELETE
│   └── utils/providers/
│       ├── UrlProvider.cs               URL base da API
│       ├── EndpointProvider.cs          Montagem dos endpoints
│       └── TokenProvider.cs             Token de autenticação
├── Postman/
│   └── Go Rest Desafio.postman_collection.json
├── docs/
└── README.md
```

## Camadas

O projeto separa responsabilidades em quatro camadas:

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
apenas informa endpoint, método e corpo, e recebe um objeto já tipado.

## `ApiClient<T>` — o núcleo

[`client/ApiClient.cs`](../Go%20Rest%20API%20Automation/client/ApiClient.cs)

```csharp
public static T Request(string endpoint, Method method = Method.GET, JObject jObjectBody = null)
```

Uma única classe genérica estática concentra toda a comunicação HTTP:

1. Cria o `RestClient` com a URL base do `UrlProvider`
2. Monta o `RestRequest` com o endpoint e o verbo recebidos
3. Anexa o corpo JSON **apenas em POST e PUT**
4. Injeta o header `Authorization` a partir do `TokenProvider`
5. Executa a chamada
6. Desserializa `restResponse.Content` para `T` via Newtonsoft.Json

O parâmetro genérico `T` sempre é `User` na suíte atual, mas a classe aceita qualquer
modelo — é o ponto de extensão para novos recursos da API.

### Assinatura de uso nos testes

```csharp
// GET — método padrão, sem corpo
var json = ApiClient<User>.Request(endpoint);

// POST/PUT — corpo obrigatório
var json = ApiClient<User>.Request(endpoint, Method.POST, jObjectbody);

// DELETE — sem corpo
var json = ApiClient<User>.Request(endpoint, Method.DELETE);
```

## Providers

| Classe | Retorno |
| --- | --- |
| `UrlProvider.BaseUrl()` | `https://gorest.co.in/public-api` |
| `EndpointProvider.Users()` | `BaseUrl() + "/users"` |
| `TokenProvider.BasicToken()` | `"Bearer <token>"` |

Centralizar essas três informações significa que trocar de ambiente, de token ou de
recurso é uma edição em um único arquivo.

> `EndpointProvider.Users()` devolve uma URL absoluta, embora o `RestClient` já receba a
> URL base. Veja [Pontos de atenção](pontos-de-atencao.md#2-url-base-duplicada-na-montagem-do-endpoint).

## Modelos

O envelope da API é mapeado em dois níveis:

```csharp
// models/User.cs — envelope
public class User
{
    [JsonProperty("code")] public int Code { get; set; }
    [JsonProperty("meta")] public string Meta { get; set; }
    [JsonProperty("data")] public Data Data { get; set; }
}

// models/Data.cs — payload
public class Data
{
    [JsonProperty("id")]         public int Id { get; set; }
    [JsonProperty("name")]       public string Name { get; set; }
    [JsonProperty("email")]      public string Email { get; set; }
    [JsonProperty("gender")]     public string Gender { get; set; }
    [JsonProperty("status")]     public string Status { get; set; }
    [JsonProperty("created_at")] public string CreatedAt { get; set; }
    [JsonProperty("updated_at")] public string UpdatedAt { get; set; }
}
```

Os atributos `[JsonProperty]` fazem a ponte entre o `snake_case` da API e o `PascalCase`
do C#.

## Ciclo de vida da suíte — `Hooks`

[`tests/Hooks.cs`](../Go%20Rest%20API%20Automation/tests/Hooks.cs) é anotada com
`[SetUpFixture]`, o que faz o NUnit executá-la uma vez para **todo o namespace**
`Go_Rest_API_Automation.tests`:

| Hook | Método | Ação |
| --- | --- | --- |
| `[OneTimeSetUp]` | `CriarUsuario()` | Cria o usuário "Leroy Borgan" e guarda o `Id` em `LastUserId` |
| — | `GetId()` | Devolve `"/" + LastUserId` para composição do endpoint |
| — | `GetUsersEndpoint()` | Atalho para `EndpointProvider.Users()` |
| `[OneTimeTearDown]` | `DeveExcluirUsuario()` | Exclui o usuário criado no setup |

Esse usuário é a **massa de teste compartilhada**: `GetBuscarUsuario`,
`PutAlterarUsuario` e `DelExcluirUsuario` operam todos sobre ele.

## Fluxo de uma requisição

```
Teste
  │  endpoint = Hooks.GetUsersEndpoint() + Hooks.GetId()
  ▼
ApiClient<User>.Request(endpoint, Method.PUT, body)
  │
  ├─► UrlProvider.BaseUrl()      → RestClient
  ├─► TokenProvider.BasicToken() → header Authorization
  ├─► JObject body               → parameter application/json
  ▼
RestSharp executa a chamada HTTP
  │
  ▼
JsonConvert.DeserializeObject<User>(response.Content)
  │
  ▼
Teste assere: json.Code.Should().Be(200)
```

## Convenções de nomenclatura

O código segue o padrão BDD em português usado no desafio:

- Classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarUmUsuario`
- Cenário positivo: `Deve<Ação>` — `DeveBuscarUsuario`
- Cenário negativo: `NaoDeve<Ação>` — `NaoDeveBuscarUsuario`

Namespaces seguem a estrutura de pastas, com duas exceções herdadas de refatorações:
`UrlProvider` está em `utils.urls` (e não em `utils.providers`), e `User` está em
`client` (e não em `Models`, como `Data`).
