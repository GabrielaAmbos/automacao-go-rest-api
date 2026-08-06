# Visão geral

## O que é este projeto

Projeto de **automação de testes de API REST** desenvolvido como desafio técnico de QA.
Ele valida o CRUD de usuários da API pública do [Go REST](https://gorest.co.in),
exercitando os quatro verbos HTTP principais em cenários de sucesso e de falha.

Não é uma aplicação com código de produção: todo o código do repositório existe para
executar e sustentar a suíte de testes.

## API sob teste

```
https://gorest.co.in/public/v2
```

O recurso exercitado é `/users`.

### Formato da resposta

A v2 devolve o payload direto, sem envelope, e usa o **status HTTP** para comunicar o
resultado da operação:

```json
{
  "id": 8571390,
  "name": "Leroy Borgan",
  "email": "leroy.borgan.a1b2c3@example.test",
  "gender": "male",
  "status": "active"
}
```

Por isso as asserções verificam `response.StatusCode` e os campos de `response.Data`,
mapeados pelo modelo [`User`](../Go%20Rest%20API%20Automation/models/User.cs).

Atenção aos valores: `gender` e `status` são **minúsculos** (`male`, `female`, `active`,
`inactive`).

### Status por operação

| Operação | Sucesso | Recurso inexistente |
| --- | --- | --- |
| `GET /users/{id}` | `200 OK` | `404 Not Found` |
| `POST /users` | `201 Created` | — |
| `PUT /users/{id}` | `200 OK` | `404 Not Found` |
| `DELETE /users/{id}` | `204 No Content` (corpo vazio) | `404 Not Found` |

Erros de validação no `POST` retornam `422` com a lista de campos:

```json
[{"field":"email","message":"can't be blank"}]
```

### Autenticação

A API exige um token Bearer no header `Authorization`. O token é lido da variável de
ambiente `GOREST_TOKEN` por
[`TokenProvider`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs)
e injetado em toda requisição pelo `ApiClient`. Sem a variável, a suíte falha logo no
setup com uma mensagem explícita.

Leitura de `/users` é pública; escrita exige o token (sem ele, `401`).

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

## Cobertura funcional

| Operação | Cenário positivo | Cenário negativo |
| --- | --- | --- |
| `GET /users/{id}` | Busca usuário existente (200) | Busca ID inválido (404) |
| `POST /users` | Cria usuário (201) | — |
| `PUT /users/{id}` | Altera usuário existente (200) | Altera ID inválido (404) |
| `DELETE /users/{id}` | Exclui usuário existente (204) | Exclui ID inválido (404) |

Detalhes em [Casos de teste](casos-de-teste.md).

## Histórico do projeto

O repositório evoluiu de testes com requisições duplicadas em cada arquivo para uma
estrutura encapsulada — os commits registram essa progressão:

1. `Encapsulando com a classe Hooks` — extração do setup/teardown compartilhado
2. `Encapsulando os requests na ApiCliente` — centralização das chamadas HTTP
3. `Encapsulando o endpoint` — criação do `EndpointProvider`
4. `refactor: migra a suíte para a API v2 do Go REST` — a v1 (`/public-api`) foi
   descontinuada e passou a responder `301` com HTML, quebrando os 7 testes. A migração
   trocou o envelope `code`/`meta`/`data` pelo payload direto e as asserções de corpo pelo
   status HTTP
