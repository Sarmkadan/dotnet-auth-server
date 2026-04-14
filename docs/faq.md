# Frequently Asked Questions (FAQ)

## General

### What is dotnet-auth-server?

A lightweight, standards-compliant OAuth2/OpenID Connect authorization server built with .NET 10. It's designed for developers who need a secure, production-ready auth solution without the complexity of commercial products like Auth0 or enterprise solutions like Keycloak.

### Who should use it?

- SaaS platforms with multi-tenant authentication needs
- Microservice architectures needing centralized auth
- Teams wanting OAuth2/OIDC compliance without vendor lock-in
- Security-focused projects requiring code transparency
- Development teams building OAuth2 integrations

### Is it production-ready?

Yes. The codebase includes security hardening, audit logging, rate limiting, and comprehensive error handling. Start with Phase 2 features for MVP, then add persistence layer (Phase 4) for production.

### What's the license?

MIT License - free for personal and commercial use. Attribution required but no warranty.

---

## Technical

### Which .NET versions are supported?

**.NET 10.0 only**. We use latest C# language features and don't maintain backward compatibility with older versions.

### Can I use with .NET 6/7/8?

Technically possible but unsupported. You'd need to:
1. Update `dotnet-auth-server.csproj` TargetFramework
2. Verify dependency compatibility
3. Test thoroughly

We don't backport changes to older versions.

### Does it support OAuth2 Device Flow?

Yes! Device Flow (RFC 8628) is implemented in `src/Handlers/DeviceFlowHandler.cs` for IoT and headless applications.

### Is OIDC fully supported?

Yes. We implement OIDC 1.0 Core including:
- ID tokens with standard claims
- UserInfo endpoint
- Discovery metadata (`.well-known/openid-configuration`)
- JWKS endpoint for key distribution

### What about SAML support?

Not currently. SAML is complex and we focus on OAuth2/OIDC. If SAML is critical, consider Apache Shibboleth or Keycloak.

---

## Security

### Is PKCE mandatory?

Yes, for all clients. This prevents authorization code interception attacks, especially important for public clients (SPAs, native apps).

### What if I don't want PKCE?

You can disable it in `appsettings.json`:
```json
{
  "AuthServer": {
    "RequirePkceForAllClients": false
  }
}
```

**⚠️ Not recommended for production**. PKCE is a best practice.

### How are passwords stored?

PBKDF2-SHA256 with 100,000 iterations per NIST 800-132. Each user gets a unique 32-byte salt.

### What about password complexity requirements?

Currently no built-in enforcement. Add in `UserService`:
```csharp
private bool ValidatePasswordStrength(string password)
{
    // Min 12 chars, upper, lower, number, symbol
    var hasLength = password.Length >= 12;
    var hasUpper = Regex.IsMatch(password, @"[A-Z]");
    var hasLower = Regex.IsMatch(password, @"[a-z]");
    var hasDigit = Regex.IsMatch(password, @"\d");
    
    return hasLength && hasUpper && hasLower && hasDigit;
}
```

### Can I rotate JWT signing keys?

Not yet. Key rotation is on the roadmap. For now:
1. Update `JwtSigningKey` in config
2. Restart server
3. Old tokens become invalid (expect client logout)

### What about token encryption?

Not implemented. Tokens are signed (integrity verified) but not encrypted. If you need encryption:
1. Use HTTPS for transport (always do this)
2. Implement JWE in `JwtTokenFormatter`

### Is there rate limiting?

Yes! Configurable per endpoint:
```json
{
  "AuthServer": {
    "EnableRateLimiting": true,
    "RateLimitPerMinute": 30
  }
}
```

Default: 10 requests/min on token endpoint, 20 requests/5min on auth endpoint.

---

## Deployment

### Can I run it in Docker?

Yes! Dockerfile included. Multi-stage build optimizes image size.

### How do I scale it horizontally?

1. Run multiple instances behind a load balancer
2. Use distributed cache (Redis) instead of in-memory
3. Use persistent database (SQL Server, PostgreSQL)
4. Configure sticky sessions for consent flow

### What database should I use?

For **production**:
- **Microsoft SQL Server** - enterprise support, good scalability
- **PostgreSQL** - open source, excellent performance, ACID compliance

For **development**: In-memory storage or SQLite.

### How do I handle database migrations?

Currently not included. You'll need:
```bash
# Using EF Core
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Can I run it in Kubernetes?

Yes. Manifest in `docs/deployment.md`. Includes:
- Deployment with 3 replicas
- Horizontal Pod Autoscaler
- Service + LoadBalancer
- ConfigMaps and Secrets

### What about high availability?

To make it HA:
1. Run multiple instances (3+ recommended)
2. Use distributed cache (Redis, Memcached)
3. Use replicated database with failover
4. Use load balancer with health checks
5. Monitor and alert on failures

---

## Customization

### How do I add custom claims?

Implement `IClaimsEnrichmentService`:
```csharp
public class CustomClaimsEnrichment : IClaimsEnrichmentService
{
    public async Task<IEnumerable<Claim>> EnrichClaimsAsync(
        User user, Client client, IEnumerable<string> requestedScopes)
    {
        var claims = new List<Claim>();
        claims.Add(new("department", user.Department));
        claims.Add(new("employee_id", user.EmployeeId));
        return claims;
    }
}
```

Then register:
```csharp
services.AddScoped<IClaimsEnrichmentService, CustomClaimsEnrichment>();
```

### How do I implement ABAC (Attribute-Based Access Control)?

Use `PolicyEnforcementService`:
```csharp
public class TimePolicyEnforcer : IPolicyEnforcementService
{
    public async Task<bool> EnforceAsync(User user, string policy, object context)
    {
        if (policy == "business_hours")
        {
            var now = DateTime.UtcNow;
            return now.Hour >= 9 && now.Hour < 17;
        }
        return true;
    }
}
```

### Can I use a different password hashing algorithm?

Yes. Replace in `UserService`:
```csharp
private string HashPassword(string password, string salt)
{
    // Replace PBKDF2 with Argon2, bcrypt, etc.
    using var scrypt = new Scrypt(N: 16384, r: 8, p: 1);
    var hash = scrypt.Hash(password, Encoding.UTF8.GetBytes(salt));
    return Convert.ToBase64String(hash);
}
```

### How do I add social login (Google, GitHub)?

Not built-in, but architecture supports it. Add:
```csharp
public class GoogleOAuthHandler
{
    public async Task<User> AuthenticateAsync(string googleToken)
    {
        // Validate token with Google
        var claims = await ValidateGoogleTokenAsync(googleToken);
        
        // Find or create user
        var user = await _userRepository.FindByEmailAsync(claims.Email);
        if (user == null)
            user = await _userRepository.AddAsync(new User { Email = claims.Email });
        
        return user;
    }
}
```

### Can I customize the consent UI?

Yes. Currently returns a simple HTML form. Customize:
```csharp
[HttpGet("authorize")]
public IActionResult Authorize(string clientId, string scope, ...)
{
    var model = new ConsentViewModel { ... };
    return View("CustomConsent", model); // Your template
}
```

---

## Integration

### How do I integrate with my existing ASP.NET API?

```csharp
// In your API's Startup
services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://auth.example.com";
        options.Audience = "my-api";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true
        };
    });

// In controllers
[Authorize]
[HttpGet("profile")]
public IActionResult GetProfile()
{
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    return Ok(new { userId });
}
```

### How do I integrate with Angular/React SPAs?

Use `@angular/oauth2-oidc` (Angular) or `oidc-client-ts` (React):

```typescript
// React example
import { useCallback } from 'react';
import { UserManager } from 'oidc-client-ts';

const userManager = new UserManager({
    authority: 'https://auth.example.com',
    client_id: 'my-spa',
    redirect_uri: 'https://myapp.com/callback',
    response_type: 'code',
    scope: 'openid profile email'
});

export function LoginButton() {
    const handleLogin = useCallback(() => {
        userManager.signinRedirect();
    }, []);
    
    return <button onClick={handleLogin}>Login</button>;
}
```

### How do I protect a Next.js API route?

```typescript
// pages/api/protected.ts
import { withAuth } from 'next-auth/middleware';
import { getToken } from 'next-auth/jwt';

export default withAuth(async (req) => {
    const token = await getToken({ req });
    
    return new Response(JSON.stringify({
        message: 'Protected resource',
        user: token?.sub
    }), { status: 200 });
});
```

---

## Troubleshooting

### Tokens expire too quickly

The access token lifetime is configurable:
```json
{
  "AuthServer": {
    "AccessTokenLifetimeSeconds": 3600
  }
}
```

Default is 1 hour. Increase for less frequent refresh requirements, but security best practice is shorter lifetime.

### Refresh token rotation seems excessive

That's by design! On each refresh:
1. Old token is revoked
2. New token issued
3. If replay detected (using old token again), all tokens from that chain revoked

This limits damage from token leaks.

### Client can't find authorization endpoint

Ensure OIDC discovery is working:
```bash
curl https://auth.example.com/.well-known/openid-configuration
```

Returns metadata including `authorization_endpoint`.

### CORS errors in browser

Add frontend origin to config:
```json
{
  "Cors": {
    "AllowedOrigins": ["https://myapp.com"]
  }
}
```

### Getting "invalid_scope" error

Client doesn't have scope registered. Update client config:
```json
{
  "Clients": [
    {
      "ClientId": "myapp",
      "AllowedScopes": ["openid", "profile", "email", "api:write"]
    }
  ]
}
```

### Token validation fails in resource server

Resource server issuer must match:
```csharp
options.Authority = "https://auth.example.com"; // Must match!
```

### Account locked - can't login

Account lockout happens after configurable failed attempts:
```json
{
  "AuthServer": {
    "MaxFailedLoginAttempts": 5,
    "AccountLockoutDurationMinutes": 15
  }
}
```

User is automatically unlocked after lockout duration expires, OR unlock via admin API (future feature).

---

## Performance

### How many concurrent users can it handle?

Depends on hardware and configuration:
- **In-memory storage**: ~1000 concurrent users per instance
- **With persistent DB**: Limited by database (typically 10,000+)
- **Behind load balancer**: Scale horizontally (3+ instances)

Real capacity depends on request complexity and desired latency.

### How long do token operations take?

Typical latencies:
- Authorization request validation: **10-50ms**
- Token issuance: **20-100ms**
- Token introspection: **5-20ms** (cached)
- Token refresh: **30-150ms**

Network latency dominates for remote operations.

### Does it support caching?

Yes. Configure in `appsettings.json`:
```json
{
  "Cache": {
    "Type": "Memory",
    "AbsoluteExpirationMinutes": 60
  }
}
```

For production, use distributed cache:
```json
{
  "Cache": {
    "Type": "Distributed",
    "ConnectionString": "localhost:6379"
  }
}
```

---

## Support & Contributing

### Where do I report bugs?

GitHub Issues: https://github.com/Sarmkadan/dotnet-auth-server/issues

Include:
- Reproduction steps
- Expected vs actual behavior
- Environment (.NET version, OS, etc.)
- Error logs

### How do I contribute?

1. Fork repository
2. Create feature branch: `git checkout -b feature/my-feature`
3. Commit changes: `git commit -am "Add feature"`
4. Push: `git push origin feature/my-feature`
5. Open Pull Request

See [Contributing Guide](../CONTRIBUTING.md) for details.

### Is there professional support?

Open source project - community support via GitHub Issues. For commercial support, contact author: rutova2@gmail.com

---

## License

MIT License - Free for personal and commercial use.

See [LICENSE](../LICENSE) for details.

---

For more information:
- [README.md](../README.md) - Overview and quick start
- [docs/getting-started.md](./getting-started.md) - Step-by-step setup
- [docs/architecture.md](./architecture.md) - System design
- [docs/deployment.md](./deployment.md) - Production deployment
