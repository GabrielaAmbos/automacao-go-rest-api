# CLAUDE.md

Contexto do projeto para o Claude Code.

## O que é este projeto

Automação de testes de API REST em **C# (.NET Core 3.1)** contra a API pública do
[Go REST](https://gorest.co.in/public/v2). Cobre os 4 recursos do domínio (users, posts,
comments, todos) mais paginação, filtros, autenticação e validação.

Não há código de aplicação — tudo no repositório existe para sustentar a suíte de testes.

**Estado: 68 de 68 passando** em 22 fixtures (verificado em 06/08/2026, 3 execuções
seguidas). A suíte leva ~1 min e faz ~150 chamadas.

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
├── client/ApiResponse.cs      StatusCode + Data + Header()/IntHeader()
├── models/                    User, Post, Comment, Todo, ErrorMessage, ValidationError
├── tests/Hooks.cs             [SetUpFixture] — massa compartilhada SOMENTE LEITURA
├── tests/*Factory.cs          User/Post/Comment/Todo — massa por fixture
├── tests/{users,posts,comments,todos,errors}/    fixtures por recurso
└── utils/
    ├── PaginationHeader.cs    constantes x-pagination-*
    ├── Query.cs               Query.Page(p, perPage) / Query.Filter(campo, valor)
    └── providers/             UrlProvider, EndpointProvider, TokenProvider
Postman/                       Coleção manual equivalente (v2, token via {{token}})
docs/                          Documentação técnica
```

## Comandos

```bash
export GOREST_TOKEN=<token>
cd "Go Rest API Automation"
dotnet build
dotnet test
dotnet test --filter "FullyQualifiedName~tests.posts"        # um grupo
dotnet test --filter "FullyQualifiedName~GetBuscarUsuario"   # uma fixture
```

O SDK 3.1 saiu de suporte e não está no Homebrew. Instalar sem sudo:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 3.1 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

## Convenções do projeto

- Namespace espelha a pasta: `tests.users`, `tests.posts`, `tests.comments`, `tests.todos`,
  `tests.errors`
- Classes de teste: `<Verbo><Ação>` — `GetBuscarUsuario`, `PostCriarPost`
- Cenário positivo: `Deve<Ação>` / negativo: `NaoDeve<Ação>`
- Cenário de inexistente usa ID negativo: `EndpointProvider.User(-id)` → 404
- Todo request vai por `ApiClient<T>` — nunca instanciar `RestClient` num teste
- Endpoints são **relativos** (`/users`); a URL base é só do `RestClient`
- URL, endpoint, token, query e headers de paginação saem de `utils/`

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

### Recursos criados pelo seu token são privados a ele

**Sem token a API devolve `404`, não `401`** — ela não vaza a existência do registro.
O `401` só aparece em escrita na coleção (`POST /users` → `Authentication failed`; token
falso → `Invalid token`). Dado de seed continua público:

| Requisição sem token | Resposta |
|---|---|
| `GET /users` ou `GET /posts` (coleção) | `200` |
| `GET /users/{id}` que você criou | `404` |
| `GET /posts/{id}` de seed | `200` |
| `GET /posts/{id}` que você criou | `404` |

Isso custou duas iterações para descobrir. Coberto pela fixture `AutenticacaoApi`.

### A API v2 usa status HTTP, não envelope

A v1 (`/public-api`) codificava o resultado num campo `code` do corpo e foi descontinuada —
passou a responder 301 com HTML e quebrou a suíte inteira. A v2 devolve o payload direto.
`gender`/`status` de user são minúsculos (`male`, `female`, `active`, `inactive`);
`status` de todo é `pending`/`completed`.

`GET` 200 · `POST` 201 · `PUT`/`PATCH` 200 · `DELETE` 204 (corpo vazio, `Data` null) ·
inexistente 404 · payload inválido 422 · escrita sem token 401.

### Posts, comments e todos só nascem pela rota aninhada

`POST /users/{id}/posts`, `POST /posts/{id}/comments`, `POST /users/{id}/todos`. Por isso a
massa vem em cadeia: usuário → post → comentário. `POST` na coleção plana falha com 422
(`user must exist`).

Excluir um post **cascateia** e apaga os comentários dele.

### Corpos de erro têm dois formatos

`ErrorMessage { Message }` para 401/404, `List<ValidationError> { Field, Message }` para
422. O `T` do `ApiClient` muda conforme o que se espera:
`ApiClient<List<ValidationError>>.Request(...)`.

### Massa de teste: duas estratégias distintas

- **Só leitura** → `[OneTimeSetUp]` da fixture, ou `Hooks.SharedUser`
- **Altera ou remove** → `[SetUp]`, com limpeza no `[TearDown]`

Não é estilo: numa versão anterior `DelExcluirUsuario` apagava a massa compartilhada e,
como o NUnit ordena fixtures alfabeticamente (`Del…` antes de `Get…` e `Put…`), derrubava
os outros testes. Pelo mesmo motivo, testes não podem depender da ordem — os de escrita sem
token verificam o efeito colateral dentro do próprio teste.

### E-mails precisam ser únicos

A API rejeita duplicados com 422. `UserFactory.UniqueEmail()` sufixa com `Guid` — é o que
torna a suíte repetível. Nunca usar e-mail literal num teste.

### `<X>Factory.Delete` ignora o resultado de propósito

Excluir algo já removido devolve 404. Os teardowns dependem disso.

### `ApiClient.Deserialize` é defensivo por experiência

Trata corpo vazio (204) e content-type não-JSON. O segundo caso existe porque a morte da v1
se manifestou como um `JsonReaderException` críptico; agora a exceção cita status,
content-type e URL efetiva.

### `RequestWithoutToken` / `RequestWithToken`

Métodos nomeados em vez de um parâmetro `token = null` ambíguo. Usados só pela fixture
`AutenticacaoApi`.

### O token não está no repositório

Vem de `GOREST_TOKEN`. Sem a variável, `TokenProvider` lança `InvalidOperationException`
com instrução de correção. A coleção Postman usa `{{token}}`, declarada vazia — ela também
guardava o token literal em 4 lugares antes da migração.

### Rate limit

`x-ratelimit-limit: 300`/min. A suíte usa ~150, então há folga — mas rodar 2× em sequência
rápida chega perto.

## Débitos técnicos conhecidos

Mapeados em [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md):

1. **O token antigo segue no histórico do Git e precisa ser revogado** em gorest.co.in
2. Pacotes mortos no `.csproj` (MSTest, SpecFlow, Microsoft.NETCore.App 2.2.8)
3. `netcoreapp3.1` fora de suporte — migrar para .NET 8 exige RestSharp 107+ (API
   incompatível, mas o impacto fica contido no `ApiClient`)
4. Lacunas de cobertura: `PATCH` só em users, matriz de 422 completa só em users, filtros
   não cobertos em posts/comments, nada de paginação fora do intervalo nem concorrência
5. Suíte é sequencial — NUnit permite paralelizar por fixture

## Documentação

| Arquivo | Conteúdo |
| --- | --- |
| [docs/visao-geral.md](docs/visao-geral.md) | Projeto, API v2, contrato, auth, cobertura |
| [docs/arquitetura.md](docs/arquitetura.md) | Camadas, `ApiClient`, factories, fluxo |
| [docs/casos-de-teste.md](docs/casos-de-teste.md) | Os 68 cenários em detalhe |
| [docs/como-executar.md](docs/como-executar.md) | SDK, token, execução, Postman, falhas comuns |
| [docs/pontos-de-atencao.md](docs/pontos-de-atencao.md) | Débitos em aberto + histórico dos resolvidos |
