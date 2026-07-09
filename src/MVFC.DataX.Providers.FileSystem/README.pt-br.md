# MVFC.DataX.Providers.FileSystem

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.FileSystem.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.FileSystem)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.FileSystem.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.FileSystem)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider de arquivos para o MVFC.DataX. Atualmente implementa leitura e escrita de CSV (usando `CsvHelper`) e formato JSONL (JSON por linha).

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.FileSystem
```

## Classes Disponíveis

| Classe | Formato | Descrição |
|---|---|---|
| `CsvDataReader<T>` | `.csv` | Lê arquivos CSV de modo assíncrono usando `CsvHelper`. |
| `CsvDataWriter<T>` | `.csv` | Escreve em CSV de modo seguro para concorrência (Thread-Safe usando um `SemaphoreSlim`). Herda `IAsyncDisposable`. |
| `JsonlDataReader<T>` | `.jsonl` | Lê JSON linha a linha de um arquivo ignorando linhas em branco automaticamente. |
| `JsonlDataWriter<T>` | `.jsonl` | Escreve logs/dados JSON formatados 1 por linha. Também thread-safe (Semáforo). |

## Assinaturas da API

### CSV
```csharp
public CsvDataReader(string filePath, CsvConfiguration? configuration = null)
public CsvDataWriter(string filePath, CsvConfiguration? configuration = null, bool append = false) : IAsyncDisposable
```

### JSONL
```csharp
public JsonlDataReader(string filePath, JsonSerializerOptions? options = null)
public JsonlDataWriter(string filePath, JsonSerializerOptions? options = null, bool append = false) : IAsyncDisposable
```

## Uso / Exemplo

### Pipeline CSV
```csharp
using System.Globalization;
using CsvHelper.Configuration;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.FileSystem;

var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

var reader = new CsvDataReader<Person>("input.csv", config);

// Garanta que `await using` seja chamado para Writers do FileSystem liberarem os handles do disco.
await using var writer = new CsvDataWriter<Person>("output.csv", config, append: false);

var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .Build();

await pipeline.RunAsync(cancellationToken);
```

### Pipeline JSONL
```csharp
using System.Text.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.FileSystem;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var reader = new JsonlDataReader<LogEntry>("logs.jsonl", options);

await using var writer = new JsonlDataWriter<LogEntry>("filtered_logs.jsonl", options);

var pipeline = PipelineBuilder.ReadFrom(reader)
    // Filtros podem ser acoplados diretamente sem precisar instanciar a classe explicitamente
    .TransformWith(new FilterTransformer<LogEntry>(log => log.Level == "ERROR"))
    .WriteTo(writer)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
