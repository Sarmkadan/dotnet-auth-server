# dotnet-auth-server

A minimal, production-ready OAuth2/OIDC authorization server for .NET 10.0 with support for PKCE, refresh token rotation, user consent, and role-based/attribute-based access control (RBAC/ABAC).

## Features

- **OAuth2 & OpenID Connect Support**
  - Authorization Code Flow with PKCE
  - Client Credentials Flow
  - Refresh Token Grant
  - Resource Owner Password Credentials Flow
  - Token introspection and revocation

- **Security**
  - Proof Key for Public Clients (PKCE) - mandatory for all clients
  - Secure refresh token rotation with generation tracking
  - Password hashing with failed login lockout
  - JWT token signing with configurable algorithms
  - CORS support

- **User Management**
  - User registration and authentication
  - Email and username validation
  - Account lockout after failed login attempts
  - Role-based access control (RBAC)
  - Attribute-based access control (ABAC)

- **Consent Management**
  - Granular scope-based user consent
  - Consent revocation and history tracking
  - Session-based and persistent consent options

- **Scope Management**
  - OpenID Connect standard scopes (openid, profile, email, phone, address)
  - Custom scope definitions
  - Scope-to-role mappings
  - Claim configuration per scope

- **Client Management**
  - Public and confidential client support
  - Dynamic client secret rotation
  - Redirect URI registration and validation
  - Grant type and scope restrictions per client

## Quick Start

### Prerequisites
- .NET 10.0 SDK or later
- Visual Studio 2025, Visual Studio Code, or compatible IDE

### Installation

```bash
# Clone the repository
git clone https://github.com/sarmkadan/dotnet-auth-server.git
cd dotnet-auth-server

# Restore packages
dotnet restore

# Run the application
dotnet run
```

The server will start at `https://localhost:7001` by default.

### Configuration

Edit `appsettings.json` to configure:

```json
{
  "AuthServer": {
    "IssuerUrl": "https://localhost:7001",
    "JwtSigningKey": "your-secret-key-min-256-bits",
    "AccessTokenLifetimeSeconds": 3600,
    "RefreshTokenLifetimeSeconds": 2592000,
    "RequirePkceForAllClients": true,
    "UseInMemoryDatabase": true
  }
}
```

## Project Structure

```
dotnet-auth-server/
├── Program.cs                      # Application entry point
├── appsettings.json                # Configuration
├── src/
│   ├── Domain/
│   │   ├── Entities/              # User, Client, AuthorizationGrant, etc.
│   │   ├── Models/                # DTOs (TokenRequest, TokenResponse, etc.)
│   │   └── Enums/                 # Grant types, token types, consent status
│   ├── Services/                   # Business logic (Token, Authorization, Consent, User, Client, Scope)
│   ├── Data/Repositories/          # Data access layer (in-memory implementation)
│   ├── Configuration/              # AuthServerOptions, Constants, ServiceRegistry
│   ├── Exceptions/                 # Custom exception types
│   └── Controllers/                # API endpoints (Token, Authorization)
```

## API Endpoints

### OAuth2 Token Endpoint
```
POST /oauth/token
```
Handles token requests with support for all configured grant types.

### OAuth2 Authorization Endpoint
```
GET /oauth/authorize
POST /oauth/authorize (for consent submission)
```
Initiates authorization flow with user consent.

### Token Introspection
```
POST /oauth/token/introspect
```
Validates and returns token information.

### Token Revocation
```
POST /oauth/token/revoke
```
Revokes access or refresh tokens.

### Discovery Endpoints
```
GET /.well-known/oauth-authorization-server
GET /.well-known/openid-configuration
```
OAuth2 and OpenID Connect metadata.

## Usage Examples

### Authorization Code Flow with PKCE

```bash
# 1. Generate PKCE parameters
code_verifier=$(openssl rand -base64 48 | tr -d '\n=+/' | cut -c1-128)
code_challenge=$(echo -n $code_verifier | openssl dgst -sha256 -binary | base64 | tr -d '\n=+/' | cut -c1-43)

# 2. Redirect user to authorization endpoint
https://localhost:7001/oauth/authorize?client_id=myapp&response_type=code&redirect_uri=https://myapp.com/callback&scope=openid+profile+email&code_challenge=$code_challenge&code_challenge_method=S256&state=random123

# 3. User authenticates and grants consent
# Server redirects to: https://myapp.com/callback?code=AUTH_CODE&state=random123

# 4. Exchange code for tokens
curl -X POST https://localhost:7001/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=myapp" \
  -d "redirect_uri=https://myapp.com/callback" \
  -d "code=AUTH_CODE" \
  -d "code_verifier=$code_verifier"
```

### Refresh Token Grant

```bash
curl -X POST https://localhost:7001/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "client_id=myapp" \
  -d "refresh_token=REFRESH_TOKEN"
```

### Client Credentials Flow

```bash
curl -X POST https://localhost:7001/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=client_credentials" \
  -d "client_id=machine_app" \
  -d "client_secret=SECRET" \
  -d "scope=api:read"
```

## Token Structure

Access tokens are JWT tokens containing:
- `sub`: Subject (user or client ID)
- `iss`: Issuer (authorization server URL)
- `aud`: Audience (client ID)
- `scope`: Granted scopes
- `exp`: Expiration time
- `iat`: Issued at time
- `roles`: User roles (RBAC)

Example decoded token:
```json
{
  "sub": "user123",
  "iss": "https://localhost:7001",
  "aud": "myapp",
  "scope": "openid profile email",
  "roles": ["user", "admin"],
  "email": "user@example.com",
  "email_verified": true,
  "exp": 1704067200,
  "iat": 1704063600
}
```

## Security Considerations

- **PKCE**: Mandatory for all clients to prevent authorization code interception attacks
- **Refresh Token Rotation**: New refresh token issued on each use; old tokens are tracked for replay detection
- **Token Storage**: Tokens are hashed in the database; never store plain tokens
- **HTTPS**: Always use HTTPS in production (localhost allowed for development)
- **Secret Management**: Store JWT signing keys in secure vaults (Azure Key Vault, HashiCorp Vault, etc.)

## Database

Currently uses in-memory storage for Phase 1. For production, implement:
- Microsoft SQL Server
- PostgreSQL
- SQLite with EF Core

Repository interfaces are defined for easy database layer implementation.

## Testing

Run the included Swagger UI at `https://localhost:7001/swagger` to test endpoints interactively.

## Contributing

This is an open-source educational project. Contributions welcome!

## License

MIT License - Copyright © 2026 Vladyslav Zaiets

See LICENSE file for details.

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- GitHub: https://github.com/sarmkadan
- Email: rutova2@gmail.com

---

**Note**: This is Phase 1 (Core Architecture). Future phases will include advanced features, persistence layer, comprehensive tests, and production hardening.
