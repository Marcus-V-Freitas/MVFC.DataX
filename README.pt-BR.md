# MVFC.DataX

[English](README.md) | [Português](README.pt-BR.md)

> Template base para bibliotecas .NET da família MVFC.

## Como usar

1. Clique em **Use this template** no GitHub (ou use o GitHub CLI)
2. O workflow `setup.yml` roda automaticamente e substitui os placeholders com base no nome do repositório
3. Atualize `Directory.Packages.props` com as versões dos pacotes necessários
4. Configure os secrets no repositório:
   - `NUGET_API_KEY` — chave da API do NuGet
   - `CODECOV_TOKEN` — token do Codecov (opcional; se ausente, o upload é ignorado automaticamente)

## Estrutura

```
.github/
  ISSUE_TEMPLATE/     # Templates de bug report e feature request
  workflows/
    ci.yml            # CI unificado: testes + publish (via tag)
    setup.yml         # Roda uma vez na criação e se auto-deleta
.config/
  dotnet-tools.json   # Manifest do Cake
src/                  # Projetos de biblioteca
tests/                # Projetos de teste
build.cake            # Script de build e cobertura
coverage.runsettings  # Configuração do coverlet
Directory.Build.props # Propriedades MSBuild compartilhadas
Directory.Build.targets
Directory.Packages.props # Versões centralizadas de pacotes
```

## CI/CD

| Evento | Jobs executados |
|---|---|
| PR para `main` | `test-and-coverage` |
| Push de tag `v*` | `test-and-coverage` + `build-and-publish` |

## Licença

[Apache 2.0](LICENSE)
