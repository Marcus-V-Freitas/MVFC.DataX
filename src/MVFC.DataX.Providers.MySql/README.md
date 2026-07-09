# MVFC.DataX.Providers.MySql

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.MySql.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MySql)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MySql.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MySql)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

MySQL data provider for MVFC.DataX, implementing readers and writers using the `MySqlConnector` library.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.MySql
```

## Available Classes

| Class | Description |
|---|---|
| `MySqlDataReader<T>` | Reads data from MySQL utilizing `MySqlDataReader` via a user-defined mapping function. |
| `MySqlDataWriter<T>` | Writes data to MySQL using parameterized queries. Supports transactions for batch writes. |

## API Signatures

### MySqlDataReader<T>
```csharp
public MySqlDataReader(
    string connectionString,
    string query,
    Func<MySqlDataReader, T> mapper,
    Action<MySqlCommand>? configureCommand = null)
```

### MySqlDataWriter<T>
```csharp
public MySqlDataWriter(
    string connectionString,
    string commandText,
    Action<MySqlCommand, T> bindParameters)
```

## Usage / Example

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.MySql;
using MySqlConnector;

var connectionString = "Server=localhost;User ID=test;Password=test;Database=testdb";

// 1. Setup the MySQL Reader
var reader = new MySqlDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = true",
    // Map the MySqlDataReader into your C# object
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Setup the MySQL Writer
var writer = new MySqlDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Bind your C# object's properties to the MySqlCommand parameters
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
    .WithBatchSize(100)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
