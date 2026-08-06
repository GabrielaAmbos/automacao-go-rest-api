# Pontos de atenção

> **Estado da suíte em 06/08/2026: 68 de 68 testes passando**, verificado em 3 execuções
> seguidas contra a API v2. Os bloqueios anteriores foram resolvidos na migração — o
> registro está em [Resolvidos](#resolvidos), no fim do documento.

Itens em aberto, em ordem de prioridade (segurança > arquitetura > manutenção).

---

## 1. O token antigo continua no histórico do Git

O `TokenProvider` já não guarda credencial: ela vem de `GOREST_TOKEN`. Mas o token que
estava versionado **permanece no histórico** e estava ativo na última verificação.

**Como corrigir:** revogar o token em [gorest.co.in](https://gorest.co.in) e gerar outro.
Remover o valor do código não basta — qualquer clone antigo ou consulta ao histórico ainda
o expõe.

Reescrever o histórico (`git filter-repo`) só faz sentido se o token não for revogado, e
tem o custo de invalidar clones existentes. Revogar é mais simples e mais seguro.

---

## 2. Pacotes NuGet não utilizados

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

O build passa sem warnings mesmo assim, mas são dependências mortas. Remover é seguro e
não afeta a suíte.

---

## 3. Target framework fora de suporte

O projeto tem como alvo `netcoreapp3.1`, cujo suporte terminou em dezembro de 2022. O SDK
3.1 não vem mais em gerenciadores de pacote — é preciso instalá-lo pelo script oficial
(veja [Como executar](como-executar.md#instalando-o-sdk-31)). Em Apple Silicon não há build
nativo.

Migrar para .NET 8 exigiria também atualizar o RestSharp, que mudou de API de forma
incompatível a partir da versão 107 (`IRestResponse` → `RestResponse`, `Execute` →
`ExecuteAsync`). Como o `ApiClient` é o único ponto que toca o RestSharp, o impacto fica
contido em um arquivo.

---

## 4. Lacunas remanescentes de cobertura

Os 4 recursos, a paginação, os filtros e os fluxos de erro estão cobertos. O que ainda
falta:

- **`PATCH` só é testado em `users`.** A v2 aceita o verbo nos outros três recursos
- **Validação `422` só é exaustiva em `users`** (5 cenários). Em posts, comments e todos há
  um ou dois casos, não a matriz completa
- **Filtros só em `users` e `todos`.** Posts e comments também aceitam query por campo
- **Nada valida `page` fora do intervalo** (`?page=99999`) nem `per_page` acima do máximo
- **Concorrência e idempotência** — dois `DELETE` simultâneos, `PUT` repetido
- A suíte é sequencial; NUnit permite paralelizar por fixture, o que cortaria o tempo de
  execução

---

## 5. Asserções de erro só verificam o status

Nos cenários negativos, só o `StatusCode` é verificado. A v2 devolve um corpo útil que
poderia ser assertado:

```json
{"message":"Resource not found"}
```

Não é defeito, mas é asserção barata que documenta melhor o contrato.

---

# Resolvidos

Registrados aqui porque explicam decisões de desenho que, sem contexto, parecem
arbitrárias.

## ✅ API v1 descontinuada — bloqueava toda a suíte

A v1 (`/public-api`) passou a responder `301` para a v2, e o caminho duplicado que o
`Hooks` montava caía num `404` HTML:

```
GET https://gorest.co.in/public-api/public-api/users
→ 301 https://gorest.co.in/public/v2/public-api/users
→ 404, content-type: text/html
```

O `ApiClient` tentava desserializar `<!DOCTYPE html>` como JSON e estourava
`JsonReaderException` dentro do `[OneTimeSetUp]`, derrubando os 7 testes de uma vez.

**Resolvido** migrando para `https://gorest.co.in/public/v2`: `User` e `Data` viraram um
DTO só, as asserções passaram de `json.Code` para `response.StatusCode`, e os valores
esperados de `gender`/`status` foram para minúsculas.

## ✅ Token versionado no repositório

O token literal aparecia em **dois** lugares, não só um:

- `TokenProvider.BasicToken()`, no código-fonte
- `Postman/Go Rest Desafio.postman_collection.json`, em 4 ocorrências (o `auth.bearer` de
  três requisições e o da coleção)

Agora o `TokenProvider` lê `GOREST_TOKEN` e lança `InvalidOperationException` com instrução
de correção quando a variável não existe; a coleção usa a variável `{{token}}`, declarada
vazia no topo.

Falta apenas revogar o token antigo — [item 1](#1-o-token-antigo-continua-no-histórico-do-git).

## ✅ Coleção Postman apontava para a v1

As 5 requisições usavam `{{url}}/public-api/users` e nenhuma funcionava. Os paths foram
migrados para `/public/v2/`, alinhados com a suíte.

## ✅ Endpoints com caminho duplicado e barra dupla

`Hooks` montava `"/public-api/users"` sobre uma base que já continha `/public-api`, e o
teardown gerava `/users//4392` porque `GetId()` já devolvia a barra.

**Resolvido** com `EndpointProvider.Users()` e `EndpointProvider.User(id)` devolvendo
caminhos **relativos**, e a URL base ficando sob responsabilidade exclusiva do
`RestClient`.

## ✅ Acoplamento entre fixtures pela massa compartilhada

`DelExcluirUsuario` excluía o usuário do `Hooks` — o mesmo que `GetBuscarUsuario` e
`PutAlterarUsuario` precisavam. Como o NUnit ordena fixtures alfabeticamente, `Del…` rodava
primeiro e derrubava os outros dois.

**Resolvido** com `UserFactory`: fixtures que alteram estado criam a própria massa no
`[SetUp]` e limpam no `[TearDown]`. `Hooks.SharedUser` ficou restrito a leitura, consumido
só pelo `GET`.

## ✅ Teardown do POST que nunca limpava

`Hooks.GetUsersEndpoint() + LastUserId` produzia `.../users4392` — faltava a barra. Como o
teardown não assertava nada, a falha passava silenciosa e a massa ficava órfã.

**Resolvido** por `EndpointProvider.User(id)`, que monta o caminho corretamente.

## ✅ Dados de teste fixos

E-mails literais no código faziam a segunda execução falhar com `422`, já que a API rejeita
duplicados. **Resolvido** com `UserFactory.UniqueEmail`, que sufixa o endereço com um
`Guid`.

## ✅ `ApiClient` engolia o status HTTP

Devolvia só o objeto desserializado, sem status nem checagem de content-type — o que
transformou um `404` HTML num `JsonReaderException` sem pista da causa.

**Resolvido** com `ApiResponse<T>` (status + corpo), retorno `default(T)` para `204` e
exceção explícita quando a resposta não é JSON, citando status, content-type e URL efetiva.

## ✅ `Hooks` como classe estática — nunca foi problema

Era `public static class` com `[SetUpFixture]`, o que contraria a recomendação do NUnit.
A execução mostrou que o NUnit 3.12 rodava os hooks normalmente — **não era a causa das
falhas**. Foi convertida para classe de instância na migração apenas por alinhamento com a
documentação do framework.
