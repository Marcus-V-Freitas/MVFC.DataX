# MVFC.DataX.Providers.SQS

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Providers.SQS.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SQS)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SQS.svg)](https://www.nuget.org/packages/MVFC.DataX.Providers.SQS)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provider de fila (Amazon SQS) para o MVFC.DataX, implementando leitura e publicação usando a biblioteca oficial `AWSSDK.SQS`.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Providers.SQS
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `SqsDataReader<T>` | Cria um stream utilizando loops de *long-polling*, aplicando em seguida o `DeleteMessageAsync` na fila para todo item já iterado e extraído para a engine. |
| `SqsDataWriter<T>` | Envia mensagens a AWS. Durante lotes (`WriteBatchAsync`), este provedor divide silenciosamente a lista provida em sublistas de 10 (limite AWS), mapeando nativamente para `SendMessageBatchAsync`. |

## Assinaturas da API

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

## Uso / Exemplo

```csharp
using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Providers.SQS;

var sqsClient = new AmazonSQSClient(); // Assume que suas AWS Credentials estão presentes no terminal
var inboundQueue = "https://sqs.us-east-1.amazonaws.com/123/inbound";
var outboundQueue = "https://sqs.us-east-1.amazonaws.com/123/outbound";

// 1. Configurando o Leitor do SQS
var reader = new SqsDataReader<Order>(
    sqsClient,
    inboundQueue,
    // Mapeia o body recebido para a classe
    deserializer: msg => JsonSerializer.Deserialize<Order>(msg.Body)!
);

// 2. Configurando o Escritor (Publicador na outra fila)
var writer = new SqsDataWriter<Order>(
    sqsClient,
    outboundQueue,
    // Cria um SendMessageRequest oficial a partir de uma classe arbitrária
    serializer: order => new SendMessageRequest
    {
        MessageBody = JsonSerializer.Serialize(order)
    }
);

// 3. Orquestrando com a Engine
var pipeline = PipelineBuilder.ReadFrom(reader)
    .WriteTo(writer)
    // Devido as restrições da AWS de 10 itens por request de Batch API, o `WithBatchSize` 
    // vai enviar 2.5 blocos por vez com uma eficiência altíssima.
    .WithBatchSize(25) 
    .Build();

await pipeline.RunAsync(cancellationToken);
```
