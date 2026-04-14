# Architecture & Design

Comprehensive overview of dotnet-auth-server's design, patterns, and implementation.

## Table of Contents

1. [System Architecture](#system-architecture)
2. [Layered Design](#layered-design)
3. [Request Flow](#request-flow)
4. [Security Architecture](#security-architecture)
5. [Data Model](#data-model)
6. [Extension Points](#extension-points)
7. [Design Patterns](#design-patterns)

---

## System Architecture

```
┌──────────────────────────────────────────────────────────────┐
│                    HTTP/HTTPS Clients                        │
│                (Browsers, Mobile, Backends)                  │
└────────────────┬─────────────────────────────────────────────┘
                 │
         ┌───────▼────────────────────────┐
         │   API Gateway / Middleware     │
         │ • CORS Handling                │
         │ • Rate Limiting                │
         │ • Request Logging              │
         │ • Error Handling               │
         └───────┬────────────────────────┘
                 │
    ┌────────────┼────────────┐
    │            │            │
    ▼            ▼            ▼
┌─────────┐ ┌────────────┐ ┌─────────┐
│ OAuth   │ │ OIDC       │ │ Admin   │
│ Endpoints  │ Endpoints  │ Endpoints│
└────┬────┘ └──────┬─────┘ └────┬────┘
     │             │            │
     └─────┬───────┴────────┬───┘
           │                │
      ┌────▼────────────────▼────┐
      │  Service Layer           │
      │ • AuthService            │
      │ • TokenService           │
      │ • ConsentService         │
      │ • UserService            │
      │ • ClientService          │
      │ • ScopeService           │
      │ • PolicyEnforcementSvc   │
      └────┬────────────────┬────┘
           │                │
      ┌────▼────────────────▼────┐
      │  Repository Pattern      │
      │ • UserRepository         │
      │ • ClientRepository       │
      │ • TokenRepository        │
      │ • ConsentRepository      │
      │ • AuditLogRepository     │
      └────┬────────────────┬────┘
           │                │
      ┌────▼────────────────▼────┐
      │  Data Access Layer       │
      │ • In-Memory (Dev)        │
      │ • SQL Server (Prod)      │
      │ • PostgreSQL (Prod)      │
      │ • SQLite (Testing)       │
      └──────────────────────────┘
```

---

## Layered Design

### Presentation Layer (Controllers)

**Location**: `src/Controllers/`

Responsible for:
- HTTP request parsing
- Route handling
- Response formatting
- Parameter validation (basic)

**Controllers:**

1. **AuthorizationController** (`/oauth/authorize`, `/oauth/userinfo`)
   - Handles user authentication flow
   - Renders consent forms
   - Manages session state

2. **TokenController** (`/oauth/token`, `/oauth/token/introspect`, `/oauth/token/revoke`)
   - Token issuance and refresh
   - Token validation
   - Token revocation

### Service Layer (Business Logic)

**Location**: `src/Services/`

Core business logic and orchestration:

```csharp
// Example service structure
public class TokenService
{
    // Generates access tokens with claims enrichment
    public TokenResponse GenerateTokenAsync(AuthorizationRequest req)
    {
        // Validate client & scopes
        // Enrich claims (ABAC)
        // Create JWT payload
        // Sign and issue token
        // Generate refresh token
        // Persist refresh token
        // Return response
    }
}
```

**Key Services:**

| Service | Purpose | Key Methods |
|---------|---------|------------|
| **TokenService** | Token generation & refresh | `GenerateTokenAsync`, `RefreshTokenAsync` |
| **AuthorizationService** | Authorization flow control | `InitiateFlowAsync`, `ValidateConsent` |
| **ConsentService** | Consent management | `GetConsentAsync`, `GrantConsentAsync`, `RevokeConsent` |
| **UserService** | User lifecycle | `RegisterUserAsync`, `AuthenticateAsync` |
| **ClientService** | Client management | `ValidateClientAsync`, `GetClientAsync` |
| **ScopeService** | Scope handling | `GetScopesAsync`, `ValidateScopesAsync` |
| **PkceValidationService** | PKCE verification | `ValidateChallengeAsync` |
| **PolicyEnforcementService** | RBAC/ABAC | `EnforceAsync`, `GetPoliciesAsync` |
| **AuditLoggingService** | Audit trail | `LogAsync`, `GetAuditLogsAsync` |

### Data Access Layer (Repositories)

**Location**: `src/Data/Repositories/`

Pattern: **Generic Repository** with **Specification** support

```csharp
// Generic repository interface
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(string id);
}

// Specific repository example
public class UserRepository : IRepository<User>
{
    // Find by username
    public async Task<User?> FindByUsernameAsync(string username)
    {
        return _users.FirstOrDefault(u => u.Username == username);
    }

    // Find with roles
    public async Task<IEnumerable<User>> GetUsersWithRoleAsync(string role)
    {
        return _users.Where(u => u.Roles.Contains(role));
    }
}
```

**Repository Classes:**

1. **UserRepository** - User accounts and authentication
2. **ClientRepository** - OAuth2 client registrations
3. **TokenRepository** - Refresh tokens and grants
4. **ConsentRepository** - User consent records
5. **AuthorizationGrantRepository** - Authorization codes

### Domain Layer (Models & Entities)

**Location**: `src/Domain/`

**Entities** (persistent objects):
- `User` - User accounts with roles
- `Client` - OAuth2 clients (public/confidential)
- `Scope` - OAuth2 scopes with claims
- `AuthorizationGrant` - Temporary authorization codes
- `RefreshToken` - Refresh token records
- `Consent` - User consent grants

**Models** (DTOs):
- `TokenRequest` / `TokenResponse` - Token endpoint
- `AuthorizationRequest` / `AuthorizationResponse` - Auth endpoint
- `ConsentRequest` / `ConsentResponse` - Consent flow
- `ApiResponse<T>` - Standard response wrapper

**Enums**:
- `GrantType` - `authorization_code`, `refresh_token`, `client_credentials`
- `TokenType` - `access_token`, `refresh_token`, `id_token`
- `ConsentStatus` - `Pending`, `Granted`, `Revoked`

### Infrastructure Layer

**Location**: `src/`

**Middleware:**
- `ErrorHandlingMiddleware` - Global exception handling
- `LoggingMiddleware` - Request/response logging
- `RateLimitingMiddleware` - DDoS protection
- `RequestContextMiddleware` - Request ID correlation

**Services:**
- `AuditLoggingService` - Audit trails
- `CacheService` - In-memory caching
- `SecretsService` - Secret management
- `SessionStateService` - Session management

**Formatters:**
- `JwtTokenFormatter` - JWT creation and signing
- `JsonTokenResponseFormatter` - Response formatting

---

## Request Flow

### Authorization Code Flow + PKCE (Most Common)

```
┌─────────┐
│ Client  │
└────┬────┘
     │
     │ 1. User clicks "Sign in with Auth Server"
     │    Generates code_verifier & code_challenge
     │
     ▼
┌──────────────────────────────────────────────┐
│ /oauth/authorize                             │
│ ?client_id=...&code_challenge=...            │
└────┬─────────────────────────────────────────┘
     │
     │ 2. AuthorizationController validates client
     │    Generates state parameter
     │
     ▼
┌──────────────────────────────────────────────┐
│ Login Page (if user not authenticated)       │
│ Form: username + password                    │
└────┬─────────────────────────────────────────┘
     │
     │ 3. UserService.AuthenticateAsync()
     │    • Hash password with salt
     │    • Compare with stored hash
     │    • Check account lockout
     │    • Update last login
     │
     ▼
┌──────────────────────────────────────────────┐
│ Consent Form                                 │
│ Checkboxes for each scope (openid, profile) │
└────┬─────────────────────────────────────────┘
     │
     │ 4. ConsentService.GrantConsentAsync()
     │    • Validate scopes
     │    • Store consent record
     │    • Emit ConsentGrantedEvent
     │
     ▼
┌──────────────────────────────────────────────┐
│ Authorization Code Issued                    │
│ Redirect to: https://client.com/callback     │
│   ?code=AUTH_XYZ123&state=random             │
└────┬─────────────────────────────────────────┘
     │
     │ 5. Client backend receives code
     │    Has code_verifier from step 1
     │
     ▼
┌──────────────────────────────────────────────┐
│ POST /oauth/token                            │
│ grant_type=authorization_code                │
│ code=AUTH_XYZ123                             │
│ code_verifier=...                            │
└────┬─────────────────────────────────────────┘
     │
     │ 6. TokenService.GenerateTokenAsync()
     │    • PkceValidationService.ValidateAsync()
     │      - SHA256(verifier) == challenge?
     │    • ClaimsEnrichmentService.EnrichAsync()
     │      - Add user roles (RBAC)
     │      - Add custom attributes (ABAC)
     │    • JwtTokenFormatter.CreateJwt()
     │      - Sign with HS256/RS256
     │    • Create refresh token
     │    • TokenRepository.SaveRefreshTokenAsync()
     │    • AuditLoggingService.LogAsync()
     │
     ▼
┌──────────────────────────────────────────────┐
│ Token Response                               │
│ {                                            │
│   "access_token": "jwt_xyz...",              │
│   "token_type": "Bearer",                    │
│   "expires_in": 3600,                        │
│   "refresh_token": "refresh_abc...",         │
│   "id_token": "jwt_with_claims..."           │
│ }                                            │
└──────────────────────────────────────────────┘
```

### Token Refresh Flow

```
┌─────────┐
│ Client  │ (has expired access token)
└────┬────┘
     │
     │ 1. POST /oauth/token
     │    grant_type=refresh_token
     │    refresh_token=refresh_abc...
     │
     ▼
┌────────────────────────────────────┐
│ TokenService.RefreshTokenAsync()   │
│ 1. Validate refresh_token exists   │
│ 2. Check not revoked               │
│ 3. Check not reused (rotation)     │
│ 4. Generate NEW access_token       │
│ 5. Generate NEW refresh_token      │
│ 6. Mark old refresh_token as used  │
│ 7. Return new tokens               │
└────┬───────────────────────────────┘
     │
     ▼
┌──────────────────────────────────┐
│ Token Response                   │
│ (new access + refresh token pair)│
│ Old refresh_token: INVALIDATED   │
└──────────────────────────────────┘
```

### Token Introspection Flow

```
┌──────────────────┐
│ Resource Server  │ (wants to validate token)
└────┬─────────────┘
     │
     │ 1. POST /oauth/token/introspect
     │    token=eyJhbGc...
     │
     ▼
┌─────────────────────────────────┐
│ JwtTokenFormatter.ValidateAsync()│
│ 1. Verify signature              │
│ 2. Check expiration              │
│ 3. Extract claims                │
│ 4. Cache result (TTL)            │
└────┬────────────────────────────┘
     │
     ▼
┌──────────────────────────────────┐
│ Response                         │
│ {                                │
│   "active": true,                │
│   "sub": "user123",              │
│   "scope": "openid profile",     │
│   "exp": 1704070800,             │
│   "roles": ["user"],             │
│   ...                            │
│ }                                │
└──────────────────────────────────┘
```

---

## Security Architecture

### Key Management

```
┌─────────────────────────────────────┐
│ JWT Signing Key                     │
│ • 256+ bits (minimum)               │
│ • HS256: Shared secret              │
│ • RS256: Private key (keep secure)  │
│ • Rotation: Manual + event logging  │
│ • Storage: Vault (prod), env (dev)  │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Client Secrets                      │
│ • Hashed in database                │
│ • PBKDF2-SHA256 algorithm           │
│ • Rotation tracked in audit log     │
│ • Compared constant-time            │
└─────────────────────────────────────┘

┌─────────────────────────────────────┐
│ Refresh Tokens                      │
│ • Hashed in database                │
│ • Rotation on use                   │
│ • One-time use (revoke on replay)   │
│ • Generation chain tracked          │
└─────────────────────────────────────┘
```

### Token Validation Pipeline

```
1. Header Validation
   • Algorithm = HS256/RS256 ✓
   • Type = JWT ✓

2. Signature Verification
   • Signature = HMAC(payload, secret) ✓
   OR
   • Signature = RSA(payload, private_key) ✓

3. Claims Validation
   • exp > now() ✓
   • iss = expected issuer ✓
   • aud = expected audience ✓
   • nbf <= now() ✓

4. Custom Validation
   • Token in blacklist? ✗
   • Revoked? ✗
   • Rate limit check ✓

5. Claim Extraction
   • Extract sub, scope, roles, custom claims
   • Return TokenInfo
```

### PKCE Implementation

```
Client Side:
1. Generate code_verifier (random 43-128 bytes)
   base64url encode, remove padding

Server Side (Authorization Endpoint):
2. Receive code_challenge
   Store with authorization code
   (Don't verify yet - need verifier later)

Client Side (Token Request):
3. Send code_verifier in token request

Server Side (Token Endpoint):
4. Receive code_verifier
   Calculate: challenge = BASE64URL(SHA256(verifier))
   Compare: challenge == stored_challenge
   
   If match: ✓ Valid PKCE
   If mismatch: ✗ Reject request
```

### Refresh Token Rotation

```
Generation Chain:
┌──────────────┐
│ Refresh v1   │ (initial token)
└──────┬───────┘
       │ Use to refresh
       ▼
    ┌──────────────┐
    │ Refresh v2   │ (new token issued)
    │ v1 → REVOKED │ (previous invalidated)
    └──────┬───────┘
           │ Use to refresh
           ▼
        ┌──────────────┐
        │ Refresh v3   │ (new token issued)
        │ v2 → REVOKED │ (previous invalidated)
        └──────┬───────┘
               │ Use v1 (compromised?)
               ▼
            ✗ REJECTED
            Log security event
```

---

## Data Model

### Core Entities

```csharp
// User Account
public class User
{
    public string UserId { get; set; }           // Unique identifier
    public string Username { get; set; }         // Login username
    public string Email { get; set; }            // Email address
    public string PasswordHash { get; set; }     // PBKDF2-SHA256 + salt
    public DateTime CreatedAt { get; set; }
    public DateTime LastLoginAt { get; set; }
    public bool IsActive { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? AccountLockedUntil { get; set; }
    public List<string> Roles { get; set; }     // ["user", "admin"]
    public Dictionary<string, string> Claims { get; set; } // Custom attributes
}

// OAuth2 Client
public class Client
{
    public string ClientId { get; set; }        // Identifier
    public string ClientSecret { get; set; }    // Hashed secret
    public string ClientName { get; set; }
    public ClientType ClientType { get; set; }  // Public/Confidential
    public bool RequirePkce { get; set; }
    public List<string> RedirectUris { get; set; }
    public List<GrantType> AllowedGrantTypes { get; set; }
    public List<string> AllowedScopes { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

// OAuth2 Scope
public class Scope
{
    public string ScopeName { get; set; }
    public string Description { get; set; }
    public bool IsRequired { get; set; }
    public bool RequiresConsent { get; set; }
    public List<string> ClaimsToInclude { get; set; }
    public List<string> RequiredRoles { get; set; } // RBAC
}

// Authorization Code (temporary)
public class AuthorizationGrant
{
    public string Code { get; set; }            // Short-lived code
    public string ClientId { get; set; }
    public string UserId { get; set; }
    public List<string> RequestedScopes { get; set; }
    public string CodeChallenge { get; set; }   // For PKCE
    public string RedirectUri { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }            // One-time use
}

// Refresh Token
public class RefreshToken
{
    public string Token { get; set; }
    public string ClientId { get; set; }
    public string UserId { get; set; }
    public List<string> GrantedScopes { get; set; }
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public int RotationCount { get; set; }      // Track refresh chain
}

// User Consent
public class Consent
{
    public string ConsentId { get; set; }
    public string UserId { get; set; }
    public string ClientId { get; set; }
    public List<string> GrantedScopes { get; set; }
    public ConsentStatus Status { get; set; }   // Pending/Granted/Revoked
    public DateTime GrantedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsPersistent { get; set; }      // Persistent vs session
}
```

---

## Extension Points

### Custom Claim Enrichment

```csharp
public interface IClaimsEnrichmentService
{
    Task<IEnumerable<Claim>> EnrichClaimsAsync(
        User user,
        Client client,
        IEnumerable<string> requestedScopes);
}

// Example: Add department attribute
public class CustomClaimsEnrichment : IClaimsEnrichmentService
{
    public async Task<IEnumerable<Claim>> EnrichClaimsAsync(
        User user, Client client, IEnumerable<string> requestedScopes)
    {
        var claims = new List<Claim>();
        
        if (requestedScopes.Contains("profile"))
            claims.Add(new("department", user.Claims["department"]));
        
        if (user.Roles.Contains("admin"))
            claims.Add(new("admin_level", "full"));
        
        return claims;
    }
}
```

### Custom Policy Enforcement

```csharp
public interface IPolicyEnforcementService
{
    Task<bool> EnforceAsync(User user, string policy, object context);
}

// Example: Time-based access
public class TimePolicyEnforcer : IPolicyEnforcementService
{
    public async Task<bool> EnforceAsync(User user, string policy, object context)
    {
        if (policy == "business_hours_only")
        {
            var now = DateTime.UtcNow;
            return now.Hour >= 9 && now.Hour < 17;
        }
        return true;
    }
}
```

### Custom Token Formatters

```csharp
public interface ITokenFormatter
{
    Task<string> FormatAsync<T>(T data);
    Task<T> ParseAsync<T>(string token);
}

// Example: Add custom header
public class CustomTokenFormatter : ITokenFormatter
{
    public async Task<string> FormatAsync<T>(T data)
    {
        var jwt = await _baseFormatter.FormatAsync(data);
        // Add custom header or claims
        return jwt;
    }
}
```

---

## Design Patterns

### Pattern 1: Repository Pattern

**Goal**: Abstract data access

```csharp
// Interface
public interface IUserRepository : IRepository<User>
{
    Task<User?> FindByUsernameAsync(string username);
}

// Implementation
public class UserRepository : IUserRepository
{
    private List<User> _users = new();

    public async Task<User?> FindByUsernameAsync(string username)
    {
        return await Task.FromResult(
            _users.FirstOrDefault(u => u.Username == username)
        );
    }
}

// Usage in Service
public class UserService
{
    public async Task<User?> AuthenticateAsync(string username, string password)
    {
        var user = await _userRepository.FindByUsernameAsync(username);
        // Validate password...
        return user;
    }
}
```

### Pattern 2: Dependency Injection

```csharp
// Register services
services.AddScoped<ITokenService, TokenService>();
services.AddScoped<IUserService, UserService>();
services.AddScoped<IRepository<User>, UserRepository>();

// Inject into controller
public class TokenController
{
    private readonly ITokenService _tokenService;

    public TokenController(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }
}
```

### Pattern 3: Options Pattern

```csharp
// Configuration
services.Configure<AuthServerOptions>(
    configuration.GetSection("AuthServer"));

// Usage
public class TokenService
{
    private readonly AuthServerOptions _options;

    public TokenService(IOptions<AuthServerOptions> options)
    {
        _options = options.Value;
    }

    public int AccessTokenLifetime => _options.AccessTokenLifetimeSeconds;
}
```

### Pattern 4: Domain Events

```csharp
// Event definition
public record TokenIssuedEvent(string UserId, string ClientId) : IDomainEvent;

// Publisher
public interface IEventPublisher
{
    Task PublishAsync(IDomainEvent @event);
}

// Handler
public class TokenIssuedEventHandler
{
    public async Task HandleAsync(TokenIssuedEvent @event)
    {
        // Log, notify, trigger other workflows
    }
}
```

---

For more details, see individual source files with implementation comments.
