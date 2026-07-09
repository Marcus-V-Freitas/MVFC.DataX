# MVFC.DataX

> 🇺🇸 [Read in English](README.md)

[![CI](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml/badge.svg)](https://github.com/Marcus-V-Freitas/MVFC.DataX/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX/branch/main/graph/badge.svg)](https://codecov.io/gh/Marcus-V-Freitas/MVFC.DataX)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)
![Platform](https://img.shields.io/badge/.NET-9%20%7C%2010-blue)

Uma suíte completa de bibliotecas em .NET para construção de pipelines de dados (ETL/ELT) assíncronos e robustos. Com uma arquitetura extensível e conectável, você pode ler, transformar e escrever dados entre múltiplos bancos de dados, sistemas de mensageria e APIs usando uma abstração única de pipeline.

## Motivação

Trabalhar com pipelines de dados e processos de ETL em .NET frequentemente significa:

- Acoplar fortemente seu código a SDKs específicos (SQL Server, MongoDB, SQS, RabbitMQ, etc.).
- Reescrever a lógica de ingestão, transformação e carga toda vez que a origem de dados muda.
- Duplicar lógica de orquestração, processamento em lote e tratamento de erros.
- Dificuldade para testar o fluxo de dados sem rodar infraestrutura real.

O **MVFC.DataX** resolve isso fornecendo uma camada limpa de abstração — `IDataReader<T>`, `IDataTransformer<TIn, TOut>` e `IDataWriter<T>` — combinada com um poderoso orquestrador de Pipeline. Você escolhe os provedores para sua origem e destino, e obtém uma API consistente e assíncrona para executar seus fluxos de dados. Trocar um banco de dados por um broker de mensagens exige apenas mudar a instanciação do provedor; sua regra de negócios permanece intacta.

## Arquitetura

Todos os pacotes seguem o mesmo padrão baseado nas abstrações do Core:

- `IDataReader<T>` — contrato para ler dados como um `IAsyncEnumerable<T>`.
- `IDataTransformer<TIn, TOut>` — contrato para transformar dados de forma assíncrona.
- `IDataWriter<T>` — contrato para escrever dados (individualmente ou em lote).
- `PipelineBuilder` — orquestra a leitura, transformação e escrita dos dados, lidando com erros e batches.
- Cada pacote provedor implementa essas interfaces usando os SDKs nativos.

Depois que você entende como usar um provedor, todos os outros funcionam de forma idêntica no pipeline.

## Pacotes

| Pacote | Origem/Destino | Downloads |
|---|---|---|
| [MVFC.DataX.Core](src/MVFC.DataX.Core/README.md) | Abstrações Base | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Core) |
| [MVFC.DataX.Pipeline](src/MVFC.DataX.Pipeline/README.md) | Orquestrador de Pipeline | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Pipeline) |
| [MVFC.DataX.Providers.FileSystem](src/MVFC.DataX.Providers.FileSystem/README.md) | Arquivos Locais/Rede | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.FileSystem) |
| [MVFC.DataX.Providers.MongoDB](src/MVFC.DataX.Providers.MongoDB/README.md) | MongoDB | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MongoDB) |
| [MVFC.DataX.Providers.MySql](src/MVFC.DataX.Providers.MySql/README.md) | MySQL | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.MySql) |
| [MVFC.DataX.Providers.Postgres](src/MVFC.DataX.Providers.Postgres/README.md) | PostgreSQL | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.Postgres) |
| [MVFC.DataX.Providers.PubSub](src/MVFC.DataX.Providers.PubSub/README.md) | Google Pub/Sub | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.PubSub) |
| [MVFC.DataX.Providers.RabbitMQ](src/MVFC.DataX.Providers.RabbitMQ/README.md) | RabbitMQ | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.RabbitMQ) |
| [MVFC.DataX.Providers.SQS](src/MVFC.DataX.Providers.SQS/README.md) | Amazon SQS | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SQS) |
| [MVFC.DataX.Providers.SqlServer](src/MVFC.DataX.Providers.SqlServer/README.md) | SQL Server | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Providers.SqlServer) |
| [MVFC.DataX.Readers.Http](src/MVFC.DataX.Readers.Http/README.md) | APIs HTTP | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Readers.Http) |
| [MVFC.DataX.Validation](src/MVFC.DataX.Validation/README.md) | Validação de Dados | ![Downloads](https://img.shields.io/nuget/dt/MVFC.DataX.Validation) |

***

## Instalação

Instale os pacotes Core e Pipeline, e depois escolha os provedores que você precisa:

```sh
# Abstrações base e orquestrador (sempre necessários)
dotnet add package MVFC.DataX.Core
dotnet add package MVFC.DataX.Pipeline

# Escolha os provedores de origem/destino (exemplos)
dotnet add package MVFC.DataX.Providers.Postgres
dotnet add package MVFC.DataX.Providers.SQS
```

## Guia de Início Rápido

### 1. Defina seus modelos de dados

```csharp
public record UserRecord(int Id, string Name, string Email);
public record UserDto(string FullName, string ContactEmail);
```

### 2. Implemente um Transformer (Opcional)

```csharp
using System.Runtime.CompilerServices;
using MVFC.DataX.Core.Abstractions;
using MVFC.DataX.Core.Models;

public class UserTransformer : IDataTransformer<UserRecord, UserDto>
{
    public async IAsyncEnumerable<DataResult<UserDto>> TransformAsync(
        IAsyncEnumerable<UserRecord> input,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in input.WithCancellation(ct))
        {
            // Transforme e retorne embrulhado em um DataResult
            yield return DataResult<UserDto>.Success(
                new UserDto(item.Name, item.Email)
            );
        }
    }
}
```

### 3. Construa e execute o pipeline

```csharp
using MVFC.DataX.Pipeline;

// Instancie seu leitor e escritor (usando PostgreSQL para SQS como exemplo)
var reader = new PostgresDataReader<UserRecord>(postgresConnectionString, "SELECT * FROM Users");
var writer = new SqsDataWriter<UserDto>(sqsClient, queueUrl);
var transformer = new UserTransformer();

// Construa o pipeline
var pipeline = PipelineBuilder.Create()
    .ReadFrom(reader)
    .TransformWith(transformer)
    .WriteTo(writer)
    .Build();

// Execute o pipeline
await pipeline.ExecuteAsync(cancellationToken);
```

## API Disponível

### Métodos do Reader (Leitor)
| Método | Descrição |
|---|---|
| `ReadAsync(CancellationToken ct)` | Retorna um `IAsyncEnumerable<T>` representando o fluxo de dados. |

### Métodos do Transformer
| Método | Descrição |
|---|---|
| `TransformAsync(IAsyncEnumerable<TIn> input, CancellationToken ct)` | Processa o fluxo e retorna os itens transformados. |

### Métodos do Writer (Escritor)
| Método | Descrição |
|---|---|
| `WriteAsync(T item, CancellationToken ct)` | Escreve um único item no destino. |
| `WriteBatchAsync(IReadOnlyList<T> items, CancellationToken ct)` | Escreve um lote (batch) de itens no destino. |

## Estrutura do Projeto

```text
src/
  MVFC.DataX.Core/                   # Abstrações base (interfaces)
  MVFC.DataX.Pipeline/               # Motor de execução do pipeline
  MVFC.DataX.Validation/             # Componentes de validação de dados
  MVFC.DataX.Readers.Http/           # Leitor de APIs HTTP
  MVFC.DataX.Providers.*/            # Implementações para tecnologias específicas
tests/
  MVFC.DataX.Tests/                  # Testes unitários e de integração
```

## Requisitos

- .NET 9.0+
- O SDK subjacente para cada provedor (baixado automaticamente via NuGet)

## Contribuindo

Veja [CONTRIBUTING.md](CONTRIBUTING.md).

## Licença

[Apache-2.0](LICENSE)
