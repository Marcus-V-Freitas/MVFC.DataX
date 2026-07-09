# MVFC.DataX.Pipeline

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Pipeline.svg)](https://www.nuget.org/packages/MVFC.DataX.Pipeline)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Pipeline.svg)](https://www.nuget.org/packages/MVFC.DataX.Pipeline)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

O motor principal de execução de Pipeline para o MVFC.DataX, fornecendo o `PipelineBuilder` para orquestração de fluxos de dados, lotes (batches) e tratamento de erros.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Pipeline
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `PipelineBuilder` (static) | Ponto de entrada — `PipelineBuilder.ReadFrom<T>(reader)`. |
| `PipelineBuilder<TInput>` | API Fluent que permite funções in-line como `.TransformWith()`, `.Skip()`, `.Take()`, `.Distinct()`, `.OrderBy()`, `.FlatMap()`, `.Aggregate()` e `.Batch()`. |
| `PipelineBuilder<TInput, TOutput>` | Builder estendido com a saída definida, adicionando `.WriteTo(writer)`, `.WithParallelism(int)`, `.WithBatchSize(int)`, `.WithRetry(int, TimeSpan)`, `.WithChannelCapacity(int)`, `.OnError(writer)` e `.OnCompleted(Action)`. |
| `DataPipeline<TInput, TOutput>` | O pipeline construído. Utilize o método `.RunAsync(ct)` para inicializá-lo e obter o resultado `PipelineStatistics`. |
| `PipelineOptions` | Record de opções preenchido internamente para manter configurações de paralelismo, fila limitadora, e retry. |

## Uso / Exemplo

### Pipeline Simples

```csharp
using MVFC.DataX.Pipeline;

// Instancie os Providers/Transformers
var reader = new MyDataReader();
var transformer = new MyDataTransformer();
var writer = new MyDataWriter();

var pipeline = PipelineBuilder.ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    .Build();

var stats = await pipeline.RunAsync(cancellationToken);

Console.WriteLine($"Processado {stats.Succeeded} itens com sucesso em {stats.Elapsed}.");
```

### Pipeline Avançado

Neste exemplo é demonstrado como tratar falhas com uma Dead-Letter Queue (DLQ), uso de batches, funções semelhantes ao LINQ integradas e paralelismo.

```csharp
using MVFC.DataX.Pipeline;

var pipeline = PipelineBuilder.ReadFrom(reader)
    // Transformações nativas disponíveis via extension methods
    .Skip(100)
    .Distinct()
    
    // Transformadores customizados (ex: Validadores, Mappers)
    .TransformWith(transformer)
    
    // Configurações avançadas na fluidez
    .WriteTo(writer)
    .WithBatchSize(500)
    .WithParallelism(4)
    .WithRetry(maxRetries: 3, delay: TimeSpan.FromSeconds(2))
    .WithChannelCapacity(5000)
    
    // Redireciona falhas (ex: ValidationError ou exception capturada no Map) para a DLQ
    .OnError(deadLetterWriter)
    
    // Hook disparado na finalização
    .OnCompleted(async stats => 
    {
        await notificationService.NotifyAsync($"Concluído. Falhas: {stats.Failed}");
    })
    .Build();

await pipeline.RunAsync(cancellationToken);
```
