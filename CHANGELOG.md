# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

---

## [1.0.0] - 2025-11-10

### Added
- Comprehensive documentation suite: getting-started.md, architecture.md, deployment.md, faq.md
- Six example projects covering all major OAuth2 flows (HTML SPA, C# M2M, token refresh, ABAC, curl scripts, resource server integration)
- Docker and Docker Compose support with full stack (PostgreSQL, Redis, Adminer, Grafana)
- CI/CD pipeline: GitHub Actions workflows for build, CodeQL analysis, and NuGet publish
- Makefile with 30+ development commands for local and CI use
- .editorconfig for consistent cross-editor formatting
- Prometheus metrics integration and health check endpoints
- CHANGELOG, CONTRIBUTING.md, CODE_OF_CONDUCT.md, SECURITY.md community files
- NuGet packaging configuration with README embed and repository metadata

### Changed
- README expanded to full reference documentation with API tables and runnable examples
- Improved error messages across all endpoints for better developer experience
- Refined API response formatting to match RFC specifications exactly

### Fixed
- Token expiration edge case in refresh grant when clock skew exceeds configured tolerance
- PKCE validation error messages now return the correct `invalid_request` error code
- Rate limiting counter reset alignment under high concurrency

---

## [0.9.0] - 2025-09-22

### Added
- WebAuthn credential entity and `WebAuthnService` for passkey registration and assertion
- `TokenCleanupWorker` background service to periodically purge expired tokens and grants
- `SessionStateService` for server-side session tracking across requests
- `SecretsService` for client secret hashing and rotation
- `WebhookClient` and `HttpClientFactory` for outbound event delivery

### Changed
- `ClaimsEnrichmentService` now supports async claim providers for external attribute lookup
- `AuditLoggingService` writes structured log entries consumable by Seq and Elasticsearch

### Fixed
- `RefreshTokenRepository` now correctly handles concurrent revocation without race conditions
- Background worker shutdown respects `CancellationToken` and drains in-flight operations cleanly

---

## [0.8.0] - 2025-08-04

### Added
- Token Introspection endpoint (RFC 7662) — `POST /oauth/token/introspect`
- Token Revocation endpoint (RFC 7009) — `POST /oauth/token/revoke`
- Device Authorization Flow (RFC 8628) — `DeviceFlowHandler` with polling and expiry
- `TokenIntrospectionHandler`, `TokenRevocationHandler`, and `ScopeMetadataHandler`
- `UserinfoHandler` for OIDC UserInfo endpoint — `POST /oauth/userinfo`
- `JwksHandler` for JWKS endpoint — `GET /.well-known/jwks.json`
- OpenID Connect Discovery endpoint — `GET /.well-known/openid-configuration`
- `RequestValidationHandler` middleware for centralized request shape enforcement

### Changed
- `TokenController` refactored to delegate to handler classes for each grant type
- Scope metadata now queryable via `GET /oauth/scopes` for dynamic client registration

### Fixed
- Device flow polling endpoint now correctly returns `authorization_pending` before approval
- UserInfo endpoint respects scope-to-claim mapping for partial profile requests

---

## [0.7.0] - 2025-06-23

### Added
- `AuditLoggingService` — structured audit trail for all sensitive operations
- `RateLimitingMiddleware` — per-IP per-endpoint throttling (configurable limits)
- `ErrorHandlingMiddleware` — global exception-to-RFC-error-response mapping
- `LoggingMiddleware` and `RequestContextMiddleware` — correlation IDs on every request
- Account lockout after N consecutive failed login attempts (configurable threshold and window)
- `LoggingOptions` and `CacheOptions` configuration sections with validation on startup

### Changed
- All sensitive operations (login, token issue, revocation, consent) now emit audit events
- Rate limit headers (`Retry-After`, `X-RateLimit-Remaining`) added to 429 responses

### Fixed
- `ErrorHandlingMiddleware` no longer swallows `OperationCanceledException` from aborted requests
- Audit log timestamps normalised to UTC across all services

---

## [0.6.0] - 2025-05-12

### Added
- `PolicyEnforcementService` for RBAC and ABAC policy evaluation
- `ClaimsEnrichmentService` — dynamic claim injection from user attributes and scope mappings
- `ScopeValidationService` and `ScopeService` for hierarchical scope management (e.g. `api:read:users`)
- `ClientValidationService` for redirect URI and grant type whitelist enforcement
- Domain events: `TokenIssuedEvent`, `UserAuthenticatedEvent`, `ConsentGrantedEvent`
- `IEventPublisher` / `EventPublisher` for in-process domain event dispatch

### Changed
- Access tokens now include `roles` and custom attribute claims when ABAC policy matches
- Scope metadata carries `RequiredRoles` list used during scope-grant evaluation

### Fixed
- Custom claims were silently dropped when scope list exceeded 10 entries
- ABAC attribute lookup no longer throws when a user has no department assigned

---

## [0.5.0] - 2025-04-14

### Added
- User registration, authentication, and profile management (`UserService`, `UserRepository`)
- Consent flow: granular per-scope user consent with session and persistent modes (`ConsentService`)
- `IConsentRepository` / `ConsentRepository` with consent history tracking
- `ConsentRequest` model and `ConsentStatus` enum (Pending / Granted / Revoked)
- `AuthorizationGrantRepository` for authorization code lifecycle management
- `Scope` and `WebAuthnCredential` domain entities

### Changed
- `/oauth/authorize` now redirects to consent screen when user has not previously granted scope
- `AuthorizationController` validates redirect URI against client registration before any redirect

### Fixed
- Authorization code was not invalidated after single use when token exchange failed mid-flight
- Consent revocation now propagates to active refresh tokens on next rotation

---

## [0.4.0] - 2025-03-17

### Added
- Refresh token rotation: every refresh issues a new token and revokes the old one
- `RefreshToken` entity with `IsRevoked`, `ReplacedByTokenId`, and generation tracking
- `RefreshTokenRepository` for CRUD and revocation queries
- Replay attack detection: reusing a revoked refresh token triggers full token family revocation
- `TokenType` and `GrantType` enumerations for type-safe grant handling

### Changed
- `TokenService` now returns a full `TokenResponse` with both access and refresh tokens
- Configurable refresh token TTL via `AuthServer__RefreshTokenLifetimeSeconds`

### Fixed
- Refresh tokens were not correctly scoped to the originating client, allowing cross-client reuse

---

## [0.3.0] - 2025-02-24

### Added
- PKCE support (RFC 7636) — S256 `code_challenge` / `code_verifier` validation mandatory for all clients
- `PkceValidationService` with constant-time comparison and BASE64URL decoding
- `code_challenge_method` enforcement: plain method rejected, S256 required
- `AuthServerOptions` configuration class with validation attributes

### Changed
- `/oauth/authorize` now requires `code_challenge` and `code_challenge_method=S256` on every request
- Authorization code stores `code_challenge` and is validated at token exchange time

### Fixed
- BASE64URL padding edge case that caused verification failures for specific verifier lengths

---

## [0.2.0] - 2025-02-03

### Added
- `POST /oauth/token` endpoint supporting `authorization_code` and `client_credentials` grants
- `AuthorizationController` with `GET /oauth/authorize` endpoint
- JWT access token issuance via `JwtTokenFormatter` (HS256)
- `Client`, `User`, `AuthorizationGrant` domain entities
- `ClientRepository` and in-memory `IRepository<T>` pattern
- `ClientService` and `TokenService` service layer
- `TokenRequest`, `TokenResponse`, `AuthorizationRequest` request/response models
- `InvalidClientException`, `InvalidGrantException`, `UnauthorizedClientException` typed exceptions
- Swagger/OpenAPI documentation at `/swagger`
- `appsettings.json` with `AuthServer` configuration section

### Changed
- Project namespace unified to `DotnetAuthServer` across all files

### Fixed
- Token endpoint returned 500 instead of 400 for malformed `grant_type` values

---

## [0.1.0] - 2025-01-20

### Added
- Initial project scaffold: `dotnet new webapi` with .NET 10 target
- Solution file (`dotnet-auth-server.sln`) and project structure (`src/`, `tests/`)
- `AuthServerException` base exception and `ApiResponse<T>` model
- `ICacheService` / `MemoryCacheService` for in-process token caching
- `Constants` class with OAuth2 endpoint paths and claim type names
- Extension methods: `ClaimsPrincipalExtensions`, `StringExtensions`, `DateTimeExtensions`
- `JsonTokenResponseFormatter` for RFC-compliant token response serialization
- xUnit test project with FluentAssertions and Moq references
- MIT License and initial README

---

## Version Numbering

- **0.1.x** - Project scaffold and core infrastructure
- **0.2.x** - OAuth2 token endpoints and JWT issuance
- **0.3.x** - PKCE (RFC 7636) enforcement
- **0.4.x** - Refresh token rotation and replay protection
- **0.5.x** - User management and consent flow
- **0.6.x** - RBAC/ABAC policy enforcement and domain events
- **0.7.x** - Audit logging, rate limiting, and observability
- **0.8.x** - Token introspection, revocation, device flow, OIDC discovery
- **0.9.x** - WebAuthn, background workers, secrets management
- **1.0.x** - Stable release: full documentation, Docker, CI/CD

---

## Contributing

Contributions are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## Author

**Vladyslav Zaiets** - CTO & Software Architect

- Website: https://sarmkadan.com
- GitHub: https://github.com/Sarmkadan

---

## License

MIT License - Copyright © 2025 Vladyslav Zaiets

See [LICENSE](LICENSE) for details.
