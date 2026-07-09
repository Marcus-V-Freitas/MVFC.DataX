# MVFC.DataX.Providers.FileSystem

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.FileSystem.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.FileSystem)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.FileSystem.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.FileSystem)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

File System providers for MVFC.DataX supporting flat files. Currently implements CSV parsing via `CsvHelper` and JSONL (JSON lines) parsing.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.FileSystem
```

## Available Classes

| Class | Format | Description |
|---|---|---|
| `CsvDataReader<T>` | `.csv` | Reads CSV files sequentially leveraging CsvHelper internally. |
| `CsvDataWriter<T>` | `.csv` | Thread-safe CSV writer leveraging a `SemaphoreSlim`. Implements `IAsyncDisposable`. |
| `JsonlDataReader<T>` | `.jsonl` | Line-by-line JSON reading. Gracefully handles blank lines. |
| `JsonlDataWriter<T>` | `.jsonl` | Thread-safe JSONL writer using `SemaphoreSlim` and `StreamWriter`. Implements `IAsyncDisposable`. |

## API Signatures

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

## Usage / Example

### CSV Pipeline
```csharp
using System.Globalization;
using CsvHelper.Configuration;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.FileSystem;

var config = new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true };

var reader = new CsvDataReader<Person>("input.csv", config);

// Use 'using' or 'await using' for writers as they hold file handles
await using var writer = new CsvDataWriter<Person>("output.csv", config, append: false);

var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .Build();

await pipeline.RunAsync(cancellationToken);
```

### JSONL Pipeline
```csharp
using System.Text.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.FileSystem;

var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

var reader = new JsonlDataReader<LogEntry>("logs.jsonl", options);

await using var writer = new JsonlDataWriter<LogEntry>("filtered_logs.jsonl", options);

var pipeline = PipelineBuilder.ReadFrom(reader)
    // You can filter data fluently
    .TransformWith(new FilterTransformer<LogEntry>(log => log.Level == "ERROR"))
    .WriteTo(writer)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
