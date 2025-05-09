# Contributing to dotnet-auth-server

Thank you for your interest in contributing! Contributions of all kinds are welcome — bug fixes, features, documentation improvements, and test coverage.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Git
- A text editor or IDE with C# support (Visual Studio, Rider, VS Code + C# Dev Kit)

## Building Locally

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-auth-server.git
cd dotnet-auth-server

# Restore dependencies
dotnet restore

# Build
dotnet build --configuration Release
```

## Running Tests

```bash
# Run all tests
dotnet test --configuration Release --verbosity normal

# Run with TRX output for CI-style results
dotnet test --configuration Release --verbosity normal --logger "trx;LogFileName=test-results.trx"

# Run a specific test project
dotnet test tests/dotnet-auth-server.Tests/ --verbosity normal
```

## Running Locally with Docker

```bash
# Build and start all services
docker compose up --build

# Development mode (with hot reload)
docker compose -f docker-compose.dev.yml up
```

## Code Style

- Follow the [EditorConfig](.editorconfig) settings already present in the repository.
- Use `PascalCase` for types and public members, `camelCase` for local variables and parameters.
- Prefer `var` only when the type is apparent from the right-hand side.
- Always use braces for control flow blocks.
- Add XML documentation comments (`/// <summary>`) to all public APIs.
- Keep existing author headers in files you edit.

## Pull Request Guidelines

1. **Fork** the repository and create a branch from `main`.
2. Branch names should follow the pattern: `feature/<short-description>` or `fix/<short-description>`.
3. Keep PRs focused — one logical change per PR.
4. Ensure all tests pass before opening a PR.
5. Add or update tests for any new behaviour.
6. Update relevant documentation (README, docs/) if your change affects public APIs or configuration.
7. Use clear, descriptive commit messages.

## Reporting Issues

Use [GitHub Issues](https://github.com/sarmkadan/dotnet-auth-server/issues). When filing a bug, include:
- .NET version (`dotnet --version`)
- OS and version
- Steps to reproduce
- Expected vs. actual behaviour

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
