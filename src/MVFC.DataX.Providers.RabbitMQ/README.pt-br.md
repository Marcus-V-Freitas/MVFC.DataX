# MVFC.DataX.Providers.RabbitMQ

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.RabbitMQ.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.RabbitMQ)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.RabbitMQ.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.RabbitMQ)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider de fila (RabbitMQ) para o MVFC.DataX, implementando leitura e publicação usando a biblioteca oficial `RabbitMQ.Client`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.RabbitMQ
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `RabbitMqDataReader<T>` | Consome itens utilizando o `AsyncEventingBasicConsumer` de modo contínuo. Confirma processamentos automáticos (`BasicAckAsync`) ou reverte (`BasicNackAsync`). |
| `RabbitMqDataWriter<T>` | Publica dados através do `BasicPublishAsync`. Garante que as conexões (`IConnection` / `IChannel`) sejam abertas por demanda (lazy init) |

## Assinaturas da API

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

## Uso / Exemplo

**Nota:** Garanta a chamada de `.Dispose()` (`using`) nas suas instâncias, para que as conexões/canais abertos com o Rabbit sejam fechados corretamente.

```csharp
using System.Text.Json;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.RabbitMQ;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };

// 1. Configurando o Leitor do RabbitMQ
using var reader = new RabbitMqDataReader<Order>(
    factory,
    "orders.inbound.queue",
    // Realiza a conversão do array de bytes pego na fila
    deserializer: body => JsonSerializer.Deserialize<Order>(body)!
);

// 2. Configurando o Escritor / Publisher
using var writer = new RabbitMqDataWriter<Order>(
    factory,
    "orders.exchange",
    "orders.processed",
    // Converte sua classe C# em array de bytes para a Exchange
    serializer: order => JsonSerializer.SerializeToUtf8Bytes(order)
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    // Permite uso de múltiplas threads para consumir a fila de forma acelerada
    .WithParallelism(4) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
