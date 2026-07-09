# MVFC.DataX.Providers.MongoDB

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.MongoDB.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MongoDB)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MongoDB.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.MongoDB)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider do MongoDB para o MVFC.DataX, implementando leitura e escrita usando a biblioteca oficial `MongoDB.Driver`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.MongoDB
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `MongoReader<T>` | Realiza a leitura e iteração utilizando cursores assíncronos do driver nativo (`IAsyncCursor<T>`). Suporta a injenção de opções de find e filtros. |
| `MongoWriter<T>` | Escreve na coleção usando `InsertOneAsync`, mas mapeia requisições em batch utilizando `InsertManyAsync`. |

## Assinaturas da API

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

## Uso / Exemplo

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.MongoDB;
using MongoDB.Driver;

var connectionString = "mongodb://localhost:27017";

// 1. Configurando o Leitor do MongoDB (Com um filtro na coleção original)
var filter = Builders<User>.Filter.Eq(u => u.IsActive, true);
var reader = new MongoReader<User>(
    connectionString,
    "admin_db",
    "users",
    filter
);

// 2. Configurando o Escritor (Inserts)
var writer = new MongoWriter<User>(
    connectionString,
    "reports_db",
    "active_users_backup"
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithBatchSize(500) // O Pipeline agrupá 500 chamadas antes de dar um InsertManyAsync
    .Build();

await pipeline.RunAsync(cancellationToken);
```
