# MVFC.DataX.Providers.SQS

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.SQS.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SQS)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SQS.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SQS)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Amazon SQS data provider for MVFC.DataX, implementing AWS messaging queues via `AWSSDK.SQS`.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Providers.SQS
```

## Available Classes

| Class | Description |
|---|---|
| `SqsDataReader<T>` | Streams data from an SQS queue via long-polling, automatically executing `DeleteMessageAsync` for yielded messages. |
| `SqsDataWriter<T>` | Writes item via `SendMessageAsync`. In batch context, chunks requests into sets of 10 mapping to `SendMessageBatchAsync`. |

## API Signatures

### SqsDataReader<T>
```csharp
public SqsDataReader(
    IAmazonSQS sqsClient,
    string queueUrl,
    Func<Message, T> deserializer,
    int maxMessages = 10,
    int waitTimeSeconds = 20)
```

### SqsDataWriter<T>
```csharp
public SqsDataWriter(
    IAmazonSQS sqsClient,
    string queueUrl,
    Func<T, SendMessageRequest> serializer)
```

## Usage / Example

```csharp
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.SQS;

var sqsClient = new AmazonSQSClient(); // Assumes AWS credentials are valid in environment
var inboundQueue = "https://sqs.us-east-1.amazonaws.com/123/inbound";
var outboundQueue = "https://sqs.us-east-1.amazonaws.com/123/outbound";

// 1. Setup the SQS Reader
var reader = new SqsDataReader<Order>(
    sqsClient,
    inboundQueue,
    // Safely deserialize the body of the SQS Message
    deserializer: msg => JsonSerializer.Deserialize<Order>(msg.Body)!
);

// 2. Setup the SQS Writer
var writer = new SqsDataWriter<Order>(
    sqsClient,
    outboundQueue,
    // Convert your object to an explicit SendMessageRequest model
    serializer: order => new SendMessageRequest
    {
        MessageBody = JsonSerializer.Serialize(order)
    }
);

// 3. Orchestrate with PipelineBuilder
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    // SQS chunks internally into 10-message limits via WriteBatchAsync optimization
    .WithBatchSize(25) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
