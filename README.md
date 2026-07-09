# MVFC.DataX

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

A complete .NET library suite for building robust, asynchronous data pipelines (ETL/ELT). With a pluggable, extensible architecture, you can read, transform, and write data across multiple databases, messaging systems, and APIs using a single, unified pipeline abstraction.

## Motivation

Building data pipelines and ETL processes in .NET often means:

- Tightly coupling your code to specific SDKs (SQL Server, MongoDB, SQS, RabbitMQ, etc.).
- Rewriting data ingestion, transformation, and load logic every time a data source changes.
- Duplicating error handling, batch processing, and pipeline orchestration logic.
- Finding it hard to test data flows without running real infrastructure.

**MVFC.DataX** solves this by providing a clean abstraction layer — `IDataReader<T>`, `IDataTransformer<TIn, TOut>`, and `IDataWriter<T>` — combined with a powerful Pipeline builder. You pick the providers for your source and destination, and you get a consistent, async-first API for executing your data workflows. Swapping out a database for a message broker requires changing only the provider instantiation; your core business logic stays exactly the same.

## Architecture

All packages follow the same pattern based on the Core abstractions:

- `IDataReader<T>` — contract for reading data as an `IAsyncEnumerable<T>`.
- `IDataTransformer<TIn, TOut>` — contract for transforming data asynchronously.
- `IDataWriter<T>` — contract for writing data (single and batch operations).
- `PipelineBuilder` — orchestrates reading, transforming, and writing data, handling errors and batching.
- Each provider package implements these interfaces using native SDKs.

Once you understand how to use one provider, all others work identically within the pipeline.

## Packages

| Package | Source/Destination | Downloads |
|---|---|---|
| [MVFC.DataX.Core](src/MVFC.DataX.Core/README.md) | Base abstractions | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Core) |
| [MVFC.DataX.Pipeline](src/MVFC.DataX.Pipeline/README.md) | Pipeline Orchestrator | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Pipeline) |
| [MVFC.DataX.Providers.FileSystem](src/MVFC.DataX.Providers.FileSystem/README.md) | Local/Network Files | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.FileSystem) |
| [MVFC.DataX.Providers.MongoDB](src/MVFC.DataX.Providers.MongoDB/README.md) | MongoDB | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MongoDB) |
| [MVFC.DataX.Providers.MySql](src/MVFC.DataX.Providers.MySql/README.md) | MySQL | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MySql) |
| [MVFC.DataX.Providers.Postgres](src/MVFC.DataX.Providers.Postgres/README.md) | PostgreSQL | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.Postgres) |
| [MVFC.DataX.Providers.PubSub](src/MVFC.DataX.Providers.PubSub/README.md) | Google Pub/Sub | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.PubSub) |
| [MVFC.DataX.Providers.RabbitMQ](src/MVFC.DataX.Providers.RabbitMQ/README.md) | RabbitMQ | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.RabbitMQ) |
| [MVFC.DataX.Providers.SQS](src/MVFC.DataX.Providers.SQS/README.md) | Amazon SQS | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SQS) |
| [MVFC.DataX.Providers.SqlServer](src/MVFC.DataX.Providers.SqlServer/README.md) | SQL Server | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SqlServer) |
| [MVFC.DataX.Readers.Http](src/MVFC.DataX.Readers.Http/README.md) | HTTP APIs | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Readers.Http) |
| [MVFC.DataX.Validation](src/MVFC.DataX.Validation/README.md) | Data Validation | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Validation) |

***

## Installation

Install the Core and Pipeline packages, and then choose the providers you need:

```sh
# Base abstractions & pipeline builder (always required)
dotnet add package MVFC.DataX.Core
dotnet add package MVFC.DataX.Pipeline

# Pick your source/destination providers (examples)
dotnet add package MVFC.DataX.Providers.Postgres
dotnet add package MVFC.DataX.Providers.SQS
```

## Quick Start

### 1. Define your data models

```csharp
public record UserRecord(int Id, string Name, string Email);
public record UserDto(string FullName, string ContactEmail);
```

### 2. Implement a Data Transformer (Optional)

```csharp
using System.Runtime.CompilerServices;
using MVFC.DataX.Core.Abstractions;
using MVFC.DataX.Core.Models;

public class UserTransformer : IDataTransformer<UserRecord, UserDto>
{
    public async IAsyncEnumerable<DataResult<UserDto>> TransformAsync(
        IAsyncEnumerable<UserRecord> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct))
        {
            // Transform and return wrapped in DataResult
            yield return DataResult<UserDto>.Success(
                new UserDto(item.Name, item.Email)
            );
        }
    }
}
```

### 3. Build and execute the pipeline

```csharp
using MVFC.DataX.Pipeline;

// Instantiate your reader and writer (using PostgreSQL to SQS as an example)
var reader = new PostgresDataReader<UserRecord>(postgresConnectionString, "SELECT * FROM Users");
var writer = new SqsDataWriter<UserDto>(sqsClient, queueUrl);
var transformer = new UserTransformer();

// Build the pipeline
var pipeline = PipelineBuilder.Create()
    .ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    .Build();

// Execute the pipeline
await pipeline.ExecuteAsync(cancellationToken);
```

## Available API

### Reader Methods
| Method | Description |
|---|---|
| `ReadAsync(CancellationToken ct)` | Returns an `IAsyncEnumerable<T>` representing the data stream. |

### Transformer Methods
| Method | Description |
|---|---|
| `TransformAsync(IAsyncEnumerable<TIn> input, CancellationToken ct)` | Processes the stream and yields transformed items. |

### Writer Methods
| Method | Description |
|---|---|
| `WriteAsync(T item, CancellationToken ct)` | Writes a single item to the destination. |
| `WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct)` | Writes a batch of items to the destination. |

## Project Structure

```text
src/
  MVFC.DataX.Core/                   # Base abstractions (interfaces)
  MVFC.DataX.Pipeline/               # Pipeline execution engine
  MVFC.DataX.Validation/             # Data validation components
  MVFC.DataX.Readers.Http/           # HTTP API reader
  MVFC.DataX.Providers.*/            # Implementations for specific technologies
tests/
  MVFC.DataX.Tests/                  # Unit and integration tests
```

## Requirements

- .NET 9.0+
- The underlying SDK for each provider (pulled automatically via NuGet)

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md).

## License

[Apache-2.0](LICENSE)
