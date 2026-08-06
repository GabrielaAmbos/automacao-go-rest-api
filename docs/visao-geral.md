# Visão geral

## O que é este projeto

Projeto de **automação de testes de API REST** desenvolvido como desafio técnico de QA.
Ele valida o CRUD de usuários da API pública do [Go REST](https://gorest.co.in),
exercitando os quatro verbos HTTP principais em cenários de sucesso e de falha.

Não é uma aplicação com código de produção: todo o código do repositório existe para
executar e sustentar a suíte de testes.

## API sob teste

A suíte aponta para a versão legada da API do Go REST:

```
https://gorest.co.in/public-api
```

O recurso exercitado é `/users`.

> ⚠️ **Essa versão foi descontinuada.** Hoje ela responde `301` redirecionando para
> `https://gorest.co.in/public/v2`, que tem contrato diferente. É por isso que os 7 testes
> falham. Veja [Pontos de atenção](pontos-de-atencao.md#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte).
> O restante desta seção descreve o contrato da v1, que é o que o código ainda assume.

### Particularidade importante: código de status no corpo

Essa versão da API responde **HTTP 200 na maior parte das chamadas**, e informa o
resultado real da operação dentro do corpo da resposta, no campo `code`:

```json
{
  "code": 200,
  "meta": null,
  "data": {
    "id": 4392,
    "name": "Leroy Borgan",
    "email": "leroyborgan123@outlook.com",
    "gender": "Male",
    "status": "Active",
    "created_at": "2021-05-04T12:00:00.000+05:30",
    "updated_at": "2021-05-04T12:00:00.000+05:30"
  }
}
```

Por isso **todas as asserções da suíte verificam `json.Code`**, e não o status HTTP
da resposta do RestSharp. Esse é o motivo de existirem os modelos
[`User`](../Go%20Rest%20API%20Automation/models/User.cs) (envelope) e
[`Data`](../Go%20Rest%20API%20Automation/models/Data.cs) (payload do usuário).

### Autenticação

A API exige um token Bearer no header `Authorization`. O token é fornecido por
[`TokenProvider`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs)
e injetado em toda requisição pelo `ApiClient`.

> O token está fixo no código-fonte. Veja [Pontos de atenção](pontos-de-atencao.md#1-token-de-autenticação-versionado-no-repositório).

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
