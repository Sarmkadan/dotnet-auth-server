# Getting Started with dotnet-auth-server

A step-by-step guide to set up and run the authorization server locally and in production.

## Table of Contents

1. [Local Development Setup](#local-development-setup)
2. [Running the Server](#running-the-server)
3. [Testing with Swagger UI](#testing-with-swagger-ui)
4. [Creating Your First Client](#creating-your-first-client)
5. [Testing the Authorization Flow](#testing-the-authorization-flow)
6. [Next Steps](#next-steps)

---

## Local Development Setup

### Prerequisites

- **Operating System**: Windows, macOS, or Linux
- **.NET 10.0 SDK**: [Download here](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Git**: For cloning and version control
- **Visual Studio Code** or **Visual Studio 2025** (optional but recommended)

### Step 1: Clone the Repository

```bash
git clone https://github.com/Sarmkadan/dotnet-auth-server.git
cd dotnet-auth-server
```

### Step 2: Restore NuGet Packages

```bash
dotnet restore
```

This downloads all required dependencies defined in `dotnet-auth-server.csproj`.

### Step 3: Verify .NET Installation

```bash
dotnet --version
# Output: 10.0.x (or newer)
```

### Step 4: (Optional) Open in IDE

**Visual Studio Code:**
```bash
code .
```

**Visual Studio 2025:**
```bash
# Double-click dotnet-auth-server.csproj
```

---

## Running the Server

### Development Mode

```bash
dotnet run
```

The server will:
- Start at `https://localhost:7001`
- Generate a self-signed SSL certificate (first run only)
- Enable Swagger UI at `https://localhost:7001/swagger`
- Output request logs to console

**Expected Output:**
```
info: DotnetAuthServer.Program[0]
      Starting server at https://localhost:7001
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7001
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to exit.
```

### Release Mode (Production-like)

```bash
dotnet run --configuration Release
```

Provides:
- Optimized performance
- Minified output
- Disabled developer features

### Debug Mode with Breakpoints

In Visual Studio Code or Visual Studio:
1. Open the project
2. Set breakpoints in code
3. Press `F5` or click Debug → Start Debugging
4. Server runs with debugger attached

---

## Testing with Swagger UI

Swagger provides an interactive API documentation and testing interface.

### Access Swagger UI

1. Start the server: `dotnet run`
2. Open browser: `https://localhost:7001/swagger`
3. You'll see all API endpoints with descriptions and test forms

### Testing the Authorization Endpoint

1. In Swagger, find `GET /oauth/authorize`
2. Click "Try it out"
3. Fill in parameters:
   ```
   client_id: test-client
   response_type: code
   redirect_uri: https://localhost:3000/callback
   scope: openid profile email
   code_challenge: E9Mrozoa2owuBmwOXwItzdsQXeGAg5nSstw_5MC6mAA
   code_challenge_method: S256
   state: random-state-123
   ```
4. Click "Execute"
5. You'll be redirected to login page

### Testing the Token Endpoint

1. In Swagger, find `POST /oauth/token`
2. Click "Try it out"
3. Fill in request body:
   ```
   {
     "grant_type": "authorization_code",
     "client_id": "test-client",
     "code": "AUTH_CODE_FROM_PREVIOUS_STEP",
     "code_verifier": "E9Mrozoa2owuBmwOXwItzdsQXeGAg5nSstw_5MC6mAA",
     "redirect_uri": "https://localhost:3000/callback"
   }
   ```
4. Click "Execute"
5. Response includes access token, refresh token, and ID token

---

## Creating Your First Client

### Via Configuration File

Edit `appsettings.json`:

```json
{
  "AuthServer": {
    "IssuerUrl": "https://localhost:7001",
    "JwtSigningKey": "your-secret-key-min-256-bits-long",
    "AccessTokenLifetimeSeconds": 3600,
    "RefreshTokenLifetimeSeconds": 2592000
  },
  "Clients": [
    {
      "ClientId": "my-spa",
      "ClientName": "My Single Page Application",
      "RedirectUris": [
        "https://localhost:3000/callback",
        "https://myapp.com/callback"
      ],
      "AllowedScopes": ["openid", "profile", "email"],
      "ClientType": "Public",
      "RequirePkce": true
    },
    {
      "ClientId": "my-backend",
      "ClientName": "My Backend Service",
      "ClientSecret": "super-secret-key-store-in-vault",
      "AllowedScopes": ["api:read", "api:write"],
      "ClientType": "Confidential"
    }
  ],
  "Users": [
    {
      "UserId": "user1",
      "Username": "alice",
      "Email": "alice@example.com",
      "PasswordHash": "hashed-password-here",
      "Roles": ["user", "editor"]
    }
  ]
}
```

### Via Registration Endpoint (Future Feature)

```bash
curl -X POST https://localhost:7001/admin/clients \
  -H "Content-Type: application/json" \
  -d '{
    "client_id": "new-client",
    "client_name": "New Application",
    "redirect_uris": ["https://newapp.com/callback"],
    "allowed_scopes": ["openid", "profile"],
    "client_type": "Public"
  }'
```

---

## Testing the Authorization Flow

### Complete End-to-End Test

**Step 1: Generate PKCE Parameters**

```bash
# macOS/Linux
code_verifier=$(openssl rand -base64 48 | tr -d '\n=+/' | cut -c1-128)
code_challenge=$(echo -n "$code_verifier" | openssl dgst -sha256 -binary | base64 | tr -d '=\n' | tr '+/' '-_')

echo "Verifier: $code_verifier"
echo "Challenge: $code_challenge"
```

**Windows (PowerShell):**

```powershell
# Generate verifier
$bytes = New-Object Byte[] 32
(Get-SecureRandom).GetBytes($bytes)
$verifier = [Convert]::ToBase64String($bytes) -replace '[+/=]'

# Generate challenge
$sha256 = [System.Security.Cryptography.SHA256]::Create()
$hash = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($verifier))
$challenge = [Convert]::ToBase64String($hash) -replace '[+/=]'

Write-Host "Verifier: $verifier"
Write-Host "Challenge: $challenge"
```

**Step 2: Initiate Authorization Request**

```bash
open "https://localhost:7001/oauth/authorize?client_id=my-spa&response_type=code&redirect_uri=https://localhost:3000/callback&scope=openid%20profile&code_challenge=$code_challenge&code_challenge_method=S256&state=random123"
```

**Step 3: Authenticate & Consent**

1. Browser opens login page
2. Enter credentials (e.g., username: `alice`, password: from config)
3. Review and accept requested scopes
4. Server redirects: `https://localhost:3000/callback?code=xyz123&state=random123`

**Step 4: Exchange Authorization Code for Tokens**

```bash
curl -X POST https://localhost:7001/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=authorization_code" \
  -d "client_id=my-spa" \
  -d "redirect_uri=https://localhost:3000/callback" \
  -d "code=xyz123" \
  -d "code_verifier=$code_verifier"
```

**Response:**

```json
{
  "access_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "refresh_token": "refresh_abc789...",
  "scope": "openid profile",
  "id_token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**Step 5: Use Access Token**

```bash
ACCESS_TOKEN="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."

curl -X GET https://api.example.com/user/profile \
  -H "Authorization: Bearer $ACCESS_TOKEN"
```

**Step 6: Refresh Expired Token**

```bash
REFRESH_TOKEN="refresh_abc789..."

curl -X POST https://localhost:7001/oauth/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=refresh_token" \
  -d "client_id=my-spa" \
  -d "refresh_token=$REFRESH_TOKEN"
```

New access token is issued; old refresh token is invalidated (rotation).

---

## Next Steps

### 1. Integrate with Your Application

- **SPA (React/Vue/Angular)**: Use OAuth2 library like `oidc-client-ts`
- **Mobile (iOS/Android)**: Use native OAuth libraries
- **Backend (ASP.NET, Node.js)**: Use middleware for token validation

### 2. Explore Advanced Features

- Read [architecture.md](./architecture.md) for system design
- Read [api-reference.md](./api-reference.md) for complete endpoint documentation
- Review [examples/](../examples/) for real-world code samples

### 3. Production Deployment

- See [deployment.md](./deployment.md) for hosting options
- Configure environment variables
- Set up HTTPS with proper certificates
- Implement persistent database

### 4. Secure Your Setup

- Generate strong JWT signing key (256+ bits)
- Use a secrets manager (Azure Key Vault, HashiCorp Vault)
- Enable HTTPS everywhere (not localhost)
- Implement rate limiting and monitoring

### 5. Test Thoroughly

```bash
# Run test suite
dotnet test

# Check code coverage
dotnet test /p:CollectCoverage=true
```

---

## Troubleshooting

### Issue: "Unable to connect to https://localhost:7001"

**Solutions:**
1. Verify .NET SDK is installed: `dotnet --version`
2. Ensure port 7001 is not in use: `netstat -an | grep 7001`
3. Check firewall settings
4. Try different port: `dotnet run --urls https://localhost:8001`

### Issue: "Self-signed certificate error"

**Solution**: Trust the development certificate

```bash
dotnet dev-certs https --trust
```

### Issue: Swagger UI shows 401 Unauthorized

**Solution**: This is expected for protected endpoints. Use public endpoints first:
- `GET /.well-known/openid-configuration`
- `GET /.well-known/jwks.json`

### Issue: "Page not found" for login

**Solution**: Ensure you have default client configured in `appsettings.json`

---

For more help, see [FAQ](./faq.md) or open an issue on GitHub.
