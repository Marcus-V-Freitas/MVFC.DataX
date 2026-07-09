# MVFC.DataX.Providers.Postgres

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.Postgres.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.Postgres)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.Postgres.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.Postgres)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

PostgreSQL data provider for MVFC.DataX, implementing readers and writers via `Npgsql`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.Postgres
```

## Available Classes

| Class | Description |
|---|---|
| `PostgresDataReader<T>` | Reads data from PostgreSQL utilizing `NpgsqlDataReader` via a mapper function. |
| `PostgresDataWriter<T>` | Writes data to PostgreSQL using parameterized queries. Supports `WriteBatchAsync` with transactions and automatic rollback on failure. |

## API Signatures

### PostgresDataReader<T>
```csharp
public PostgresDataReader(
    string connectionString,
    string query,
    Func<NpgsqlDataReader, T> mapper,
    Action<NpgsqlCommand>? configureCommand = null)
```

### PostgresDataWriter<T>
```csharp
public PostgresDataWriter(
    string connectionString,
    string commandText,
    Action<NpgsqlCommand, T> bindParameters)
```

## Usage / Example

This example demonstrates how to read a `User` entity from a Postgres database and copy it into an `ImportedUser` table. Note how `bindParameters` securely mitigates SQL Injection by pushing parameters into the `NpgsqlCommand`.

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.Postgres;
using Npgsql;

var connectionString = "Host=localhost;Username=test;Password=test;Database=testdb";

// 1. Setup the Postgres Reader
var reader = new PostgresDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = true",
    // Map the NpgsqlDataReader into your C# object
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Setup the Postgres Writer
var writer = new PostgresDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Bind your C# object's properties to the NpgsqlCommand parameters
    bindParameters: (command, user) =>
    {
        command.Parameters.AddWithValue("Id", user.Id);
        command.Parameters.AddWithValue("Name", user.Name);
        command.Parameters.AddWithValue("Email", user.Email);
    }
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(100) // Writes will be grouped into transactions of 100 inserts each
    .Build();

await pipeline.RunAsync(cancellationToken);
```
