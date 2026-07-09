# MVFC.DataX.Providers.Postgres

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.Postgres.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.Postgres)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.Postgres.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.Postgres)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider do PostgreSQL para o MVFC.DataX, implementando leitura e escrita usando a biblioteca oficial `Npgsql`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.Postgres
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `PostgresDataReader<T>` | Realiza queries lendo os dados através de um `NpgsqlDataReader` e um mapping para tipos tipados. |
| `PostgresDataWriter<T>` | Escreve via insert/update parametrizados. Operações como o `WriteBatchAsync` abrem uma transaction automaticamente, invocando o Rollback em caso de falha. |

## Assinaturas da API

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

## Uso / Exemplo

Este exemplo demonstra a leitura da tabela `Users`, sua conversão para o tipo C# interno, e finalmente a inserção (ou escrita) em uma nova tabela de destinos. Note o uso seguro da configuração de queries no parâmetro `bindParameters`.

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.Postgres;
using Npgsql;

var connectionString = "Host=localhost;Username=test;Password=test;Database=testdb";

// 1. Configurando o Leitor do PostgreSQL
var reader = new PostgresDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = true",
    // Converte os resultados brutos em sua classe .NET (User)
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Configurando o Escritor (Inserts)
var writer = new PostgresDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Vincula a classe do .NET com os parametros SQL via `NpgsqlCommand`
    bindParameters: (command, user) =>
    {
        command.Parameters.AddWithValue("Id", user.Id);
        command.Parameters.AddWithValue("Name", user.Name);
        command.Parameters.AddWithValue("Email", user.Email);
    }
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(100) // 100 Inserts são gerados por cada "Commit" transacional
    .Build();

await pipeline.RunAsync(cancellationToken);
```
