# MVFC.DataX.Readers.Http

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Readers.Http.svg)](https://www.nuget.org/packages/MVFC.DataX.Readers.Http)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Readers.Http.svg)](https://www.nuget.org/packages/MVFC.DataX.Readers.Http)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provedor de consumo de dados via HTTP para o MVFC.DataX. Leia perfeitamente coleções JSON normais ou integre-se as novas APIs nativas de stream (`IAsyncEnumerable<T>`) dentro de seus Data Pipelines.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Readers.Http
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `HttpApiReader<T>` | Aceita um delegate injetável para permitir uso facilitado do `HttpClient`, independentemente do método de autenticação da aplicação cliente. |

## Assinaturas da API

```csharp
// Construtor para REST APIs comuns (retornam a coleção toda de uma vez na resposta JSON)
public HttpApiReader(Func<CancellationToken, Task<IEnumerable<T>>> fetch)

// Construtor para alta performance e Streaming usando Server-Sent Events / Chunking
public HttpApiReader(Func<CancellationToken, IAsyncEnumerable<T>> fetch)
```

## Uso / Exemplo

### Exemplo 1: Chamada REST API Padrão

```csharp
using System.Net.Http.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Readers.Http;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// 1. Configurando o Leitor HTTP
var reader = new HttpApiReader<User>(
    // Esse array será desserializado na memória de uma vez e só então 
    // os objetos serão injetados 1 a 1 no pipeline.
    async ct => await httpClient.GetFromJsonAsync<IEnumerable<User>>("/users", ct)
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(myWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```

### Exemplo 2: Streaming HTTP (Alta Performance)

```csharp
using System.Net.Http.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Readers.Http;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// 1. Configurando Leitura HTTP com Streaming
var reader = new HttpApiReader<User>(
    // O recurso GetFromJsonAsAsyncEnumerable do .NET vai gerar os itens "on-the-fly"
    // sem precisar esperar a requisição HTTP inteira terminar de baixar 
    // os bytes do JSON (ideal para bases enormes)
    ct => httpClient.GetFromJsonAsAsyncEnumerable<User>("/users/stream", cancellationToken: ct)
);

var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(myWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
