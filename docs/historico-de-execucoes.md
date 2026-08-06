# Histórico de execuções

Registro de todas as execuções da suíte em **06/08/2026**, na sessão que migrou o projeto
da API v1 para a v2 e expandiu a cobertura de 7 para 68 testes.

> **Procedência dos dados.** As execuções 1 a 12 foram reconstruídas do log da sessão — na
> época nenhuma rodou com logger, então não há `.trx` para elas. A execução 13 rodou com
> `--logger trx` e é a única com artefato verificável, em
> `Go Rest API Automation/TestResults/execucao.trx` (não versionado).

## Ambiente

| Item | Valor |
| --- | --- |
| SDK | .NET Core 3.1.426, instalado em `~/.dotnet` via `dotnet-install.sh` |
| Máquina | macOS 24.6.0, Intel Core i5-8257U |
| API | `https://gorest.co.in/public/v2` (real, sem mock) |
| Token | via `GOREST_TOKEN` |

O SDK precisou ser instalado: não havia `dotnet` na máquina, e o 3.1 saiu de suporte em
dezembro de 2022 — o Homebrew só oferece 6, 8 e 9.

---

## Linha do tempo

| # | Contexto | Testes | Passou | Falhou | Tempo |
| --- | --- | --- | --- | --- | --- |
| 1 | Estado inicial, antes da migração | 7 | 0 | **7** | 2,8 s |
| 2 | Após migrar para a v2 | 7 | 7 | 0 | 10,0 s |
| 3 | Repetibilidade — 2ª seguida | 7 | 7 | 0 | 9,4 s |
| 4 | Repetibilidade — 3ª seguida | 7 | 7 | 0 | 9,6 s |
| 5 | Controle negativo: sem `GOREST_TOKEN` | 7 | 0 | **7** | — |
| 6 | Verificação final antes do commit | 7 | 7 | 0 | 9,7 s |
| 7 | Suíte expandida, 1ª execução | 65 | 63 | **2** | 1,25 min |
| 8 | Após 1ª correção de autenticação | 67 | 66 | **1** | 1,20 min |
| 9 | Após 2ª correção de autenticação | 68 | 68 | 0 | 1,20 min |
| 10 | Repetibilidade — 2ª seguida | 68 | 68 | 0 | — |
| 11 | Repetibilidade — 3ª seguida | 68 | 68 | 0 | — |
| 12 | Verificação final antes do commit | 68 | 68 | 0 | — |
| 13 | Execução com logger `trx` | 68 | 68 | 0 | 1,21 min |

Onde o tempo está vazio, a saída foi filtrada por `grep` e a linha `Total time` não foi
capturada.

### Por que o total de testes muda entre 7 → 65 → 67 → 68

Cada salto corresponde a uma edição da suíte entre execuções:

- **7 → 65**: implementação de posts, comments, todos, listagem, filtros e erros
- **65 → 67**: a fixture `AutenticacaoApi` foi reescrita de 5 para 7 testes ao corrigir a
  premissa sobre o comportamento sem token
- **67 → 68**: `DeveBuscarPostSemToken` virou dois testes, separando o post criado pelo
  token do post público de seed

---

## Execuções que falharam

### #1 — A API v1 tinha sido descontinuada

```
Total tests: 7
     Failed: 7

OneTimeSetUp: Newtonsoft.Json.JsonReaderException :
Unexpected character encountered while parsing value: <. Path '', line 0, position 0.
   at ApiClient`1.Request(...) in client/ApiClient.cs:line 30
   at Hooks.CriarUsuario() in tests/Hooks.cs:line 25
```

Uma única causa derrubou os 7 testes: a falha aconteceu no `[OneTimeSetUp]`, que é
pré-requisito de todas as fixtures.

O `/public-api` passou a responder `301` para a v2, e o caminho duplicado que o `Hooks`
montava caía num `404` HTML:

```
GET https://gorest.co.in/public-api/public-api/users
→ 301 https://gorest.co.in/public/v2/public-api/users
→ 404, content-type: text/html
```

O cliente tentava desserializar `<!DOCTYPE html>` como JSON. Não era regressão do código —
a API alvo tinha sido desligada.

**Resolução:** migração completa para a v2 (execução #2).

### #5 — Controle negativo do guard de token

Execução proposital sem `GOREST_TOKEN`, para validar que a ausência da variável falha de
forma legível:

```
System.InvalidOperationException : Environment variable GOREST_TOKEN is not set.
Get a token at https://gorest.co.in and export it before running the tests.
```

Comportamento esperado — não é defeito. Confirma que o guard do `TokenProvider` funciona.

### #7 — Duas premissas erradas sobre autenticação

```
Total tests: 65
     Passed: 63
     Failed: 2
```

| Teste | Esperado | Obtido |
| --- | --- | --- |
| `DeveBuscarUsuarioSemToken` | `200 OK` | `404 NotFound` |
| `NaoDeveExcluirUsuarioSemToken` | `401 Unauthorized` | `404 NotFound` |

Eu tinha assumido que, sendo a leitura pública, `GET /users/{id}` funcionaria sem token, e
que escrita sem token daria `401`. A verificação na API mostrou outra coisa:

```
GET    /users/{id} sem token → 404 {"message":"Resource not found"}
DELETE /users/{id} sem token → 404
PUT    /users/{id} sem token → 404
```

A API **não vaza a existência do recurso**: devolve `404` em vez de `401`. O `401` só
aparece em escrita na coleção (`POST /users`).

**Resolução:** testes reescritos para asserir o comportamento real, e os de escrita passaram
a confirmar, com token, que a tentativa não teve efeito colateral.

### #8 — O escopo do token é mais amplo do que eu tinha entendido

```
Total tests: 67
     Passed: 66
     Failed: 1
```

| Teste | Esperado | Obtido |
| --- | --- | --- |
| `DeveBuscarPostSemToken` | `200 OK` | `404 NotFound` |

Eu tinha concluído que a regra do `404` valia só para `/users/{id}`, porque um `GET` sem
token num post de seed devolvia `200`. Investigando com um post criado por mim:

```
GET /posts/{id} criado pelo meu token, sem token → 404
GET /posts/{id} de seed,               sem token → 200
```

A regra real não é por recurso, é por origem: **o que o seu token cria fica restrito a
ele**. Dado de seed continua público.

**Resolução:** o teste virou dois — um confirmando que o post próprio some sem
autenticação, outro confirmando que o post público continua legível.

---

## Execução #13 — detalhamento

Única com artefato `.trx`. Rodada após o commit da suíte expandida.

```
Total tests: 68
     Passed: 68
 Total time: 1.2106 Minutes
```

| Campo | Valor |
| --- | --- |
| Início | 06/08/2026 15:01:28 |
| Fim | 06/08/2026 15:02:41 |
| Duração média por teste | 0,69 s |

### Por grupo

| Grupo | Testes | Tempo |
| --- | --- | --- |
| users | 17 | 14,01 s |
| posts | 13 | 9,69 s |
| todos | 13 | 8,82 s |
| comments | 12 | 8,53 s |
| errors | 13 | 6,06 s |

### Testes mais lentos

| Tempo | Teste |
| --- | --- |
| 1,62 s | `users/DelExcluirUsuario.NaoDeveBuscarUsuarioDepoisDeExcluir` |
| 1,29 s | `users/PutAlterarUsuario.DeveAlterarUsuarioExistente` |
| 1,28 s | `posts/PutAlterarPost.DeveAlterarPostExistente` |
| 1,26 s | `todos/PutAlterarTarefa.DeveConcluirTarefa` |
| 1,26 s | `posts/DelExcluirPost.DeveExcluirPostExistente` |
| 1,23 s | `users/PutAlterarUsuario.NaoDeveAlterarUsuario` |
| 1,23 s | `comments/PutAlterarComentario.DeveAlterarComentarioExistente` |
| 1,23 s | `todos/DelExcluirTarefa.NaoDeveExcluirTarefaInexistente` |

O tempo é dominado por I/O de rede, não por processamento: os mais lentos são justamente os
que fazem mais de uma chamada (massa no `[SetUp]` + a chamada do teste + verificação).

---

## Leitura dos resultados

**Estabilidade.** Seis execuções consecutivas da suíte final (#9 a #13, mais a
verificação de commit) passaram sem nenhuma falha intermitente. A repetibilidade vem de
duas decisões: e-mail com sufixo `Guid` em toda massa criada, e limpeza no `[TearDown]` de
cada fixture.

**Toda falha foi de premissa, não de flakiness.** As três execuções vermelhas tiveram causa
determinística — API descontinuada (#1), variável ausente (#5, proposital) e duas
suposições minhas sobre autenticação (#7, #8). Nenhuma falhou e passou depois sem
mudança de código.

**O ciclo #7 → #8 → #9 é o achado mais útil da sessão.** Duas iterações de teste vermelho
revelaram uma regra de negócio da API que não está documentada: o escopo do token sobre os
recursos que ele cria. Está registrado em
[Visão geral](visao-geral.md#autenticação--o-detalhe-que-mais-surpreende).

**Custo de API.** ~150 chamadas por execução, contra um rate limit de 300/min
(`x-ratelimit-limit`). Duas execuções em sequência muito rápida chegam perto do teto.

---

## Como gerar um novo relatório

```bash
export GOREST_TOKEN=<seu-token>
cd "Go Rest API Automation"
dotnet test --logger "trx;LogFileName=execucao.trx"
```

O arquivo sai em `TestResults/execucao.trx` — XML com resultado, duração e stack trace de
cada teste, legível por Visual Studio, Azure DevOps e Jenkins. A pasta já é ignorada pelo
`.gitignore` (`[Tt]est[Rr]esult*/`), então o artefato não vai para o repositório.

Para extrair o resumo por grupo:

```bash
python3 - <<'PY'
import xml.etree.ElementTree as ET, collections
ns = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
root = ET.parse("TestResults/execucao.trx").getroot()
defs = {d.get("id"): d for d in root.findall(".//t:UnitTest", ns)}
by = collections.defaultdict(lambda: [0, 0.0])
for r in root.findall(".//t:UnitTestResult", ns):
    className = defs[r.get("testId")].find("t:TestMethod", ns).get("className")
    h, m, s = [float(p) for p in r.get("duration", "0:0:0").split(":")]
    group = by[className.split(".")[-2]]
    group[0] += 1
    group[1] += h * 3600 + m * 60 + s
for name, (count, seconds) in sorted(by.items()):
    print(f"{name:10} {count:2} testes  {seconds:6.2f}s")
PY
```
