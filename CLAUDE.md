# CLAUDE.md

## Project overview

OAuth 2.0 / OpenID Connect authorization server for .NET 10 (ASP.NET Core): auth code + PKCE, client credentials, refresh/password grants, introspection, revocation, dynamic client registration, consent, TOTP MFA, sessions. Storage is in-memory only (repository interfaces are the seam for persistence).

## Build

```bash
dotnet restore
dotnet build                          # Debug
dotnet build --configuration Release  # what CI runs
dotnet run --project dotnet-auth-server.csproj   # start host (Swagger at /swagger in Development)
make build | make run | make dev      # Makefile wrappers (dev = dotnet watch run)
docker build -t dotnet-auth-server:latest .
```

Solution `dotnet-auth-server.sln` contains the host and the test project. The benchmark project (`dotnet-auth-server.Benchmarks/`, BenchmarkDotNet) is not in the solution; run it with `dotnet run -c Release --project dotnet-auth-server.Benchmarks`.

## Test

```bash
dotnet test                                  # all tests (xUnit, ~1000 [Fact]/[Theory])
dotnet test --filter "FullyQualifiedName~ConsentServiceTests"
dotnet test --configuration Release --no-build   # CI form
```

Conventions:
- Test project: `tests/dotnet-auth-server.Tests/`, namespace `DotnetAuthServer.Tests`, one file per class under test named `<Class>Tests.cs`.
- xUnit + FluentAssertions + Moq are referenced; most tests use plain `Assert.*` and hand-written fakes (e.g. `FakeConsentRepository`) or in-memory repositories rather than mocks.
- Services are constructed directly in the test constructor (no DI container, no `WebApplicationFactory`).
- Method naming: `Method_Scenario_ExpectedResult`, with `// Arrange / Act / Assert` comments.
- Test classes are `public sealed`.

## Lint / Format

```bash
dotnet format          # applies .editorconfig
dotnet format --verify-no-changes
```

No analyzers beyond the SDK defaults. `.editorconfig` is authoritative: 4-space indent, LF, `var` only when the type is apparent, braces always, PascalCase types/members, camelCase locals, `_camelCase` private fields.

## Architecture

| Path | Role |
|---|---|
| `Program.cs` + `dotnet-auth-server.csproj` | Thin host / composition root: options binding, DI, middleware order, minimal-API endpoints (`/.well-known/*`, `/health`). Compiles nothing from `src/` itself; it references `src/DotnetAuthServer.Core.csproj`. |
| `src/` (`DotnetAuthServer.Core`, `OutputType=Library`) | All real code. Root namespace `DotnetAuthServer`. |
| `src/Controllers/` | Attribute-routed MVC, thin: parse HTTP, delegate to services/handlers. |
| `src/Services/` | Domain logic. `TokenService.HandleTokenRequestAsync` dispatches on `grant_type`. Validation split by concern: `ClientValidationService`, `ScopeValidationService`, `PkceValidationService`. |
| `src/Handlers/` | Protocol endpoints reusable from controllers and minimal APIs (introspection, revocation, userinfo, JWKS, device flow). |
| `src/Data/Repositories/` | `IRepository<T,TKey>` + per-aggregate interfaces; all implementations are `ConcurrentDictionary`-backed singletons. |
| `src/Domain/` | `Entities/` (mutable POCOs), `Models/` (DTOs), `Enums/`. |
| `src/Middleware/` | Pipeline order: RequestContext -> Logging -> RateLimiting -> ErrorHandling. |
| `src/Exceptions/` | `AuthServerException` hierarchy (`InvalidGrant`, `InvalidClient`, `InvalidScope`, `Validation`, ...). |
| `src/Security/`, `src/Caching/`, `src/Events/`, `src/Configuration/`, `src/Integration/`, `src/BackgroundWorkers/` | Cross-cutting: rate limiters, revoked-token store, `ICacheService`, in-process `IEventPublisher`, options classes, OPA/webhook clients. |
| `docs/ARCHITECTURE.md` | Authoritative design write-up incl. known limitations. `docs/*.md` are per-class notes. |
| `examples/` | Client samples; excluded from compilation. |

Configuration binds from the `DotnetAuthServer` section (`DotnetAuthServerOptions`, validated with data annotations on start); see `appsettings.example.json`. `JwtSigningKey` must come from the environment outside local dev. Tokens are HS256 by default.

## Conventions

- Every `.cs` file starts with `#nullable enable` and the author banner comment block; keep both on new files.
- File-scoped namespaces; `using` directives placed after the namespace line.
- Nullable and implicit usings enabled; `LangVersion latest`; collection expressions (`[]`) used for initializers.
- XML `/// <summary>` docs on all public APIs (`GenerateDocumentationFile` is on; missing docs produce warnings).
- Error handling: services and controllers throw typed `AuthServerException` subclasses carrying `ErrorCode` / `StatusCode` / `ErrorDescription`; `ErrorHandlingMiddleware` converts them to OAuth `{"error","error_description"}` JSON. Do not add try/catch in controllers for these.
- DI: repositories, rate limiters, `RevokedTokenStore`, `ICacheService`, `IEventPublisher` are singletons; services and handlers are scoped. Services are registered as concrete types (no interface) unless a second implementation is expected (`ITokenIssuer`, `ITokenValidator`, `IAuditLoggingService`, `ICacheService`, repositories). Options are injected as plain `AuthServerOptions`/`CacheOptions`/etc. singletons, not `IOptions<T>`.
- Partial-concern helpers live in sibling files named `<Class>Extensions.cs`, `<Class>Validation.cs`, `<Class>JsonExtensions.cs`.
- Naming: `PascalCase` types/members, `camelCase` locals/params, `_camelCase` private fields, `Async` suffix on async methods.
- Commits: short imperative subject with a conventional prefix (`docs:`, `chore:`, `feat:`, `fix:`).
