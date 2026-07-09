# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.2.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.0.0...v1.1.0
[1.0.1]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/releases/tag/v1.0.0
