# MVFC.DataX

[English](README.md) | [Português](README.pt-BR.md)

> Base template for .NET libraries in the MVFC family.

## How to use

1. Click **Use this template** on GitHub (or use the GitHub CLI).
2. The `setup.yml` workflow runs automatically and replaces placeholders based on the repository name.
3. Update `Directory.Packages.props` with the required package versions.
4. Configure the secrets in the repository:
   - `NUGET_API_KEY` — NuGet API key.
   - `CODECOV_TOKEN` — Codecov token (optional; if missing, upload is ignored automatically).

## Structure

```
.github/
  ISSUE_TEMPLATE/          # Bug report and feature request templates
  workflows/
    ci.yml                 # Unified CI: tests + publish (via tag)
    setup.yml              # Runs once on creation and deletes itself
.config/
  dotnet-tools.json        # Cake manifest
src/                       # Library projects
tests/                     # Test projects
build.cake                 # Build and coverage script
coverage.runsettings       # Coverlet configuration
Directory.Build.props      # Shared MSBuild properties
Directory.Build.targets
Directory.Packages.props  # Centralized package versions
```

## CI/CD

| Event | Executed Jobs |
|---|---|
| PR to `main` | `test-and-coverage` |
| Tag push `v*` | `test-and-coverage` + `build-and-publish` |

## License

[Apache 2.0](LICENSE)
