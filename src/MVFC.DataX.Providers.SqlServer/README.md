# MVFC.DataX.Providers.SqlServer

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.SqlServer.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SqlServer)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SqlServer.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SqlServer)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

SQL Server (MSSQL) data provider for MVFC.DataX, implementing readers and writers using `Microsoft.Data.SqlClient`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.SqlServer
```

## Available Classes

| Class | Description |
|---|---|
| `SqlServerDataReader<T>` | Reads data from SQL Server utilizing `SqlDataReader` via a user-defined mapping function. |
| `SqlServerDataWriter<T>` | Writes data to SQL Server using parameterized queries. Supports batch transactions with commit and automatic rollback. |

## API Signatures

### SqlServerDataReader<T>
```csharp
public SqlServerDataReader(
    string connectionString,
    string query,
    Func<SqlDataReader, T> mapper,
    Action<SqlCommand>? configureCommand = null)
```

### SqlServerDataWriter<T>
```csharp
public SqlServerDataWriter(
    string connectionString,
    string commandText,
    Action<SqlCommand, T> bindParameters)
```

## Usage / Example

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.SqlServer;
using Microsoft.Data.SqlClient;

var connectionString = "Server=localhost;Database=testdb;User Id=sa;Password=test;";

// 1. Setup the SQL Server Reader
var reader = new SqlServerDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = 1",
    // Map the SqlDataReader into your C# object
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Setup the SQL Server Writer
var writer = new SqlServerDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Bind your C# object's properties to the SqlCommand parameters
    bindParameters: (command, user) =>
    {
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Name", user.Name);
        command.Parameters.AddWithValue("@Email", user.Email);
    }
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(100) // Writes will be grouped into transactions
    .Build();

await pipeline.RunAsync(cancellationToken);
```
