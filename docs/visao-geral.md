# Visão geral

## O que é este projeto

Projeto de **automação de testes de API REST** desenvolvido como desafio técnico de QA.
Ele valida a API pública do [Go REST](https://gorest.co.in) — os quatro recursos do
domínio e os fluxos de erro — com 68 testes.

Não é uma aplicação com código de produção: todo o código do repositório existe para
executar e sustentar a suíte de testes.

## API sob teste

```
https://gorest.co.in/public/v2
```

Quatro recursos, com relacionamento entre eles:

```
user ─┬─► post ──► comment
      └─► todo
```

| Recurso | Coleção | Item | Rota aninhada |
| --- | --- | --- | --- |
| users | `/users` | `/users/{id}` | — |
| posts | `/posts` | `/posts/{id}` | `/users/{id}/posts` |
| comments | `/comments` | `/comments/{id}` | `/posts/{id}/comments` |
| todos | `/todos` | `/todos/{id}` | `/users/{id}/todos` |

Posts, comentários e tarefas **só podem ser criados pela rota aninhada** — é ela que
estabelece o vínculo com o pai.

### Formato da resposta

A v2 devolve o payload direto, sem envelope, e usa o **status HTTP** para comunicar o
resultado:

```json
{
  "id": 8571390,
  "name": "Leroy Borgan",
  "email": "leroy.borgan.a1b2c3@example.test",
  "gender": "male",
  "status": "active"
}
```

Atenção aos valores: `gender` e `status` são **minúsculos** (`male`, `female`, `active`,
`inactive`). Em `todos`, `status` é `pending` ou `completed`.

### Status por operação

| Operação | Sucesso | Inexistente | Corpo inválido |
| --- | --- | --- | --- |
| `GET /{recurso}` | `200 OK` | — | — |
| `GET /{recurso}/{id}` | `200 OK` | `404` | — |
| `POST` (rota aninhada) | `201 Created` | — | `422` |
| `PUT` / `PATCH` | `200 OK` | `404` | `422` |
| `DELETE` | `204 No Content` (corpo vazio) | `404` | — |

### Corpos de erro

```json
// 401 e 404
{"message": "Resource not found"}

// 422 — array, um item por campo
[{"field": "email", "message": "has already been taken"}]
```

### Paginação e filtros

Toda coleção aceita `?page=` e `?per_page=`, e devolve a paginação em headers:

```
x-pagination-page: 2
x-pagination-limit: 5
x-pagination-pages: 590
x-pagination-total: 2946
```

Filtros por campo funcionam via query: `?status=inactive`, `?gender=female`, `?email=…`.

### Autenticação — o detalhe que mais surpreende

A API exige um token Bearer no header `Authorization`, lido da variável de ambiente
`GOREST_TOKEN` por
[`TokenProvider`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs).

**Recursos criados pelo seu token ficam restritos a ele.** Sem token, a API responde
`404 Resource not found` em vez de `401` — ela não vaza a existência do registro:

| Requisição | Sem token |
| --- | --- |
| `GET /users` (coleção de seed) | `200` |
| `GET /users/{id}` de um usuário que você criou | `404` |
| `GET /posts/{id}` de um post de seed | `200` |
| `GET /posts/{id}` de um post que você criou | `404` |
| `POST /users` | `401 Authentication failed` |
| `POST /users` com token falso | `401 Invalid token` |

O `401` só aparece em escrita na coleção. Esse comportamento está coberto pela fixture
`AutenticacaoApi`.

### Rate limit

`x-ratelimit-limit: 300` por minuto. A suíte completa consome bem menos que isso.

## Stack

| Componente | Versão | Papel |
| --- | --- | --- |
| .NET Core | 3.1 | Target framework do projeto |
| NUnit | 3.12.0 | Framework de testes (atributos, ciclo de vida, runner) |
| NUnit3TestAdapter | 3.16.1 | Descoberta dos testes pelo `dotnet test` / Visual Studio |
| RestSharp | 106.11.7 | Cliente HTTP |
| Newtonsoft.Json | transitiva | Serialização e desserialização do JSON |
| FluentAssertions | 5.10.3 | Asserções legíveis (`Should().Be(...)`) |
| Microsoft.NET.Test.Sdk | 16.9.4 | Infraestrutura de execução de testes |

O `.csproj` também referencia `MSTest.TestFramework` e `SpecFlow`, mas **nenhum dos dois
é usado** pelo código atual.

## Cobertura

| Grupo | Fixtures | Testes | O que cobre |
| --- | --- | --- | --- |
| users | 6 | 17 | CRUD completo, `PATCH`, listagem, paginação e 3 filtros |
| posts | 5 | 13 | CRUD, rota aninhada, lista vazia, `422` |
| comments | 5 | 12 | CRUD, rota aninhada, `422` de e-mail inválido |
| todos | 5 | 13 | CRUD, rota aninhada, filtro por status, `422` |
| errors | 2 | 13 | `401`, escopo do token e as 5 regras de validação de usuário |
| **Total** | **22** | **68** | |

Detalhes em [Casos de teste](casos-de-teste.md).

## Histórico do projeto

1. `Encapsulando com a classe Hooks` — extração do setup/teardown compartilhado
2. `Encapsulando os requests na ApiCliente` — centralização das chamadas HTTP
3. `Encapsulando o endpoint` — criação do `EndpointProvider`
4. `refactor: migra a suíte para a API v2 do Go REST` — a v1 (`/public-api`) foi
   descontinuada e passou a responder `301` com HTML, quebrando os 7 testes. A migração
   trocou o envelope `code`/`meta`/`data` pelo payload direto e as asserções de corpo pelo
   status HTTP
5. `test: cobre posts, comments, todos e os fluxos de erro` — de 7 para 68 testes
