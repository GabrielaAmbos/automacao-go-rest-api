# CLAUDE.md

Contexto do projeto para o Claude Code.

## O que é este projeto

Automação de testes de API REST em **C# (.NET Core 3.1)** contra a API pública do
[Go REST](https://gorest.co.in/public/v2). Valida o CRUD de usuários com 7 testes
cobrindo GET, POST, PUT e DELETE em cenários positivos e negativos.

Não há código de aplicação — tudo no repositório existe para sustentar a suíte de testes.

**Estado: 7 de 7 passando** (verificado em 06/08/2026, 3 execuções seguidas).

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
├── client/ApiResponse.cs      Wrapper: StatusCode + Data desserializado
├── models/User.cs             DTO do usuário (a v2 não tem envelope)
├── tests/Hooks.cs             [SetUpFixture] — massa compartilhada SOMENTE LEITURA
├── tests/UserFactory.cs       Create/Delete/UniqueEmail para massa por fixture
├── tests/*.cs                 Fixtures por verbo HTTP
└── utils/providers/           UrlProvider, EndpointProvider, TokenProvider
Postman/                       Coleção manual equivalente (v2, token via {{token}})
docs/                          Documentação técnica
```

## Comandos

```bash
export GOREST_TOKEN=<token>
cd "Go Rest API Automation"
dotnet restore
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~GetBuscarUsuario"
```

O SDK 3.1 saiu de suporte e não está no Homebrew. Instalar sem sudo:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 3.1 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

## Convenções do projeto

- Nomes de classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarUmUsuario`
- Cenário positivo: `Deve<Ação>` / negativo: `NaoDeve<Ação>`
- Cenário negativo usa ID negativo: `EndpointProvider.User(-id)` → a API devolve 404
- Todo request vai por `ApiClient<T>.Request(endpoint, method, body)` — nunca instanciar
  `RestClient` direto num teste
- Endpoints são **relativos** (`/users`); a URL base é só do `RestClient`
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

## Detalhes que não são óbvios pelo código

### A API v2 usa status HTTP, não envelope

A v1 (`/public-api`) codificava o resultado num campo `code` do corpo e foi descontinuada
em 2025 — passou a responder 301 com HTML e quebrou a suíte inteira. A v2 devolve o payload
direto e o status real no HTTP. Por isso `User` é um DTO único (não existe mais `Data`) e as
asserções são `response.StatusCode.Should().Be(HttpStatusCode.OK)`.

`gender` e `status` são **minúsculos** na v2: `male`, `female`, `active`, `inactive`.

### Status por operação

`GET` 200 · `POST` 201 · `PUT` 200 · `DELETE` 204 (corpo vazio, `Data` vem null) ·
inexistente 404 · payload inválido 422 · escrita sem token 401.

### Massa de teste: duas estratégias distintas

- `Hooks.SharedUser` — criado uma vez, **somente leitura**, consumido só pelo `GET`
- `UserFactory.Create()` no `[SetUp]` — fixtures que **alteram estado** (PUT, DELETE) criam
  a própria massa e limpam no `[TearDown]`

Isso não é preferência de estilo: na versão anterior `DelExcluirUsuario` apagava a massa
compartilhada e, como o NUnit ordena fixtures alfabeticamente (`Del…` antes de `Get…` e
`Put…`), derrubava os outros dois testes.

### E-mails precisam ser únicos

A API rejeita duplicados com 422. `UserFactory.UniqueEmail()` sufixa com `Guid` — é o que
torna a suíte repetível. Nunca usar e-mail literal num teste.

### `UserFactory.Delete` ignora o resultado de propósito

Excluir usuário já removido devolve 404. O teardown do `DelExcluirUsuario` depende disso.

### `ApiClient.Deserialize` é defensivo por experiência

Trata corpo vazio (204) e content-type não-JSON. O segundo caso existe porque a morte da v1
se manifestou como um `JsonReaderException` críptico; agora a exceção cita status,
content-type e URL efetiva.

### O token não está no repositório

Vem de `GOREST_TOKEN`. Sem a variável, `TokenProvider` lança `InvalidOperationException`
com instrução de correção. A coleção Postman usa `{{token}}`, declarada vazia — ela também
guardava o token literal em 4 lugares antes da migração.

## Débitos técnicos conhecidos

Mapeados em [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md):

1. **O token antigo segue no histórico do Git e precisa ser revogado** em gorest.co.in
2. Pacotes mortos no `.csproj` (MSTest, SpecFlow, Microsoft.NETCore.App 2.2.8)
3. `netcoreapp3.1` fora de suporte — migrar para .NET 8 exige RestSharp 107+ (API
   incompatível, mas o impacto fica contido no `ApiClient`)
4. Cobertura rasa: faltam 422, 401, listagem com paginação e PATCH

## Documentação

| Arquivo | Conteúdo |
| --- | --- |
| [docs/visao-geral.md](docs/visao-geral.md) | O que é o projeto, API v2, stack |
| [docs/arquitetura.md](docs/arquitetura.md) | Camadas, `ApiClient`, massa de teste, fluxo |
| [docs/casos-de-teste.md](docs/casos-de-teste.md) | Cada cenário automatizado em detalhe |
| [docs/como-executar.md](docs/como-executar.md) | SDK, token, execução, Postman, falhas comuns |
| [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md) | Débitos em aberto + histórico dos resolvidos |
