# Architecture

This document describes the solution as it actually is in the code today - not the
aspirational version. If something is a limitation, it is listed as a limitation.

## Overview

dotnet-auth-server is an OAuth 2.0 / OpenID Connect authorization server built on
ASP.NET Core (net10.0). It is split into two projects plus tests and benchmarks:

| Project | Path | Role |
|---|---|---|
| `DotnetAuthServer` | `Program.cs` + root csproj | The web host: DI wiring, middleware pipeline, minimal-API metadata endpoints |
| `DotnetAuthServer.Core` | `src/DotnetAuthServer.Core.csproj` | Everything else: controllers, services, handlers, domain, repositories |
| Tests | `tests/dotnet-auth-server.Tests` | xUnit tests over services and entities |
| Benchmarks | `dotnet-auth-server.Benchmarks` | BenchmarkDotNet suites (token, PKCE, scope/client validation, introspection, revocation) |

The root project deliberately compiles nothing from `src/`, `tests/`, `examples/`
(`<Compile Remove>` in the csproj) and consumes the core purely as a project
reference. Rationale: the core is a library (`OutputType=Library`) that can be
packed and reused, while the host stays a thin composition root.

## Component breakdown

### Composition root - `Program.cs`

- Binds `DotnetAuthServerOptions` from the `DotnetAuthServer` configuration section
  and `WebhookOptions` from `Webhooks`, both with `ValidateDataAnnotations()` +
  `ValidateOnStart()` so a misconfigured server fails at boot, not on first request.
- Re-registers the nested option objects (`AuthServerOptions`, `CacheOptions`,
  `LoggingOptions`, `OpaOptions`) as plain singletons. This is a backward-compat
  bridge: most services take `AuthServerOptions` directly in their constructors
  instead of `IOptions<T>`. Trade-off: simpler constructors, but no live reload of
  configuration (acceptable - signing keys and lifetimes should not hot-swap anyway).
- Registers the `OpaClient` HttpClient only when `DotnetAuthServer:Opa:Enabled` is
  true, so the HttpClient factory does not hold connections to a policy engine
  nobody configured.
- Maps three minimal-API endpoints itself: the two discovery documents
  (`/.well-known/oauth-authorization-server`, `/.well-known/openid-configuration`),
  `/.well-known/jwks.json`, and `/health`. Everything else is attribute-routed MVC.

### Middleware - `src/Middleware/`

Pipeline order (deliberate, set in `Program.cs`):

```
RequestContextMiddleware  -> assigns/propagates a request correlation id
LoggingMiddleware         -> request/response logging
RateLimitingMiddleware    -> in-process token-bucket per client key, stricter buckets for token/authorize endpoints
ErrorHandlingMiddleware   -> maps AuthServerException hierarchy to OAuth error JSON
```

Error handling sits after rate limiting on purpose: a rate-limit rejection is a
short-circuit response, not an exception, so it does not need the translator; but
everything downstream (controllers, services) throws typed exceptions
(`InvalidGrantException`, `InvalidClientException`, `ValidationException`, ...)
that the middleware converts to spec-shaped `{"error": "...", "error_description": "..."}`
bodies with the right status code. That keeps controllers free of try/catch noise.

### Controllers - `src/Controllers/`

Attribute-routed, thin; they parse/shape HTTP and delegate:

| Controller | Route | Delegates to |
|---|---|---|
| `AuthorizationController` | `oauth/authorize` (+ `consent`) | `AuthorizationService`, `ConsentService` |
| `TokenController` | `oauth/token`, `/oauth/introspect`, `/oauth/revoke` | `TokenService`, `TokenIntrospectionHandler`, `TokenRevocationHandler` |
| `ClientRegistrationController` | `register` | `DynamicClientRegistrationService` (RFC 7591) |
| `UserManagementController` | `api/users` | `UserManagementService` |
| `SessionManagementController` | `api/sessions` | `UserSessionService` |
| `MfaController` | `api/users/{userId}/mfa` | `TotpService` |

### Services - `src/Services/`

The domain logic layer. The center of gravity is `TokenService`
(`HandleTokenRequestAsync` dispatches on `grant_type` to authorization_code,
refresh_token, client_credentials and password grant handlers). Around it:

- `AuthorizationService` / `ConsentService` - authorization code flow + consent state.
- `ClientValidationService`, `ScopeValidationService`, `PkceValidationService` -
  request validation split by concern (also what the benchmark project measures).
- `PolicyEnforcementService` - ABAC hook; optionally consults OPA via `OpaClient`.
- `UserManagementService`, `UserSessionService`, `TotpService` - admin/user-facing
  features (user CRUD, session dashboard, TOTP MFA).
- `SecretsService`, `ClaimsEnrichmentService`, `AuditLoggingService`,
  `SessionStateService`, `DynamicClientRegistrationService`, `WebAuthnService`.

Services are registered as concrete types, not interfaces. That is a conscious
shortcut: there is exactly one implementation of each and the tests construct them
directly with in-memory repositories. The repository layer is where the
abstraction boundary was actually needed, and there it exists (see below).

### Handlers - `src/Handlers/`

Protocol endpoints that are not full controllers: `TokenIntrospectionHandler`
(RFC 7662), `TokenRevocationHandler` (RFC 7009), `UserinfoHandler`,
`JwksHandler`, `DeviceFlowHandler`, `ScopeMetadataHandler`,
`RequestValidationHandler`. They hold the response-shaping logic so both
controllers and minimal-API routes can reuse them.

Note on `JwksHandler`: the server currently signs with a symmetric key (HS256 by
default), and a JWKS document is public - so the handler publishes only `kid`,
`alg`, `use` for the `oct` key, never the key material. Resource servers
validating HS256 tokens must receive the shared secret out of band.

### Data - `src/Data/Repositories/`

Generic `IRepository<T, TKey>` plus per-aggregate interfaces
(`IUserRepository`, `IClientRepository`, `IRefreshTokenRepository`,
`IAuthorizationGrantRepository`, `IUserSessionRepository`,
`ITotpCredentialRepository`, `IConsentRepository`).

**All current implementations are in-memory** (`ConcurrentDictionary`-backed,
registered as singletons). `Microsoft.Data.Sqlite` is referenced by the host and
a connection string exists in `appsettings.json`, but no SQLite-backed repository
is implemented yet. This is the single most important thing to know before
deploying: state (users, clients, tokens, consents) does not survive a restart.
The interface seam is exactly where a persistent implementation plugs in.

### Domain - `src/Domain/`

- `Entities/` - `User`, `Client`, `AuthorizationGrant`, `RefreshToken`,
  `UserSession`, `Consent`, `Scope`, `TotpCredential`, `WebAuthnCredential`.
- `Models/` - request/response DTOs (`TokenRequest`, `TokenResponse`,
  `AuthorizationRequest`, registration and MFA models).
- `Enums/` - `GrantType`, `TokenType`, `ConsentStatus`.

Entities are mutable POCOs with helper extension methods, not a rich DDD model.
Given the in-memory storage that is fine; revisit if a real ORM arrives.

### Cross-cutting

- `src/Security/` - `RevokedTokenStore` (in-memory jti denylist, entries
  auto-expire with the original token expiry) and `LoginRateLimiter`
  (failed-login lockout, thresholds from `AuthServerOptions`).
- `src/Caching/` - `ICacheService` with a `MemoryCacheService` implementation.
  The interface exists so a Redis implementation can be swapped in without
  touching consumers (JWKS caching, WebAuthn challenges, client lookups).
- `src/Events/` - a minimal in-process pub/sub: `IEventPublisher` /
  `EventPublisher` with typed subscribers over `IDomainEvent`
  (`TokenIssuedEvent`, `UserAuthenticatedEvent`, `ConsentGrantedEvent`).
  Synchronous and in-memory by design - it is an extension point, not a bus.
- `src/Integration/` - `WebhookClient` (typed HttpClient, timeout from
  `WebhookOptions`) and `OpaClient` (Open Policy Agent queries, with a
  fail-open/fail-closed switch in `OpaOptions.FailClosedOnError`).
- `src/BackgroundWorkers/TokenCleanupWorker.cs` - hosted service, hourly sweep
  of expired grants/refresh tokens and expired revocation entries.

## Data flow: authorization code + PKCE

1. `GET /oauth/authorize` -> `AuthorizationController` -> `RequestValidationHandler` /
   `PkceValidationService` validate client, redirect URI, scopes, code challenge.
2. Consent (if `RequireUserConsent`) via `ConsentService`; grant stored through
   `IAuthorizationGrantRepository`; code returned on the redirect.
3. `POST /oauth/token` -> `TokenController` -> `TokenService.HandleTokenRequestAsync`
   -> code grant handler: verifies the code, PKCE verifier, client credentials;
   `JwtTokenFormatter` signs the JWT (HS256/RS256 per config); refresh token is
   persisted; `TokenIssuedEvent` published; response formatted.
4. `POST /oauth/introspect` / `POST /oauth/revoke` go through the respective
   handlers; revocation lands in `RevokedTokenStore`, which introspection consults.
5. `TokenCleanupWorker` prunes what expired.

## Key design decisions

1. **Thin host / fat library.** All behavior lives in `DotnetAuthServer.Core`; the
   host only composes. Cost: the DI wiring in `Program.cs` is long and manual.
   Benefit: the core is testable without a web host and packable as a NuGet.
2. **Interfaces at the storage seam only.** Repositories and `ICacheService` are
   interfaces because implementations will genuinely vary; services are concrete
   because they will not. Avoids interface-per-class ceremony.
3. **Exceptions as control flow for protocol errors.** One middleware knows how to
   translate `AuthServerException` subtypes into OAuth error responses; everything
   else just throws. Trade-off: exceptions on hot validation paths - measured by
   the benchmark project and acceptable at this scale.
4. **In-memory everything (repos, cache, revocation list, rate limiting, events).**
   Fastest possible dev loop and zero infrastructure to try the server out; the
   explicit non-goal is multi-instance deployment. Every one of these has an
   interface or single class where a distributed implementation can replace it.
5. **Options validated at startup, consumed as raw singletons.** Fail-fast on bad
   config beats live reload for an auth server.

## Extension points

- Implement `IRepository<T,TKey>`-derived interfaces against SQLite/Postgres and
  swap the singleton registrations - no service changes needed.
- Implement `ICacheService` against Redis for multi-instance JWKS/challenge caching.
- Subscribe to `IEventPublisher` events for audit sinks or webhooks fan-out.
- Enable OPA (`DotnetAuthServer:Opa:Enabled`) to externalize ABAC decisions.
- `WebhookClient` for outbound notifications, configured under `Webhooks`.

## Known limitations

- **No persistence.** All repositories are in-memory; restart loses everything.
- **Single-instance only.** Rate limiting, revocation list, cache and events are
  all process-local.
- Userinfo (`UserinfoHandler`) and device flow (`DeviceFlowHandler`) exist and are
  registered in DI, but no HTTP route maps to them yet, even though the discovery
  documents advertise `userinfo_endpoint`. JWT bearer authentication middleware
  is not wired up, which is the actual prerequisite for a userinfo endpoint.
- CORS policy is `AllowAll` - fine for development, must be tightened for real use.
- Default signing is symmetric HS256; RS256 is supported by the formatter but no
  key-rotation story exists beyond the JWKS `kid`.
- `WebAuthnService`, `ScopeService` and `ClientService` are implemented in the
  core but not registered in the host DI container - they are library surface,
  usable when composing your own host.
