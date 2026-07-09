# MVFC.DataX.Resilience

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Resilience.svg)](https://www.nuget.org/packages/MVFC.DataX.Resilience)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Resilience.svg)](https://www.nuget.org/packages/MVFC.DataX.Resilience)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Resilience extensions for MVFC.DataX pipelines, integrating with `Microsoft.Extensions.Resilience` to provide robust fault tolerance (retry, circuit breaker, timeout, rate limiting) on data writers.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Resilience
```

## Available Classes

| Class | Description |
|---|---|
| `PipelineBuilderExtensions` | Provides extensions to wrap writers with Microsoft.Extensions.Resilience pipelines. |

## Usage / Example

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Resilience;
using Polly; // Typically used to build ResiliencePipelines

var resiliencePipeline = new ResiliencePipelineBuilder()
    .AddRetry(new()
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromSeconds(1)
    })
    .Build();

var pipeline = PipelineBuilder.ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    .WithResiliencePolicy(resiliencePipeline)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
