# Contributing

Thanks for helping improve dotnet-auth-server. This guide covers how to build, test, and submit changes.

## Prerequisites

- **.NET SDK 10.0.x** (the projects target `net10.0`; CI uses `10.0.x`).
- Git.

## Build

```bash
dotnet restore
dotnet build                          # Debug
dotnet build --configuration Release  # what CI runs
```

The solution `dotnet-auth-server.sln` contains the host and the test project. The benchmark project (`dotnet-auth-server.Benchmarks/`) is not in the solution; run it separately with `dotnet run -c Release --project dotnet-auth-server.Benchmarks`.

## Test

```bash
dotnet test                                                 # all tests
dotnet test --filter "FullyQualifiedName~ConsentServiceTests"
```

Add or update tests for every behaviour change. Tests live in `tests/dotnet-auth-server.Tests/`, one file per class under test named `<Class>Tests.cs`.

## Coding conventions

- `.editorconfig` is authoritative: 4-space indentation, LF line endings, braces always, `var` only when the type is apparent.
- Run `dotnet format` before committing, and make sure `dotnet format --verify-no-changes` passes.
- Nullable reference types are enabled. Every `.cs` file starts with `#nullable enable` and the author banner block.
- Use file-scoped namespaces and place `using` directives after the namespace line.
- Public APIs need XML `/// <summary>` documentation (`GenerateDocumentationFile` is on).
- Naming: `PascalCase` for types and members, `camelCase` for locals and parameters, `_camelCase` for private fields, `Async` suffix on async methods.
- Test methods are named `Method_Scenario_ExpectedResult` and use `// Arrange / Act / Assert` comments.
- Services and controllers throw typed `AuthServerException` subclasses. Do not add try/catch in controllers for these.

## Project layout

| Path | Role |
|---|---|
| `Program.cs` | Host and composition root: options binding, DI, middleware order, minimal endpoints |
| `src/` | All library code (`DotnetAuthServer.Core`), root namespace `DotnetAuthServer` |
| `src/Controllers/`, `src/Services/`, `src/Handlers/` | HTTP endpoints, domain logic, protocol handlers |
| `src/Data/Repositories/`, `src/Domain/` | Repository interfaces and in-memory implementations; entities, models, enums |
| `tests/dotnet-auth-server.Tests/` | xUnit test project |
| `docs/` | Architecture write-up and per-class notes |

## Branch naming

Create a branch from an up-to-date `main` with a short, lowercase, hyphenated name and a type prefix:

- `feature/<short-description>`
- `fix/<short-description>`
- `docs/<short-description>`

## Commit messages

Use a short, imperative subject with a conventional prefix: `feat:`, `fix:`, `docs:`, `refactor:`, `test:`, or `chore:`. For example: `fix(consent): reject expired consent requests`. Keep the subject under about 72 characters and explain the reasoning in the body when it is not obvious from the diff.

## Pull request process

1. Open your pull request against `main`.
2. CI must pass. It runs `dotnet build` and `dotnet test` in Release, and CodeQL analysis.
3. At least one maintainer approval is required before merging.
4. Keep each PR focused on one logical change, and fill in the PR template.

## Reporting issues

Use [GitHub Issues](https://github.com/sarmkadan/dotnet-auth-server/issues). Include your .NET SDK version, steps to reproduce, and expected versus actual behaviour. For security problems, follow [SECURITY.md](SECURITY.md) instead of opening a public issue.

## License

By contributing, you agree that your contributions will be licensed under the [MIT License](LICENSE).
