# MVFC.DataX.Readers.Http

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Readers.Http.svg)](https://www.nuget.org/packages/MVFC.DataX.Readers.Http)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Readers.Http.svg)](https://www.nuget.org/packages/MVFC.DataX.Readers.Http)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

HTTP API Reader for MVFC.DataX. Seamlessly read bulk JSON arrays or use modern `IAsyncEnumerable<T>` streaming API integrations from your Data Pipelines.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Readers.Http
```

## Available Classes

| Class | Description |
|---|---|
| `HttpApiReader<T>` | Supports generating streams out of API HTTP responses via `HttpClient` (bulk) or HTTP streaming protocols. |

## API Signatures

```csharp
// Constructor for fetching a collection all at once (bulk mapping)
public HttpApiReader(Func<CancellationToken, Task<IEnumerable<T>>> fetch)

// Constructor for consuming an API streaming protocol natively
public HttpApiReader(Func<CancellationToken, IAsyncEnumerable<T>> fetch)
```

## Usage / Example

### Example 1: Standard REST API Call (Bulk)

```csharp
using System.Net.Http.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Readers.Http;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// 1. Setup the HTTP Reader
var reader = new HttpApiReader<User>(
    // Note: This lambda will execute once and buffer the full IEnumerable 
    // to then stream items to the pipeline.
    async ct => await httpClient.GetFromJsonAsync<IEnumerable<User>>("/users", ct)
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(myWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```

### Example 2: Streaming HTTP (High Performance)

```csharp
using System.Net.Http.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Readers.Http;

var httpClient = new HttpClient { BaseAddress = new Uri("https://api.example.com") };

// 1. Setup the HTTP Reader with streaming
var reader = new HttpApiReader<User>(
    // Using Refit or .NET native GetFromJsonAsAsyncEnumerable handles 
    // partial chunks gracefully
    ct => httpClient.GetFromJsonAsAsyncEnumerable<User>("/users/stream", cancellationToken: ct)
);

var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(myWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
