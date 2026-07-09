# MVFC.DataX.Providers.MySql

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.MySql.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MySql)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MySql.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MySql)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider do MySQL para o MVFC.DataX, implementando leitura e escrita usando a biblioteca oficial `MySqlConnector`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.MySql
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `MySqlDataReader<T>` | Realiza queries lendo os dados através de um `MySqlDataReader` e um mapeamento customizado. |
| `MySqlDataWriter<T>` | Escreve via queries parametrizadas. Suporta `WriteBatchAsync` provendo uso interno de transações (`MySqlTransaction`). |

## Assinaturas da API

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

## Uso / Exemplo

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.MySql;
using MySqlConnector;

var connectionString = "Server=localhost;User ID=test;Password=test;Database=testdb";

// 1. Configurando o Leitor do MySQL
var reader = new MySqlDataReader<User>(
    connectionString,
    "SELECT Id, Name, Email FROM Users WHERE IsActive = true",
    // Converte os resultados do banco em um objeto C#
    mapper: r => new User
    {
        Id = r.GetInt32(0),
        Name = r.GetString(1),
        Email = r.GetString(2)
    }
);

// 2. Configurando o Escritor (Inserts)
var writer = new MySqlDataWriter<User>(
    connectionString,
    "INSERT INTO ImportedUsers (OriginalId, Name, Email) VALUES (@Id, @Name, @Email)",
    // Vincula o objeto as váriaveis do MySQL Command
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
    .WithBatchSize(100)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
