# MVFC.DataX.Providers.PubSub

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.PubSub.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.PubSub)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.PubSub.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.PubSub)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Google Cloud Pub/Sub data provider for MVFC.DataX, implementing messaging via `Google.Cloud.PubSub.V1`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.PubSub
```

## Available Classes

| Class | Description |
|---|---|
| `PubSubDataReader<T>` | Streams messages via `SubscriberClient.StartAsync`, pushing results into an internal Channel and sending Acks/Nacks back to GCP based on pipeline execution. |
| `PubSubDataWriter<T>` | Publishes messages via `PublisherClient.PublishAsync`. Batching is handled by issuing `Task.WhenAll` concurrently. |

## API Signatures

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

## Usage / Example

```csharp
using System.Text.Json;
using Google.Cloud.PubSub.V1;
using Google.Protobuf;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.PubSub;

// Ensure your GOOGLE_APPLICATION_CREDENTIALS environment variable is set
var subscriptionName = SubscriptionName.FromProjectSubscription("my-gcp-project", "inbound-sub");
var topicName = TopicName.FromProjectTopic("my-gcp-project", "outbound-topic");

var subscriber = await SubscriberClient.CreateAsync(subscriptionName);
var publisher = await PublisherClient.CreateAsync(topicName);

// 1. Setup the Pub/Sub Reader
var reader = new PubSubDataReader<Order>(
    subscriber,
    // Deserialize PubsubMessage (ByteString -> string -> object)
    deserializer: msg => JsonSerializer.Deserialize<Order>(msg.Data.ToStringUtf8())!
);

// 2. Setup the Pub/Sub Writer
var writer = new PubSubDataWriter<Order>(
    publisher,
    // Serialize to Protobuf ByteString wrapper
    serializer: order => new PubsubMessage
    {
        Data = ByteString.CopyFromUtf8(JsonSerializer.Serialize(order))
    }
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    .WithParallelism(4) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
