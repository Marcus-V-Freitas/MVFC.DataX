# Contributing to MVFC.DataX

First off, thank you for considering contributing to MVFC.DataX! It's people like you that make this library better for everyone.

## Development Workflow

### 1. Branch Naming
Please follow standard conventions for naming your branches:
- `feat/feature-name` — for new features
- `fix/bug-name` — for bug fixes
- `docs/doc-name` — for documentation updates
- `refactor/refactor-name` — for code refactorings

### 2. Local Setup
Ensure you have the latest .NET 9.0+ SDK installed.
Run `dotnet build` from the root directory to ensure everything compiles correctly.

### 3. Tests & Testcontainers
We use **Testcontainers** extensively for our integration tests (e.g. Postgres, RabbitMQ, MongoDB). 
To run the full test suite locally, you must have [Docker](https://www.docker.com/) running on your machine.

Run the tests using the .NET CLI or our Cake build script:
```sh
dotnet test
```
Or via Cake:
```sh
dotnet cake
```

Make sure all tests pass before submitting your Pull Request. If you are adding a new feature or fixing a bug, please include tests that validate your changes.

### 4. Pull Requests
1. Fork the repository and create your branch from `main`.
2. Make your changes and commit them.
3. Open a Pull Request against the `main` branch.
4. Ensure the CI pipeline (GitHub Actions) passes. We enforce a 100% pass rate on all tests.
5. Once approved by a maintainer, your PR will be merged.

## Coding Style
- We follow standard C# formatting conventions.
- Keep the `Usings.cs` global using declarations centralized per project. Avoid adding `using` statements inside individual `.cs` files unless strictly necessary for alias resolution.
- Please add `/// <summary>` XML documentation for any new public interfaces or classes.

Thank you for your contributions!
