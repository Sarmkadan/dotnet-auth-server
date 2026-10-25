# TokenService

**Namespace:** none declared in the source file (global namespace)
**Source:** `src/Services/TokenService.cs`

Public service class intended to sit in the OAuth 2.0 token issuance pipeline: the token endpoint hands a token request to `TokenService`, which dispatches on `grant_type` and issues the resulting tokens.

> **Status:** `TokenService.cs` currently declares only the class and its summary comment. It has no constructor, no methods, no properties, and no fields. The members below are therefore none. The pipeline role described above is the intended design from `CLAUDE.md`. It is not implemented in this file yet.

---

## Public surface

| Member kind | Members |
|---|---|
| Constructors | None declared (the compiler-generated default parameterless constructor applies) |
| Methods | None |
| Properties | None |
| Fields | None |
| Events | None |

---

## Pipeline role (intended)

Per the project architecture (`CLAUDE.md`), the token issuance flow is:

1. The controller parses the HTTP token request and delegates to the service layer.
2. `TokenService` dispatches on the `grant_type` value (authorization code, client credentials, refresh token, password).
3. Validation is delegated to the concern-specific services: `ClientValidationService`, `ScopeValidationService`, and `PkceValidationService`.
4. Errors are raised as typed `AuthServerException` subclasses. `ErrorHandlingMiddleware` converts them to OAuth `{"error","error_description"}` responses.

Once the members listed above exist, this page should document them in the same format as the other per-class notes in `docs/`.

---

## Known gaps between this file and its consumers

- `examples/BasicUsage.cs`, `examples/AdvancedUsage.cs`, and `examples/IntegrationExample.cs` register `TokenService` with `AddScoped<TokenService>()` and resolve it from the container. Examples are excluded from compilation.
- The benchmark project (`dotnet-auth-server.Benchmarks/`) calls `new TokenService(...)` with constructor arguments and calls `HandleTokenRequestAsync(...)`. Neither exists in this file. The benchmark project is not part of the solution.

Until the file gains these members, those call sites cannot compile against it.
