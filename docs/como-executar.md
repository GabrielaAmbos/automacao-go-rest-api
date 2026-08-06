# Como executar

## Pré-requisitos

- **.NET Core SDK 3.1** (target framework do projeto)
- Acesso à internet — os testes chamam a API pública real, não há mock
- Token válido do Go REST configurado em
  [`TokenProvider`](../Go%20Rest%20API%20Automation/utils/providers/TokenProvider.cs)

Opcional:

- **Visual Studio 2019+** ou **JetBrains Rider** para rodar pela interface
- **Postman** para as chamadas manuais da coleção

## Restaurar e compilar

```bash
cd "Go Rest API Automation"
dotnet restore
dotnet build
```

## Executar a suíte

```bash
dotnet test
```

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
2. `Test > Test Explorer`
3. `Run All`

## Renovando o token

O token do Go REST expira. Para renovar:

1. Acesse [gorest.co.in](https://gorest.co.in) e faça login
2. Copie o access token gerado
3. Substitua o valor em `TokenProvider.BasicToken()`, mantendo o prefixo `Bearer `

```csharp
public static string BasicToken()
{
    return "Bearer <seu-token-aqui>";
}
```

> Idealmente esse valor deveria vir de variável de ambiente. Veja
> [Pontos de atenção](pontos-de-atencao.md#1-token-de-autenticação-versionado-no-repositório).

## Coleção Postman

`Postman/Go Rest Desafio.postman_collection.json` contém as mesmas chamadas em versão
manual, útil para explorar a API ou depurar uma falha da automação.

### Importando

1. Postman > `Import` > selecione o arquivo
2. Crie as variáveis de ambiente usadas pela coleção:

| Variável | Valor |
| --- | --- |
| `url` | `https://gorest.co.in` |
| `LastUserId` | ID de um usuário existente |

3. Configure o header `Authorization` com `Bearer <token>`

### Requisições disponíveis

| Nome | Método | Endpoint |
| --- | --- | --- |
| Listar todos os usuários | GET | `{{url}}/public-api/users` |
| Criar um usuário | POST | `{{url}}/public-api/users` |
| Buscar usuário | GET | `{{url}}/public-api/users/{{LastUserId}}` |
| Alterar usuário | PUT | `{{url}}/public-api/users/{{LastUserId}}` |
| Excluir um usuário | DELETE | `{{url}}/public-api/users/{{LastUserId}}` |

## Falhas comuns

| Sintoma | Causa provável |
| --- | --- |
| Todos os testes falham com `code == 401` | Token expirado ou inválido |
| `DeveCriarUmUsuario` falha com `code == 422` | E-mail fixo já cadastrado na API — a massa não foi limpa em execução anterior |
| `DeveBuscarUsuario` / `DeveAlterarUsuarioExistente` falham com `code == 404` | `DelExcluirUsuario` rodou antes e removeu a massa compartilhada |
| `NullReferenceException` em `json.Data` | A API respondeu sem o campo `data` (erro de autenticação ou payload rejeitado) |
