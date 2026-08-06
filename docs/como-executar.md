# Como executar

## Pré-requisitos

- **.NET Core SDK 3.1** (target framework do projeto)
- Acesso à internet — os testes chamam a API pública real, não há mock
- Token do Go REST exportado em `GOREST_TOKEN`

Opcional:

- **Visual Studio 2019+** ou **JetBrains Rider** para rodar pela interface
- **Postman** para as chamadas manuais da coleção

## Instalando o SDK 3.1

O .NET Core 3.1 saiu de suporte em dezembro de 2022 e não está mais nos gerenciadores de
pacote (o Homebrew só oferece 6, 8 e 9). Use o script oficial, que instala em `~/.dotnet`
sem `sudo` e sem alterar o sistema:

```bash
curl -fsSL https://dot.net/v1/dotnet-install.sh -o dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 3.1 --install-dir "$HOME/.dotnet"
export PATH="$HOME/.dotnet:$PATH"
```

Em macOS com Apple Silicon não há build nativo do 3.1 — é preciso rodar sob Rosetta ou
migrar o target framework.

## Configurando o token

O token é lido da variável de ambiente `GOREST_TOKEN` — **não fica no código**.

1. Acesse [gorest.co.in](https://gorest.co.in) e faça login
2. Copie o access token gerado
3. Exporte a variável (sem o prefixo `Bearer `, que o `TokenProvider` adiciona):

```bash
export GOREST_TOKEN=<seu-token>
```

Para não repetir a cada sessão, adicione a linha ao `~/.zshrc`. No Visual Studio ou Rider,
configure a variável nas opções de execução de testes.

Sem a variável, a suíte falha no setup com a mensagem:

```
System.InvalidOperationException : Environment variable GOREST_TOKEN is not set.
Get a token at https://gorest.co.in and export it before running the tests.
```

## Restaurar, compilar e testar

```bash
export GOREST_TOKEN=<seu-token>
cd "Go Rest API Automation"
dotnet restore
dotnet build
dotnet test
```

Saída esperada:

```
Test Run Successful.
Total tests: 7
     Passed: 7
```

A suíte é repetível: cada execução gera e-mails únicos e limpa a massa que criou, então
pode rodar quantas vezes for preciso sem colisão de dados.

### Executar uma fixture específica

```bash
dotnet test --filter "FullyQualifiedName~GetBuscarUsuario"
```

### Executar um teste específico

```bash
dotnet test --filter "Name=DeveBuscarUsuario"
```

### Saída detalhada

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Executar pelo Visual Studio

1. Abra `Go Rest API Automation/Go Rest API Automation.sln`
2. Garanta que `GOREST_TOKEN` está definida no ambiente
3. `Test > Test Explorer`
4. `Run All`

## Coleção Postman

`Postman/Go Rest Desafio.postman_collection.json` contém as mesmas chamadas em versão
manual, útil para explorar a API ou depurar uma falha da automação. Já aponta para a v2.

### Importando

1. Postman > `Import` > selecione o arquivo
2. Preencha as variáveis da coleção:

| Variável | Valor |
| --- | --- |
| `url` | `https://gorest.co.in` (já vem preenchida) |
| `token` | seu token do Go REST — vem **vazia** de propósito, para não versionar credencial |
| `LastUserId` | ID de um usuário existente |

O `token` é usado no `auth` bearer das requisições de escrita; as de leitura não precisam.

### Requisições disponíveis

| Nome | Método | Endpoint |
| --- | --- | --- |
| Listar todos os usuários | GET | `{{url}}/public/v2/users` |
| Criar um usuário | POST | `{{url}}/public/v2/users` |
| Buscar usuário | GET | `{{url}}/public/v2/users/{{LastUserId}}` |
| Alterar usuário | PUT | `{{url}}/public/v2/users/{{LastUserId}}` |
| Excluir um usuário | DELETE | `{{url}}/public/v2/users/{{LastUserId}}` |

## Falhas comuns

| Sintoma | Causa provável |
| --- | --- |
| `InvalidOperationException: Environment variable GOREST_TOKEN is not set` | Variável não exportada na sessão que roda os testes |
| Todos os testes falham com `401 Unauthorized` | Token expirado ou inválido — gere outro em gorest.co.in |
| `InvalidOperationException: Expected a JSON response but got ...` | A API respondeu HTML. Confira status, content-type e URL na própria mensagem — foi assim que a descontinuação da v1 se manifestou |
| `DeveCriarUmUsuario` falha com `422` | Validação da API. O e-mail é único por execução, então suspeite de `gender`/`status` fora dos valores aceitos (minúsculos) |

### Diagnosticando pela API direto

```bash
API=https://gorest.co.in/public/v2
TOKEN="Bearer $GOREST_TOKEN"

# leitura é pública
curl -s -w "\nHTTP %{http_code}\n" "$API/users"

# checagem de token sem criar massa: 401 = inválido, 422 = válido
curl -s -w "\nHTTP %{http_code}\n" -X POST -H "Authorization: $TOKEN" \
  -H "Content-Type: application/json" -d '{}' "$API/users"
```
