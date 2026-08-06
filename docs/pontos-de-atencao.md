# Pontos de atenção

> **Estado da suíte em 06/08/2026: os 7 testes falham.**
> Causa raiz única e externa ao código: a API alvo foi descontinuada
> ([item 1](#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte)).
> Os demais itens são débitos reais do código, hoje mascarados por essa falha.

Resultado da última execução (`dotnet test`):

```
Total tests: 7
     Failed: 7

OneTimeSetUp: Newtonsoft.Json.JsonReaderException :
Unexpected character encountered while parsing value: <. Path '', line 0, position 0.
   at Go_Rest_API_Automation.client.ApiClient`1.Request(...) in client/ApiClient.cs:line 30
   at Go_Rest_API_Automation.tests.Hooks.CriarUsuario() in tests/Hooks.cs:line 25
```

Itens em ordem de prioridade (bloqueio > segurança > arquitetura > confiabilidade).

---

## 1. A API alvo foi descontinuada — bloqueia toda a suíte

**Onde:** [`utils/providers/UrlProvider.cs:9`](../Go%20Rest%20API%20Automation/utils/providers/UrlProvider.cs#L9)

A versão v1 da API (`/public-api`) não existe mais. Hoje ela responde **301** e redireciona
para a v2:

```
GET https://gorest.co.in/public-api/users
→ 301, location: https://gorest.co.in/public/v2/users
```

O `Hooks` ainda monta o caminho duplicado `base + "/public-api/users"`, e o redirect leva a
uma rota inexistente na v2:

```
GET https://gorest.co.in/public-api/public-api/users
→ 301 https://gorest.co.in/public/v2/public-api/users
→ 404, content-type: text/html
```

O corpo devolvido é `<!DOCTYPE html>...`. O `ApiClient` tenta desserializá-lo como JSON e
lança `JsonReaderException` no `<` inicial — dentro do `[OneTimeSetUp]`, o que derruba
**todos os 7 testes** de uma vez.

O token **continua válido** (verificado: `POST /public/v2/users` sem token → 401, com token
→ 422). Não é problema de autenticação.

### O que a v2 muda

| | v1 (`/public-api`) | v2 (`/public/v2`) |
| --- | --- | --- |
| Status | No corpo, campo `code` | No HTTP da resposta |
| Envelope | `{ code, meta, data }` | Payload direto, sem envelope |
| `GET /users` | Objeto com `data` | Array puro |
| `gender` / `status` | `Male` / `Active` | `male` / `active` (minúsculas) |

Resposta atual da v2:

```json
[{"id":8570443,"name":"Ekadant Achari","email":"ekadant_achari@rempel.test","gender":"male","status":"active"}]
```

### Como corrigir

1. `UrlProvider.BaseUrl()` → `https://gorest.co.in/public/v2`
2. Colapsar `User` e `Data` em um único DTO — a v2 não tem envelope
3. Trocar as asserções de `json.Code` para o status HTTP da resposta, o que exige o
   `ApiClient` devolver o `IRestResponse` (ver [item 8](#8-status-http-nunca-é-verificado))
4. Ajustar os valores esperados para minúsculas (`male`, `active`)
5. Trocar o cenário negativo: `/users/-{id}` deve virar um ID inexistente válido

Não é um ajuste de path — é uma migração de contrato.

---

## 2. Token de autenticação versionado no repositório

**Onde:** [`utils/providers/TokenProvider.cs:11`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs#L11)

O token Bearer está literal no código-fonte e no histórico do Git. Qualquer pessoa com
acesso ao repositório tem a credencial — e ela **está ativa**, como a verificação acima
confirmou.

**Como corrigir:**

```csharp
public static string BasicToken()
{
    return "Bearer " + Environment.GetEnvironmentVariable("GOREST_TOKEN");
}
```

Ainda que seja um token de API pública de testes, remover credenciais do versionamento é
o padrão correto. A remoção do arquivo não basta — o valor precisa ser revogado no
Go REST, já que permanece no histórico.

---

## 3. Endpoint duplicado no `Hooks`

**Onde:** [`tests/Hooks.cs:17`](../Go%20Rest%20API%20Automation/tests/Hooks.cs#L17)
e [`tests/Hooks.cs:43`](../Go%20Rest%20API%20Automation/tests/Hooks.cs#L43)

O `Hooks` monta endpoints à mão, ignorando o `EndpointProvider` que ele mesmo expõe:

```csharp
// OneTimeSetUp — o "/public-api" já está na BaseUrl do RestClient
string endpoint = "/public-api/users";        // → .../public-api/public-api/users

// OneTimeTearDown — GetId() já devolve "/" + id, gerando barra dupla
string endpoint = "/public-api/users/" + GetId();   // → .../public-api/users//4392
```

Ambos deveriam usar `GetUsersEndpoint()`, e o teardown não deveria adicionar a barra que
o `GetId()` já inclui. Esse é o defeito que, somado ao [item 1](#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte),
produz a exceção do `OneTimeSetUp`.

---

## 4. URL base duplicada na montagem do endpoint

**Onde:** [`utils/providers/EndpointProvider.cs:9`](../Go%20Rest%20API%20Automation/utils/providers/EndpointProvider.cs#L9)
e [`client/ApiClient.cs:16-17`](../Go%20Rest%20API%20Automation/client/ApiClient.cs#L16-L17)

`EndpointProvider.Users()` devolve uma URL **absoluta**:

```csharp
return UrlProvider.BaseUrl() + "/users";   // https://gorest.co.in/public-api/users
```

Mas o `ApiClient` já configura a base no `RestClient` e passa o endpoint como recurso:

```csharp
RestClient restClient = new RestClient(UrlProvider.BaseUrl());
RestRequest restRequest = new RestRequest(endpoint, method);
```

O RestSharp acaba recebendo a URL completa como recurso — funciona por acidente, porque a
URL absoluta sobrescreve a base. O `EndpointProvider` deveria devolver apenas o caminho
relativo (`"/users"`), deixando a base sob responsabilidade exclusiva do cliente.

---

## 5. Concatenação de endpoint sem separador no teardown do POST

**Onde:** [`tests/PostCriarUmUsuario.cs:38`](../Go%20Rest%20API%20Automation/tests/PostCriarUmUsuario.cs#L38)

```csharp
string endpoint = Hooks.GetUsersEndpoint() + LastUserId;   // ".../users4392"
```

Falta a barra entre o recurso e o ID. Os demais testes usam `Hooks.GetId()`, que já
inclui o separador. Como o teardown não assere nada, a falha passa silenciosa — o usuário
criado no teste **não é excluído**, e o e-mail fixo `tomasoliver@outlook.com` fica preso
na API, fazendo a próxima execução falhar com `422`.

---

## 6. Acoplamento entre fixtures pela massa compartilhada

**Onde:** [`tests/DelExcluirUsuario.cs:13-20`](../Go%20Rest%20API%20Automation/tests/DelExcluirUsuario.cs#L13-L20)

`DelExcluirUsuario.DeveExcluirUsuarioExistente` exclui o usuário criado pelo `Hooks` —
exatamente o mesmo usuário que `GetBuscarUsuario` e `PutAlterarUsuario` precisam.

O NUnit executa fixtures em ordem alfabética por padrão, então `DelExcluirUsuario` roda
**primeiro** e derruba os testes seguintes. Além disso, o `[OneTimeTearDown]` tenta
excluir um usuário que já não existe.

Hoje esse defeito está mascarado pelo [item 1](#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte):
como o setup falha antes, nenhum teste chega a rodar. Ele reaparece assim que a API for
migrada.

**Como corrigir:** cada fixture que altera ou remove estado deve criar a própria massa,
como `PostCriarUmUsuario` já faz. O `Hooks` deveria fornecer apenas dados de leitura, ou
um método `CreateUser()` reutilizável.

---

## 7. Dados de teste fixos

**Onde:** todos os arquivos de teste

E-mails, nomes e gêneros estão fixos no código. A API do Go REST rejeita e-mails
duplicados, então qualquer execução que não limpe a massa corretamente quebra a próxima.

**Como corrigir:** gerar dados dinâmicos (por exemplo, com [Bogus](https://github.com/bchavez/Bogus))
ou sufixar o e-mail com um timestamp.

---

## 8. Status HTTP nunca é verificado

**Onde:** [`client/ApiClient.cs:28-30`](../Go%20Rest%20API%20Automation/client/ApiClient.cs#L28-L30)

```csharp
IRestResponse restResponse = restClient.Execute(restRequest);

return JsonConvert.DeserializeObject<T>(restResponse.Content);
```

O `ApiClient` descarta o `IRestResponse` e devolve apenas o objeto desserializado. Não há
validação de `StatusCode`, nem tratamento para respostas não-JSON.

É exatamente por isso que a falha do [item 1](#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte)
se manifesta como um `JsonReaderException` críptico em vez de "a API respondeu 404 com
`text/html`". Um guard simples tornaria o diagnóstico imediato:

```csharp
if (!restResponse.ContentType.Contains("json"))
{
    throw new InvalidOperationException(
        $"{(int)restResponse.StatusCode} {restResponse.ContentType} em {restResponse.ResponseUri}");
}
```

Na migração para a v2 esse retorno precisa mudar de qualquer forma, já que as asserções
passarão a depender do status HTTP.

---

## 9. Pacotes NuGet não utilizados

**Onde:** [`Go Rest API Automation.csproj:12-18`](../Go%20Rest%20API%20Automation/Go%20Rest%20API%20Automation.csproj#L12-L18)

```xml
<PackageReference Include="Microsoft.NETCore.App" Version="2.2.8" />
<PackageReference Include="MSTest.TestFramework" Version="2.2.3" />
<PackageReference Include="SpecFlow" Version="3.7.38" />
```

- `MSTest.TestFramework` — o projeto usa NUnit; dois frameworks de teste no mesmo assembly
  podem confundir o test adapter
- `SpecFlow` — não há nenhum `.feature` no repositório
- `Microsoft.NETCore.App` como `PackageReference` é obsoleto desde o .NET Core 3.0 e a
  versão declarada (2.2.8) não bate com o target framework (`netcoreapp3.1`)

O build passa sem warnings mesmo assim, mas são dependências mortas.

---

## 10. Target framework fora de suporte

O projeto tem como alvo `netcoreapp3.1`, cujo suporte terminou em dezembro de 2022. O SDK
3.1 não vem mais em gerenciadores de pacote (o Homebrew só oferece 6, 8 e 9) — é preciso
instalá-lo pelo script oficial. Veja [Como executar](como-executar.md#instalando-o-sdk-31).

Migrar para uma LTS atual (.NET 8) exigiria também atualizar o RestSharp, que mudou de API
de forma incompatível a partir da versão 107 (`IRestResponse` → `RestResponse`,
`Execute` → `ExecuteAsync`).

---

## 11. Inconsistências de namespace

| Classe | Namespace | Esperado |
| --- | --- | --- |
| `UrlProvider` | `utils.urls` | `utils.providers` |
| `User` | `client` | `Models` |
| `Data` | `Models` | ✔ |

Além disso, `UrlProvider` é `internal` (sem modificador explícito) enquanto os demais
providers são `public`.

`models/User.cs` também importa `FluentAssertions`, `System.Collections.Generic` e
`System.Text` sem usar nenhum deles.

---

## Não é problema: `Hooks` como classe estática

`Hooks` é declarada `public static class` com `[SetUpFixture]`, o que contraria a
recomendação da documentação do NUnit (classe não estática, com construtor público sem
parâmetros).

Na prática, **o NUnit 3.12 executa os hooks normalmente** — a execução confirmou que
`CriarUsuario()` roda e que sua exceção propaga corretamente como `OneTimeSetUp` para as
4 fixtures. Não é a causa das falhas atuais e não precisa ser mexido.
