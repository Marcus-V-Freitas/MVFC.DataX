# MVFC.DataX.Resilience

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Resilience.svg)](https://www.nuget.org/packages/MVFC.DataX.Resilience)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Resilience.svg)](https://www.nuget.org/packages/MVFC.DataX.Resilience)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Extensões de resiliência para pipelines do MVFC.DataX, integrando-se com o `Microsoft.Extensions.Resilience` para fornecer tolerância a falhas robusta (retry, circuit breaker, timeout, rate limiting) nos gravadores de dados.

Este pacote faz parte do conjunto **MVFC.DataX**. Para obter a documentação completa e mais exemplos, consulte o [README principal do repositório](../../README.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Resilience
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `PipelineBuilderExtensions` | Fornece extensões para encapsular writers com pipelines do Microsoft.Extensions.Resilience. |

## Uso / Exemplo

```csharp
using MVFC.DataX.Pipeline;
using MVFC.DataX.Resilience;
using Polly; // Normalmente usado para construir ResiliencePipelines

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
