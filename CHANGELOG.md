# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.3.0] - 2026-11-07

### Added

- Added `ResilientDataWriter<T>` with `IAsyncDisposable` / `IDisposable` lifecycle support for disposing underlying writers.
- Separated `DeadLetterWriter`, `IgnoreWriter`, and `LogAndDiscardWriter` into individual dedicated files in `MVFC.DataX.Pipeline`.
- Added CsvHelper 34 source generator record mappings (`ProductMap`, `ProductResultMap`, `UserMap`) in the test suite.
- Added .NET SDK `10.0.100` (`rollForward: latestFeature`) configuration to `global.json`.
- Added `dotnet-reportgenerator-globaltool` (v5.3.11) and `dotnet-coverage` (v18.12.0) local tools in `.config/dotnet-tools.json`.
- Enabled central transitive package pinning (`CentralPackageTransitivePinningEnabled`) in `Directory.Packages.props`.
- Added OIDC authentication via `NuGet/login@v1` and dual publishing to GitHub Packages in CI workflow.

### Changed

- Upgraded `CsvHelper` to `34.0.0`: migrated `CsvDataReader` and `CsvDataWriter` from `CsvConfiguration` to `CsvReaderOptions` / `CsvWriterOptions` using factory methods `CsvReader.Create` and `CsvWriter.Create`.
- Updated `build.cake` to Cake 5.1.0 and changed execution to invoke restored local manifest tools (`dotnet tool run`).
- Replaced `SonarAnalyzer.CSharp` with `MVFC.Pack.Analyzers` (4.0.2) in `Directory.Build.targets`.
- Updated dependencies in `Directory.Packages.props` (`Microsoft.Extensions.Resilience` 10.11.0, `Microsoft.NET.Test.Sdk` 18.11.0, `Microsoft.Testing.Platform` 2.5.2, `Testcontainers` 4.16.0, `xunit.v3` 4.0.2, `Microsoft.Data.SqlClient` 7.1.1, `Google.Cloud.PubSub.V1` 3.39.0, `AWSSDK.SQS` 4.0.100.25, `MinVer` 8.0.0, `FluentValidation` 12.2.0, `MongoDB.Driver` 3.13.0, `coverlet.collector` 10.1.0).
- Modernized CI workflow (`.github/workflows/ci.yml`) with concurrency controls, branch verification for release tags, combined .NET 9/10 setup, and sticky PR coverage comments.
- Configured `.editorconfig` rules to ignore `MA0004` and `MA0015`.
- Standardized markdown tables and CI/Codecov badge links across repository and package documentation.

### Fixed

- Propagated cancellation tokens via `WithCancellation(ct)` to `transformedStream` in `PipelineEngine`.
- Added asynchronous record writing with `WriteRecordAsync` and propagated cancellation tokens in `CsvDataWriter`.
- Added explicit null validation (`ArgumentNullException.ThrowIfNull`) in `ErrorHandling.DeadLetter` and `ErrorHandling.LogAndDiscard`.
- Formatted `OrderByTransformer` exception messages with `CultureInfo.InvariantCulture`.

## [1.2.2] - 2026-08-27

### Added

- Added `global.json` configuring `Microsoft.Testing.Platform` as the test runner.

## [1.2.1] - 2026-08-27

### Fixed

- Added explicit null safety checks (`ArgumentNullException.ThrowIfNull`) and removed nullable suppressions in `CsvDataWriter`, `JsonlDataWriter`, and `RabbitMqDataWriter`.
- Added null-safe check on `response.Failed` in `SqsDataWriter`.
- Propagated cancellation token to `_channel.BasicCancelAsync` during reader shutdown in `RabbitMqDataReader`.

### Changed

- Configured graceful subscriber shutdown using `ShutdownMode.WaitForProcessing` in `PubSubDataReader`.

### Added

- Added `SSH.NET` package reference and asset metadata for `xunit.runner.visualstudio` in test suite.

## [1.2.0] - 2026-07-09

### Added

- Added exponential backoff retry support with jitter and max retry delay to `PipelineOptions` and `PipelineBuilder`.
- Added `OnError` exception classifier (`Skip`, `Retry`, `DeadLetter`, `Abort`) to `PipelineOptions` and `PipelineBuilder`.
- Added `OnRetry` telemetry callback to `PipelineOptions`.
- Added structured exception diagnostic metadata (`ExceptionType`, `StackTrace`) to `DataError.FromException`.
- Integrated `IPipelineMiddleware` execution in the pipeline engine and exposed `.Use()` fluent APIs in the builder.
- Added `maxCapacity` sliding-window option to `DistinctTransformer` and `maxItems` threshold check to `OrderByTransformer` to prevent OOM.

### Changed

- Optimized `PipelineEngine.RunAsync` to run producer and worker tasks concurrently with `Task.WhenAll`.
- Optimized `PipelineEngine.ProcessWorkerAsync` to recycle batch lists (`batch.Clear()`) instead of reallocating them.
- Unified unit and integration tests into the `MVFC.DataX.Tests` project, organizing them using xUnit `Category` traits.

## [1.1.0] - 2026-07-09

### Added

- Introduced `MVFC.DataX.Resilience` package for retry policies and fault tolerance.
- Implemented `IPipelineMiddleware` architecture for pipeline interception and hooks.
- Added `MergeReader` and `IQueryableReader` for merging and querying data sources.
- Added `CONTRIBUTING.md` guidelines.
- Added comprehensive tests for pipeline cancellation, error handling, memory constraints, and models coverage.

### Changed

- Improved transformers (`MapTransformer`, `FluentTransformer`) to explicitly return failures for null mapping results instead of skipping.
- Moved `PipelineOptions` to `MVFC.DataX.Core.Models`.
- Refactored global `using` directives across all projects for better organization.
- Updated core interfaces (`IDataReader`, `IDataTransformer`, `IDataWriter`) and `DataResult` to support new features.

## [1.0.1] - 2026-07-08

### Fixed

- Standardized `.csproj` files to include `README.md` and `icon.png` for NuGet packaging across providers.
- Simplified array initialization syntax in `SqsIntegrationTests.cs`.

## [1.0.0] - 2026-07-08

### Added

- **Initial release of the MVFC.DataX suite.**
- Created the core ETL/ELT architecture with `IDataReader<T>`, `IDataTransformer<TIn, TOut>`, and `IDataWriter<T>`.
- Implemented the `PipelineBuilder` and execution engine in `MVFC.DataX.Pipeline`.
- Added data providers for Postgres, MySQL, SQL Server, MongoDB, RabbitMQ, SQS, PubSub, and FileSystem (CSV/JSONL).
- Added `MVFC.DataX.Readers.Http` for bulk and streaming HTTP REST consumption.
- Added `MVFC.DataX.Validation` for FluentValidation integration.
- Standardized all `.csproj` files with NuGet metadata, `TargetFrameworks` (.NET 9/10), tags, and repository information.
- Created comprehensive `README.md` and `README.pt-br.md` files for the repository root and all 12 individual projects, featuring complete API signatures and real-world C# examples.

[1.3.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.2.2...v1.3.0
[1.2.2]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.2.1...v1.2.2
[1.2.1]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.0.0...v1.1.0
[1.0.1]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/releases/tag/v1.0.0
