# MVFC.DataX.Providers.RabbitMQ

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.RabbitMQ.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.RabbitMQ)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.RabbitMQ.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.RabbitMQ)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

RabbitMQ data provider for MVFC.DataX, implementing readers and writers via `RabbitMQ.Client`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.RabbitMQ
```

## Available Classes

| Class | Description |
|---|---|
| `RabbitMqDataReader<T>` | Reads data from a RabbitMQ queue via `AsyncEventingBasicConsumer` mapped via a deserializer delegate. Automatically handles Ack/Nack responses based on completion. |
| `RabbitMqDataWriter<T>` | Publishes items via `BasicPublishAsync`. Maintains a lazy internal connection (`IConnection`) established when writing the first time. |

## API Signatures

### RabbitMqDataReader<T>
```csharp
public RabbitMqDataReader(
    IConnectionFactory connectionFactory,
    string queueName,
    Func<byte[], T> deserializer) : IDisposable
```

### RabbitMqDataWriter<T>
```csharp
public RabbitMqDataWriter(
    IConnectionFactory connectionFactory,
    string exchange,
    string routingKey,
    Func<T, byte[]> serializer) : IDisposable
```

## Usage / Example

**Note:** Both reader and writer maintain internal connections and channels. Ensure you call `.Dispose()` properly, utilizing `using`.

```csharp
using System.Text.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.RabbitMQ;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };

// 1. Setup the RabbitMQ Reader
using var reader = new RabbitMqDataReader<Order>(
    factory,
    "orders.inbound.queue",
    // Convert byte[] array back to your object
    deserializer: body => JsonSerializer.Deserialize<Order>(body)!
);

// 2. Setup the RabbitMQ Writer
using var writer = new RabbitMqDataWriter<Order>(
    factory,
    "orders.exchange",
    "orders.processed",
    // Serialize object to byte array
    serializer: order => JsonSerializer.SerializeToUtf8Bytes(order)
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    // Run multiple threads to process message queue simultaneously
    .WithParallelism(4) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
