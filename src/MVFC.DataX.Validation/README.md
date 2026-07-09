# MVFC.DataX.Validation

> 🇧🇷 [Leia em Português](README.pt-br.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Validation.svg)](https://www.nuget.org/packages/MVFC.DataX.Validation)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Validation.svg)](https://www.nuget.org/packages/MVFC.DataX.Validation)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provides integration between `MVFC.DataX` and the popular `FluentValidation` library, allowing you to validate data as it streams through your pipelines.

This package is part of the **MVFC.DataX** suite. For the full documentation and more examples, please check the [main repository README](../../README.md).

## Installation

```sh
dotnet add package MVFC.DataX.Validation
```

## Available Classes

| Class | Description |
|---|---|
| `FluentTransformer<TInput, TOutput>` | An `IDataTransformer` that maps an input item to an output item, then executes a FluentValidation `IValidator<TOutput>`. Failures automatically translate into `DataResult.Failure` with specific `DataError` items populated via the framework's detailed property errors. |

## API Signatures

```csharp
public FluentTransformer(
    Func<TInput, TOutput?> mapFunc,
    IValidator<TOutput> validator)
```

## Usage / Example

```csharp
using FluentValidation;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Validation;

// 1. Define your object and its FluentValidation Validator
public class Order
{
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}

public class OrderValidator : AbstractValidator<Order>
{
    public OrderValidator()
    {
        RuleFor(x => x.Quantity).GreaterThan(0);
        RuleFor(x => x.Price).GreaterThan(0);
    }
}

// 2. Setup standard Readers / DLQs
var reader = new MyDataReader();
var deadLetterWriter = new MyDlqWriter();

// 3. Create the Fluent Transformer using the map function and the rules instance
var validator = new OrderValidator();
var transformer = new FluentTransformer<IncomingData, Order>(
    mapFunc: dto => new Order { Quantity = dto.Qty, Price = dto.Val },
    validator: validator
);

// 4. Orchestrate
var pipeline = PipelineBuilder.ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    // Any items that failed the rules in OrderValidator will be redirected to the DLQ 
    // seamlessly by the Pipeline Engine
    .OnError(deadLetterWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
