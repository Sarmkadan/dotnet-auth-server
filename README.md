# dotnet-auth-server

A lightweight, extensible OAuth 2.0 / OpenID Connect authorization server for .NET
(net10.0). Authorization code + PKCE, client credentials, refresh token and password
grants, token introspection/revocation, dynamic client registration, consent,
TOTP MFA, session management - with an in-memory storage layer designed to be
swapped for a real database.

## Quick start

```bash
dotnet run --project dotnet-auth-server.csproj
```

Swagger UI is available at `/swagger` in development. Discovery documents live at
`/.well-known/oauth-authorization-server` and `/.well-known/openid-configuration`.

Configuration is bound from the `DotnetAuthServer` section - see
`appsettings.example.json` for the full shape. Set `JwtSigningKey` from the
environment in anything but local development.

## Endpoints

| Route | Purpose |
|---|---|
| `GET /oauth/authorize` | Authorization code flow (PKCE enforced by default) |
| `POST /oauth/token` | Token endpoint (authorization_code, refresh_token, client_credentials, password) |
| `POST /oauth/introspect` | RFC 7662 token introspection |
| `POST /oauth/revoke` | RFC 7009 token revocation |
| `POST /register` | RFC 7591 dynamic client registration |
| `GET /.well-known/jwks.json` | Signing key set |
| `api/users`, `api/sessions`, `api/users/{id}/mfa` | Management APIs |
| `GET /health` | Health check |

## Architecture

The solution is a thin ASP.NET Core host (`Program.cs`) over a self-contained
library (`src/`, `DotnetAuthServer.Core`): controllers delegate to services,
services talk to repository interfaces, and everything protocol-shaped
(introspection, revocation, JWKS, userinfo) lives in dedicated handlers.
Storage is currently in-memory - the repository interfaces are the seam for a
persistent implementation.

Full write-up with rationale, data flow and known limitations:
[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md).

## Tests and benchmarks

```bash
dotnet test tests/dotnet-auth-server.Tests
dotnet run -c Release --project dotnet-auth-server.Benchmarks
```

## WebAuthnCredential

The `WebAuthnCredential` entity represents a WebAuthn/FIDO2 public-key credential
associated with a user account, enabling passwordless and phishing-resistant
authentication via platform authenticators.

```csharp
using DotnetAuthServer.Domain.Entities;

var credential = new WebAuthnCredential
{
    Id = Guid.NewGuid().ToString(),
    CredentialId = "cred-123",
    PublicKey = new byte[] { 1, 2, 3 },
    Algorithm = -7, // ES256
    UserId = "user-123",
    FriendlyName = "Security Key",
    AaGuid = "00000000-0000-0000-0000-000000000000",
    BackupEligible = true,
    BackedUp = false,
    IsActive = true
};
```

## License

MIT - see [LICENSE](LICENSE).
