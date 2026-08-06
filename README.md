[![Typing SVG](https://readme-typing-svg.demolab.com?font=Fira+Code&size=18&pause=1000&color=DF62F7&width=435&lines=Projeto+de+Automa%C3%A7%C3%A3o+Go+Rest+API)](https://git.io/typing-svg)

Projeto de automação no endpoint https://gorest.co.in/public/v2 utilizando a linguagem C# e o framework de automação RestSharp.

## Features testadas:

    /Get Buscar Usuário
    /Post Criar Usuário
    /Put Alterar Usuário
    /Delete Excluir Usuário

## Executando

```bash
export GOREST_TOKEN=<seu-token>
cd "Go Rest API Automation"
dotnet test
```

O token é lido da variável de ambiente `GOREST_TOKEN` — gere o seu em [gorest.co.in](https://gorest.co.in).

## Documentação

Documentação técnica completa em [docs/](docs/):

- [Visão geral](docs/visao-geral.md) — projeto, API e stack
- [Arquitetura](docs/arquitetura.md) — camadas, cliente HTTP e gestão de massa
- [Casos de teste](docs/casos-de-teste.md) — detalhe de cada cenário
- [Como executar](docs/como-executar.md) — setup, execução e troubleshooting
- [Pontos de atenção](docs/pontos-de-atencao.md) — débitos técnicos
