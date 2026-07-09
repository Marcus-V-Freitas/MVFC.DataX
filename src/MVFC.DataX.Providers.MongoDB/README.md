# MVFC.DataX.Providers.MongoDB

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.MongoDB.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MongoDB)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MongoDB.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MongoDB)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

MongoDB data provider for MVFC.DataX, using the official `MongoDB.Driver`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.MongoDB
```

## Available Classes

| Class | Description |
|---|---|
| `MongoReader<T>` | Reads data from a Mongo Collection using an asynchronous cursor (`IAsyncCursor<T>`). Supports filters and options. |
| `MongoWriter<T>` | Writes data using `InsertOneAsync` and batches using `InsertManyAsync`. |

## API Signatures

### MongoReader<T>
```csharp
public MongoReader(
    string connectionString,
    string database,
    string collectionName,
    FilterDefinition<T>? filter = null,
    FindOptions<T>? options = null)
```

### MongoWriter<T>
```csharp
public MongoWriter(
    string connectionString,
    string database,
    string collectionName)
```

## Usage / Example

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.MongoDB;
using MongoDB.Driver;

var connectionString = "mongodb://localhost:27017";

// 1. Setup the MongoDB Reader (Filtering for Active users)
var filter = Builders<User>.Filter.Eq(u => u.IsActive, true);
var reader = new MongoReader<User>(
    connectionString,
    "admin_db",
    "users",
    filter
);

// 2. Setup the MongoDB Writer
var writer = new MongoWriter<User>(
    connectionString,
    "reports_db",
    "active_users_backup"
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(500) // 500 documents will be inserted at a time using InsertManyAsync
    .Build();

await pipeline.RunAsync(cancellationToken);
```
