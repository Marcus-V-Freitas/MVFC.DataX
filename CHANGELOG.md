# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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

[1.0.1]: https://github.com/Marcus-V-Freitas/MVFC.DataX/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/Marcus-V-Freitas/MVFC.DataX/releases/tag/v1.0.0
