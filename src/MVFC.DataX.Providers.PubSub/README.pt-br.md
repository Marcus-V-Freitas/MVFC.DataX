# MVFC.DataX.Providers.PubSub

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.PubSub.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.PubSub)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.PubSub.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.PubSub)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider para mensageria no Google Cloud Pub/Sub do MVFC.DataX usando o pacote `Google.Cloud.PubSub.V1`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.PubSub
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `PubSubDataReader<T>` | Consome continuamente a subscription via `SubscriberClient.StartAsync`. Ele traduz automaticamente `Ack/Nack` para o GCP com base no status do item no Pipeline usando um Channel local em memória. |
| `PubSubDataWriter<T>` | Publica dados no GCP via `PublisherClient.PublishAsync`. Batch actions publicam dados via array e disparam concorrência com `Task.WhenAll`. |

## Assinaturas da API

### PubSubDataReader<T>
```csharp
public PubSubDataReader(
    SubscriberClient subscriberClient,
    Func<PubsubMessage, T> deserializer)
```

### PubSubDataWriter<T>
```csharp
public PubSubDataWriter(
    PublisherClient publisherClient,
    Func<T, PubsubMessage> serializer)
```

## Uso / Exemplo

```csharp
using System.Text.Json;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.PubSub;

// Verifique as configurações ADC (GOOGLE_APPLICATION_CREDENTIALS) no SO
var subscriptionName = SubscriptionName.FromProjectSubscription("my-gcp-project", "inbound-sub");
var topicName = TopicName.FromProjectTopic("my-gcp-project", "outbound-topic");

var subscriber = await SubscriberClient.CreateAsync(subscriptionName);
var publisher = await PublisherClient.CreateAsync(topicName);

// 1. Configurando o Leitor do PubSub
var reader = new PubSubDataReader<Order>(
    subscriber,
    // Realiza a conversão do ByteString oficial do pacote do Google
    deserializer: msg => JsonSerializer.Deserialize<Order>(msg.Data.ToStringUtf8())!
);

// 2. Configurando o Escritor (Publicador)
var writer = new PubSubDataWriter<Order>(
    publisher,
    // Envolve sua classe no objeto nativo (Protobuf/ByteString) 
    serializer: order => new PubsubMessage
    {
        Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(order))
    }
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithParallelism(4) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
