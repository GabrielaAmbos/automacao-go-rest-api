# Documentação — Automação Go Rest API

Documentação técnica do projeto de automação de testes de API do
[Go REST](https://gorest.co.in), escrito em C# com RestSharp e NUnit.

> ⚠️ **A suíte não passa hoje.** O build compila, mas os 7 testes falham: a API v1 que eles
> testam foi descontinuada. Diagnóstico completo em
> [Pontos de atenção](pontos-de-atencao.md#1-a-api-alvo-foi-descontinuada--bloqueia-toda-a-suíte).

## Índice

| Documento | Conteúdo |
| --- | --- |
| [Visão geral](visao-geral.md) | O que é o projeto, objetivo, API testada e stack |
| [Arquitetura](arquitetura.md) | Estrutura de pastas, camadas e fluxo de uma requisição |
| [Casos de teste](casos-de-teste.md) | Detalhamento de cada cenário automatizado |
| [Como executar](como-executar.md) | Pré-requisitos, build, execução e coleção Postman |
| [Pontos de atenção](pontos-de-atencao.md) | Débitos técnicos e riscos mapeados no código atual |

## Resumo rápido

- **Linguagem:** C# (.NET Core 3.1)
- **Runner de testes:** NUnit 3
- **Cliente HTTP:** RestSharp 106
- **Asserções:** FluentAssertions
- **API sob teste:** `https://gorest.co.in/public-api` (descontinuada — hoje redireciona para `/public/v2`)
- **Cenários automatizados:** 7 testes cobrindo GET, POST, PUT e DELETE de usuários
- **Última execução:** 06/08/2026 — build OK, 7 de 7 testes falhando
