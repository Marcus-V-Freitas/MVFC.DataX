# MVFC.DataX.Core

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![NuGet](https://img.shields.io/nuget/v/MVFC.DataX.Core.svg)](https://www.nuget.org/packages/MVFC.DataX.Core)
[![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Core.svg)](https://www.nuget.org/packages/MVFC.DataX.Core)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](../../LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Abstrações base e interfaces (`IDataReader<T>`, `IDataTransformer<TIn, TOut>`, `IDataWriter<T>`) para a suíte de ETL/ELT MVFC.DataX.

Este pacote faz parte da suíte **MVFC.DataX**. Para a documentação completa e mais exemplos, por favor consulte o [README do repositório principal](../../README.pt-br.md).

## Instalação

```sh
dotnet add package MVFC.DataX.Core
```

## Classes Disponíveis

| Classe | Tipo | Descrição |
|---|---|---|
| `IDataReader<T>` | Interface | Contrato de leitura — `ReadAsync()` retorna `IAsyncEnumerable<T>` |
| `IDataTransformer<TIn, TOut>` | Interface | Contrato de transformação — `TransformAsync()` retorna `IAsyncEnumerable<DataResult<TOut>>` |
| `IDataWriter<T>` | Interface | Contrato de escrita — `WriteAsync()` e `WriteBatchAsync()` |
| `DataResult<T>` | Record | Wrapper de resultado — `IsSuccess`, `Value`, `Errors`. Contém factories `DataResult.Success<T>()` e `DataResult.Failure<T>()` |
| `DataError` | Record | Erro em dados contendo: `PropertyName`, `ErrorMessage`, `AttemptedValue` |
| `PipelineStatistics` | Record | Estatísticas de execução: `TotalRead`, `Succeeded`, `Failed`, `Skipped`, `Elapsed`, `Errors` |
| `EnumerableReader<T>` | Classe | Encapsula um `IEnumerable<T>` ou `IAsyncEnumerable<T>` em um `IDataReader<T>` |
| `ChannelDataReader<T>` | Classe | Reader que consome dados a partir de um `ChannelReader<T>` |
| `InMemoryWriter<T>` | Classe | Writer que acumula itens numa coleção `ConcurrentBag<T>` exposta pela propriedade `Items` |
| `DelegateWriter<T>` | Classe | Writer que delega a escrita em batch a um delegate `Func<IReadOnlyList<T>, CancellationToken, Task>` |
| `MapTransformer<TIn, TOut>` | Classe | Aplica uma função de transformação, capturando exceções como `DataResult.Failure` |
| `FilterTransformer<T>` | Classe | Filtra os dados através de um predicado (condição) |
| `BatchTransformer<T>` | Classe | Agrupa dados em lotes `IReadOnlyList<T>` de tamanho fixo |
| `SkipTransformer<T>` / `TakeTransformer<T>` | Classe | Componentes de paginação |
| `DistinctTransformer<T>` | Classe | Remove duplicatas usando um comparador |
| `OrderByTransformer<T, TKey>` | Classe | Ordenação de fluxo em memória |
| `FlatMapTransformer<TIn, TOut>` | Classe | Expansão 1-N (Achata listas aninhadas em fluxo contínuo) |
| `AggregateTransformer<TIn, TAcc>` | Classe | Acumulação de estado (fold) |

## Uso / Exemplo

Normalmente o motor do pacote `MVFC.DataX.Pipeline` orquestra essas classes automaticamente, mas elas podem ser instanciadas de modo livre.

```csharp
using MVFC.DataX.Core.Models;
using MVFC.DataX.Core.Readers;
using MVFC.DataX.Core.Transformers;
using MVFC.DataX.Core.Writers;

var sourceData = new[] { 1, 2, 3, 4, 5 };

// 1. Cria um reader a partir de uma coleção em memória
var reader = new EnumerableReader<int>(sourceData);

// 2. Cria um transformer (mapa) com tratamento de erro
var transformer = new MapTransformer<int, int>(x => 
{
    if (x % 2 != 0) throw new Exception("Ímpares não permitidos!");
    return x * 2;
});

// 3. Cria um in-memory writer
var writer = new InMemoryWriter<int>();

// 4. Executa manualmente as abstrações
await foreach (var item in reader.ReadAsync())
{
    var transformedStream = transformer.TransformAsync(
        new[] { item }.ToAsyncEnumerable()
    );

    await foreach (var result in transformedStream)
    {
        if (result.IsSuccess)
        {
            await writer.WriteAsync(result.Value);
        }
        else
        {
            Console.WriteLine($"Erro capturado: {result.Errors[0].ErrorMessage}");
        }
    }
}

// 5. Inspeciona a coleção final
foreach (var written in writer.Items)
{
    Console.WriteLine($"Gravado: {written}"); // Imprime: 4, 8
}
```
