# MVFC.DataX.Validation

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Validation.svg)](https://www.nuget.org/packages/MVFC.DataX.Validation)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Validation.svg)](https://www.nuget.org/packages/MVFC.DataX.Validation)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Provedor de validações para o `MVFC.DataX` provendo integração profunda com a popular biblioteca `FluentValidation`. Permite que você valide dados dinamicamente enquanto fluem no pipeline de forma asíncrona.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Validation
```

## Classes Disponíveis

| Classe | Descrição |
|---|---|
| `FluentTransformer<TInput, TOutput>` | Implementa um `IDataTransformer` atuando simultaneamente em conversão (mapping) de dados e validações regras do `IValidator<TOutput>`. Validações rejeitadas são automaticamente convertidas para o fluxo nulo de `DataResult.Failure` repassando todos os erros de propriedades internas para a Engine de forma legível. |

## Assinaturas da API

```csharp
public FluentTransformer(
    Func<TInput, TOutput?> mapFunc,
    IValidator<TOutput> validator)
```

## Uso / Exemplo

```csharp
using FluentValidation;
using MVFC.DataX.Pipeline;
using MVFC.DataX.Validation;

// 1. Defina sua entidade e suas regras do FluentValidation Validator
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

// 2. Setup do pipeline padrão
var reader = new MyDataReader();
var deadLetterWriter = new MyDlqWriter();

// 3. Crie a interface Fluent do Transformer embutindo o map/conversor e o validador
var validator = new OrderValidator();
var transformer = new FluentTransformer<IncomingData, Order>(
    mapFunc: dto => new Order { Quantity = dto.Qty, Price = dto.Val },
    validator: validator
);

// 4. Orquestrando
var pipeline = PipelineBuilder.ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    // Quaisquer entidades onde a validação negou regras serão enviadas com todos
    // detalhes do por que a regra falhou, e também o Valor Tentado de volta a classe OnError
    .OnError(deadLetterWriter)
    .Build();

await pipeline.RunAsync(cancellationToken);
```
