# CLAUDE.md

Contexto do projeto para o Claude Code.

## O que é este projeto

Automação de testes de API REST em **C# (.NET Core 3.1)** contra a API pública do
[Go REST](https://gorest.co.in/public-api). Valida o CRUD de usuários com 7 testes
cobrindo GET, POST, PUT e DELETE em cenários positivos e negativos.

Não há código de aplicação — tudo no repositório existe para sustentar a suíte de testes.

## Stack

- **NUnit 3.12** — runner e ciclo de vida
- **RestSharp 106.11.7** — cliente HTTP
- **FluentAssertions 5.10.3** — asserções
- **Newtonsoft.Json** — serialização (dependência transitiva do RestSharp)

`MSTest.TestFramework` e `SpecFlow` estão no `.csproj` mas **não são usados**.

## Estrutura

```
Go Rest API Automation/
├── client/ApiClient.cs        Cliente HTTP genérico — todo request passa por aqui
├── models/User.cs             Envelope da resposta (code, meta, data)
├── models/Data.cs             Payload do usuário
├── tests/Hooks.cs             [SetUpFixture] — cria/exclui a massa compartilhada
├── tests/*.cs                 Fixtures por verbo HTTP
└── utils/providers/           UrlProvider, EndpointProvider, TokenProvider
Postman/                       Coleção equivalente para chamadas manuais
docs/                          Documentação técnica
```

## Comandos

```bash
cd "Go Rest API Automation"
dotnet restore
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~GetBuscarUsuario"
```

## Convenções do projeto

- Nomes de classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarUmUsuario`
- Cenário positivo: `Deve<Ação>` / negativo: `NaoDeve<Ação>`
- Cenário negativo é sempre feito prefixando `-` ao ID: `/users/-{id}`
- Todo request vai por `ApiClient<T>.Request(endpoint, method, body)` — nunca instanciar
  `RestClient` direto num teste
- URL, endpoint e token só saem dos providers em `utils/providers/`

## Preferências do usuário

- **Falar sempre em português** com o usuário
- **Código em inglês** (variáveis, funções, classes, arquivos), **comentários em português**
- Evitar comentários ao máximo — só para decisão de negócio, nunca para explicar o óbvio
- Explicações curtas: "tomei a decisão de fazer XYZ porque ABC"
- Commits em Conventional Commits com mensagem em português: `feat(auth): adiciona X`
- Trabalhar direto na `main`, commitar e dar push ao terminar edições
- Aplicar SOLID, evitar over engineering
- Prioridade nas decisões: segurança > arquitetura > funcionalidade > usabilidade

## Estado atual: a suíte está quebrada

Verificado em 06/08/2026 com `dotnet test`: **build passa, 7 de 7 testes falham.**

```
OneTimeSetUp: Newtonsoft.Json.JsonReaderException :
Unexpected character encountered while parsing value: <.
```

Causa raiz externa ao código: a API v1 (`/public-api`) foi descontinuada e hoje responde
301 → 404 com `text/html`. O `ApiClient` tenta desserializar o HTML como JSON e explode no
`[OneTimeSetUp]` do `Hooks`, derrubando todas as fixtures.

O token **continua válido** — não é 401. Verificado com `POST /public/v2/users` sem token
(401) e com token (422).

Corrigir exige migrar para `https://gorest.co.in/public/v2`, que mudou o contrato: sem
envelope `code`/`meta`/`data`, status real no HTTP, `gender`/`status` em minúsculas.
É migração de contrato, não ajuste de path. Detalhes em
[docs/pontos-de-atencao.md](docs/pontos-de-atencao.md).

## Detalhes que não são óbvios pelo código

### A API v1 respondia HTTP 200 quase sempre

A versão legada `/public-api` codificava o resultado real no campo `code` do corpo. Por
isso todas as asserções checam `json.Code`, e não o status HTTP — e é o motivo dos modelos
`User` (envelope) e `Data` (payload) existirem separados. Na v2 isso deixa de fazer
sentido: os dois viram um DTO só.

### `Hooks` fornece a massa compartilhada

`[OneTimeSetUp]` cria o usuário "Leroy Borgan" e guarda o ID. `GetId()` devolve
`"/" + id` — **já com a barra**, então não adicionar outra ao compor o endpoint.

### `DelExcluirUsuario` destrói essa massa

O NUnit roda fixtures em ordem alfabética, então `Del...` executa antes de `Get...` e
`Put...` e derruba os dois. Hoje está mascarado pela falha do setup, mas reaparece assim
que a API for migrada.

### `Hooks` como `static class` não é problema

Contraria a recomendação do NUnit para `[SetUpFixture]`, mas o NUnit 3.12 executa os hooks
normalmente — confirmado na execução. Não mexer.

### Rodar os testes exige instalar o SDK 3.1 à parte

`netcoreapp3.1` saiu de suporte e não está no Homebrew. Instalar sem sudo:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 3.1 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

## Débitos técnicos conhecidos

Mapeados em [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md). Os principais:

1. **API v1 descontinuada** — bloqueia toda a suíte (`UrlProvider.cs:9`)
2. Token versionado no repositório e ativo (`TokenProvider.cs:11`)
3. `Hooks` monta endpoint com caminho duplicado e barra dupla, ignorando o `EndpointProvider`
4. `EndpointProvider.Users()` devolve URL absoluta, duplicando a base do `RestClient`
5. Teardown do POST concatena ID sem barra → massa nunca é limpa
6. Acoplamento entre fixtures pela massa compartilhada
7. Dados de teste fixos + API rejeita e-mail duplicado → execuções repetidas quebram
8. `ApiClient` descarta o `IRestResponse`, sem validar status nem content-type — é o que
   transforma um 404 HTML num `JsonReaderException` críptico

## Documentação

| Arquivo | Conteúdo |
| --- | --- |
| [docs/visao-geral.md](docs/visao-geral.md) | O que é o projeto, API testada, stack |
| [docs/arquitetura.md](docs/arquitetura.md) | Camadas, `ApiClient`, fluxo de requisição |
| [docs/casos-de-teste.md](docs/casos-de-teste.md) | Cada cenário automatizado em detalhe |
| [docs/como-executar.md](docs/como-executar.md) | Build, execução, Postman, falhas comuns |
| [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md) | Débitos técnicos com correções sugeridas |
