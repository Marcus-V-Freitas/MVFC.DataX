# MVFC.DataX.Providers.SqlServer

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.SqlServer.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SqlServer)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SqlServer.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SqlServer)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider do SQL Server (MSSQL) para o MVFC.DataX, implementando leitura e escrita usando a biblioteca oficial `Microsoft.Data.SqlClient`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.SqlServer
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `SqlServerDataReader<T>` | Realiza queries lendo os dados através de um `SqlDataReader` mapeando os atributos manualmente em uma `Func`. |
| `SqlServerDataWriter<T>` | Escreve e injeta comandos `SqlCommand` suportando o método `WriteBatchAsync` para transações em massa (`SqlTransaction`) e rollbacks automáticos. |

## Assinaturas da API

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

## Uso / Exemplo

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.SqlServer;
using Microsoft.Data.SqlClient;

var connectionString = "Server=localhost;Database=testdb;User Id=sa;Password=test;";

// 1. Configurando o Leitor do SQL Server
var reader = new SqlServerDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = 1",
    // Converte os resultados vindos do banco em C#
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Configurando o Escritor (Inserts)
var writer = new SqlServerDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Vincula a classe aos @ parâmetros previnindo SQL Injections
    bindParameters: (command, user) =>
    {
        command.Parameters.AddWithValue("@Id", user.Id);
        command.Parameters.AddWithValue("@Name", user.Name);
        command.Parameters.AddWithValue("@Email", user.Email);
    }
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(100)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
