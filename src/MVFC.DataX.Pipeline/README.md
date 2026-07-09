# MVFC.DataX.Pipeline

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Pipeline.svg)](https://www.nuget.org/packages/MVFC.DataX.Pipeline)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Pipeline.svg)](https://www.nuget.org/packages/MVFC.DataX.Pipeline)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

The core Pipeline execution engine for MVFC.DataX, providing `PipelineBuilder` for orchestrating data flows, batching, and error handling.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Pipeline
```

## Available Classes

| Class | Description |
|---|---|
| `PipelineBuilder` (static) | Entry point to create a pipeline via `PipelineBuilder.ReadFrom<T>(reader)`. |
| `PipelineBuilder<TInput>` | Fluent builder API supporting `.TransformWith()`, `.Skip()`, `.Take()`, `.Distinct()`, `.OrderBy()`, `.FlatMap()`, `.Aggregate()`, and `.Batch()`. |
| `PipelineBuilder<TInput, TOutput>` | Advanced builder with `.WriteTo(writer)`, `.WithParallelism(int)`, `.WithBatchSize(int)`, `.WithRetry(int, TimeSpan)`, `.WithChannelCapacity(int)`, `.OnError(writer)`, and `.OnCompleted(Action)`. |
| `DataPipeline<TInput, TOutput>` | The built orchestrator. Call `.RunAsync(ct)` to begin processing and retrieve `PipelineStatistics`. |
| `PipelineOptions` | Configuration record capturing degrees of parallelism, capacities, timeouts, and batch sizes. |

## Usage / Example

### Simple Pipeline

```csharp
using MVFC.DataX.Pipeline;

// Instantiate providers/transformers
var reader = new MyDataReader();
var transformer = new MyDataTransformer();
var writer = new MyDataWriter();

var pipeline = PipelineBuilder.ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    .Build();

var stats = await pipeline.RunAsync(cancellationToken);

Console.WriteLine($"Processed {stats.Succeeded} items successfully in {stats.Elapsed}.");
```

### Advanced Pipeline

This example demonstrates handling failures with a Dead-Letter Queue (DLQ), batching, inline Linq-like operations, and parallelism.

```csharp
using MVFC.DataX.Pipeline;

var pipeline = PipelineBuilder.ReadFrom(reader)
    // Inline transformations available by default via the PipelineBuilder
    .Skip(100)
    .Distinct()
    
    // Custom transformers
    .TransformWith(transformer)
    
    // Fluent configuration
    .WriteTo(writer)
    .WithBatchSize(500)
    .WithParallelism(4)
    .WithRetry(maxRetries: 3, delay: TimeSpan.FromSeconds(2))
    .WithChannelCapacity(5000)
    
    // Route validation or transformation failures to a DLQ Writer
    .OnError(deadLetterWriter)
    
    // Setup a completion hook
    .OnCompleted(async stats => 
    {
        await notificationService.NotifyAsync($"Pipeline completed. Failed: {stats.Failed}");
    })
    .Build();

await pipeline.RunAsync(cancellationToken);
```
