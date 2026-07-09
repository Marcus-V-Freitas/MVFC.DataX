# MVFC.DataX.Core

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Core.svg)](https://www.nuget.org/packages/MVFC.DataX.Core)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Core.svg)](https://www.nuget.org/packages/MVFC.DataX.Core)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Core abstractions and interfaces (`IDataReader<T>`, `IDataTransformer<TIn, TOut>`, `IDataWriter<T>`) for the MVFC.DataX ETL/ELT suite.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Core
```

## Available Classes

| Class | Type | Description |
|---|---|---|
| `IDataReader<T>` | Interface | Reading contract — `ReadAsync()` returns `IAsyncEnumerable<T>` |
| `IDataTransformer<TIn, TOut>` | Interface | Transformation contract — `TransformAsync()` returns `IAsyncEnumerable<DataResult<TOut>>` |
| `IDataWriter<T>` | Interface | Writing contract — `WriteAsync()` and `WriteBatchAsync()` |
| `DataResult<T>` | Record | Result wrapper — `IsSuccess`, `Value`, `Errors`. Has factories `DataResult.Success<T>()` and `DataResult.Failure<T>()` |
| `DataError` | Record | Data error representing failures: `PropertyName`, `ErrorMessage`, `AttemptedValue` |
| `PipelineStatistics` | Record | Pipeline run stats: `TotalRead`, `Succeeded`, `Failed`, `Skipped`, `Elapsed`, `Errors` |
| `EnumerableReader<T>` | Class | Wraps an `IEnumerable<T>` or `IAsyncEnumerable<T>` into an `IDataReader<T>` |
| `ChannelDataReader<T>` | Class | Reader that consumes from a `ChannelReader<T>` |
| `InMemoryWriter<T>` | Class | Writer that accumulates items in a `ConcurrentBag<T>` accessible via the `Items` property |
| `DelegateWriter<T>` | Class | Writer that delegates batch writes to a `Func<IReadOnlyList<T>, CancellationToken, Task>` |
| `MapTransformer<TIn, TOut>` | Class | Applies a mapping function with automatic exception handling to `DataResult.Failure` |
| `FilterTransformer<T>` | Class | Filters data using a predicate. Exceptional rows become `DataResult.Failure` |
| `BatchTransformer<T>` | Class | Groups inputs into `IReadOnlyList<T>` of a fixed size |
| `SkipTransformer<T>` / `TakeTransformer<T>` | Class | Pagination transformers |
| `DistinctTransformer<T>` | Class | Removes duplicates using an optional `IEqualityComparer<T>` |
| `OrderByTransformer<T, TKey>` | Class | Sorts the stream by a key |
| `FlatMapTransformer<TIn, TOut>` | Class | 1-to-N expansion (Flattens iterables) |
| `AggregateTransformer<TIn, TAcc>` | Class | State accumulation (fold) |

## Usage / Example

While you typically use `PipelineBuilder` to orchestrate these classes, you can use the Core classes directly to structure data flows.

```csharp
using MVFC.DataX.Core.Models;
using MVFC.DataX.Core.Readers;
using MVFC.DataX.Core.Transformers;
using MVFC.DataX.Core.Writers;

var sourceData = new[] { 1, 2, 3, 4, 5 };

// 1. Create a reader from an in-memory collection
var reader = new EnumerableReader<int>(sourceData);

// 2. Create a transformer that multiplies numbers by 2, dropping odds
var transformer = new MapTransformer<int, int>(x => 
{
    if (x % 2 != 0) throw new Exception("Odds not allowed");
    return x * 2;
});

// 3. Create an in-memory writer
var writer = new InMemoryWriter<int>();

// 4. Manually pipe data (Pipeline engine automates this)
await foreach (var item in reader.ReadAsync())
{
    var transformedStream = transformer.TransformAsync(
        new[] { item }.ToAsyncEnumerable()
    );

    await foreach (var result in transformedStream)
    {
        if (result.IsSuccess)
        {
            await writer.WriteAsync(result.Value);
        }
        else
        {
            Console.WriteLine($"Error: {result.Errors[0].ErrorMessage}");
        }
    }
}

// 5. Inspect the written results
foreach (var written in writer.Items)
{
    Console.WriteLine($"Written: {written}"); // Will print: 4, 8
}
```
