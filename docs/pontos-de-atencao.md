# Pontos de atenção

Débitos técnicos e riscos identificados no código atual, em ordem de prioridade
(segurança > arquitetura > confiabilidade dos testes).

---

## 1. Token de autenticação versionado no repositório

**Onde:** [`utils/providers/TokenProvider.cs:11`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs#L11)

O token Bearer está literal no código-fonte e no histórico do Git. Qualquer pessoa com
acesso ao repositório tem a credencial.

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

## 2. URL base duplicada na montagem do endpoint

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

O RestSharp acaba recebendo a URL completa como recurso. O `EndpointProvider` deveria
devolver apenas o caminho relativo (`"/users"`), deixando a base sob responsabilidade
exclusiva do cliente.

---

## 3. Endpoint inconsistente no teardown do `Hooks`

**Onde:** [`tests/Hooks.cs:17`](../Go%20Rest%20API%20Automation/tests/Hooks.cs#L17)
e [`tests/Hooks.cs:43`](../Go%20Rest%20API%20Automation/tests/Hooks.cs#L43)

O `Hooks` monta endpoints à mão, ignorando o `EndpointProvider` que ele mesmo expõe:

```csharp
// OneTimeSetUp
string endpoint = "/public-api/users";

// OneTimeTearDown — GetId() já devolve "/" + id, gerando barra dupla
string endpoint = "/public-api/users/" + GetId();   // "/public-api/users//4392"
```

Ambos deveriam usar `GetUsersEndpoint()`, e o teardown não deveria adicionar a barra que
o `GetId()` já inclui.

---

## 4. Concatenação de endpoint sem separador no teardown do POST

**Onde:** [`tests/PostCriarUmUsuario.cs:38`](../Go%20Rest%20API%20Automation/tests/PostCriarUmUsuario.cs#L38)

```csharp
string endpoint = Hooks.GetUsersEndpoint() + LastUserId;   // ".../users4392"
```

Falta a barra entre o recurso e o ID. Os demais testes usam `Hooks.GetId()`, que já
inclui o separador. Como o teardown não assere nada, a falha passa silenciosa — o usuário
criado no teste **não é excluído**, e o e-mail fixo `tomasoliver@outlook.com` fica preso
na API, fazendo a próxima execução falhar com `422`.

---

## 5. Acoplamento entre fixtures pela massa compartilhada

**Onde:** [`tests/DelExcluirUsuario.cs:13-20`](../Go%20Rest%20API%20Automation/tests/DelExcluirUsuario.cs#L13-L20)

`DelExcluirUsuario.DeveExcluirUsuarioExistente` exclui o usuário criado pelo `Hooks` —
exatamente o mesmo usuário que `GetBuscarUsuario` e `PutAlterarUsuario` precisam.

O NUnit executa fixtures em ordem alfabética por padrão, então `DelExcluirUsuario` roda
**primeiro** e derruba os testes seguintes. Além disso, o `[OneTimeTearDown]` tenta
excluir um usuário que já não existe.

**Como corrigir:** cada fixture que altera ou remove estado deve criar a própria massa,
como `PostCriarUmUsuario` já faz. O `Hooks` deveria fornecer apenas dados de leitura, ou
um método `CreateUser()` reutilizável.

---

## 6. `Hooks` declarada como classe estática

**Onde:** [`tests/Hooks.cs:10`](../Go%20Rest%20API%20Automation/tests/Hooks.cs#L10)

```csharp
[SetUpFixture]
public static class Hooks
```

O NUnit espera que um `[SetUpFixture]` seja uma classe **não estática** com construtor
público sem parâmetros — em C#, `static class` compila como `abstract sealed`. Se os
hooks não estiverem disparando, essa é a primeira coisa a verificar.

A alternativa é tornar a classe não estática e separar os helpers (`GetId()`,
`GetUsersEndpoint()`) em uma classe de contexto própria.

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

O `ApiClient` descarta o `IRestResponse` e devolve apenas o objeto desserializado. Não há
validação de `restResponse.StatusCode`, nem tratamento para respostas vazias ou não-JSON
— nesses casos `JsonConvert.DeserializeObject` devolve `null` e o teste quebra com
`NullReferenceException` em vez de uma mensagem útil.

Como a API legada codifica o resultado no corpo, checar o `code` é válido — mas o cliente
deveria pelo menos falhar de forma explícita quando a resposta não for desserializável.

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

---

## 10. API alvo descontinuada

A suíte aponta para `https://gorest.co.in/public-api`, que é a versão legada. A API atual
do Go REST é `https://gorest.co.in/public/v2`, com contrato diferente:

- Retorna o status real no HTTP, sem o envelope `code`/`meta`/`data`
- Responde `201`, `404`, `422` diretamente

Migrar exige reescrever os modelos (`User`/`Data` viram um único DTO) e trocar as
asserções de `json.Code` para o status da resposta.

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
