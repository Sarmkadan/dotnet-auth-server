# AuthorizationService

`AuthorizationService` orchestrates the OAuth 2.0 authorization flow on the server side. It validates incoming authorization requests, manages consent prompts, creates authorization grants, and enforces PKCE verification. The service also exposes contextual properties describing the current client, user, and requested scopes, and provides a maintenance method for removing expired grants from the backing store.

## API

### Properties

#### `ClientId`
`public string ClientId`

The unique identifier of the OAuth 2.0 client that initiated the authorization request.

#### `ClientName`
`public string ClientName`

The display name of the requesting client, suitable for presentation in consent screens.

#### `ClientLogoUri`
`public string? ClientLogoUri`

An optional URI pointing to the client’s logo image. Returns `null` when the client has not registered a logo.

#### `ClientDescription`
`public string? ClientDescription`

An optional human-readable description of the client application. Returns `null` when no description is available.

#### `UserId`
`public string UserId`

The unique identifier of the resource owner (authenticated user) participating in the authorization flow.

#### `UserName`
`public string UserName`

The display name of the authenticated user, intended for consent UI rendering.

#### `RequestedScopes`
`public IEnumerable<string> RequestedScopes`

The set of scope strings the client has requested. This collection reflects the scopes parsed from the original authorization request.

#### `RequireConsent`
`public bool RequireConsent`

Indicates whether explicit user consent must be obtained before an authorization grant can be issued. Evaluated based on client configuration, previously granted consents, and scope sensitivity.

### Constructor

#### `AuthorizationService`
```csharp
public AuthorizationService(/* implementation-defined dependencies */)
```
Instantiates the service with the required dependencies for request validation, grant persistence, and consent evaluation. The exact constructor parameters are determined by the dependency injection configuration of the host application.

### Methods

#### `ValidateAuthorizationRequestAsync`
```csharp
public async Task<AuthorizationRequest> ValidateAuthorizationRequestAsync(
    /* authorization request parameters */)
```
Parses and validates an incoming OAuth 2.0 authorization request. This includes verifying the client identity, redirect URI matching, response type support, and scope validity.

- **Returns**: A validated `AuthorizationRequest` object containing the normalized request parameters.
- **Throws**: Throws an exception when the client is unknown, the redirect URI is invalid or mismatched, the response type is unsupported, or required parameters are missing. The specific exception type is determined by the error handling policy of the server.

#### `CreateAuthorizationGrantAsync`
```csharp
public async Task<AuthorizationGrant> CreateAuthorizationGrantAsync(
    AuthorizationRequest request)
```
Generates an authorization grant (typically an authorization code) based on a validated request. The grant is persisted and associated with the client, user, and requested scopes.

- **Parameters**:
  - `request`: The validated `AuthorizationRequest` from which to produce the grant.
- **Returns**: An `AuthorizationGrant` containing the generated code and its metadata.
- **Throws**: Throws when persistence fails or when the request state does not permit grant creation (e.g., consent required but not yet obtained).

#### `GetConsentPromptAsync`
```csharp
public async Task<ConsentResponse> GetConsentPromptAsync()
```
Retrieves the current consent state for the active client, user, and requested scopes. When `RequireConsent` is `true`, the returned `ConsentResponse` describes the scopes needing approval and any previously granted scopes that can be silently authorized.

- **Returns**: A `ConsentResponse` detailing which scopes require explicit user action.
- **Throws**: Throws when the service state is incomplete (e.g., no validated request has been loaded prior to calling this method).

#### `ValidatePkceCodeVerifier`
```csharp
public bool ValidatePkceCodeVerifier(
    string codeVerifier,
    string storedCodeChallenge,
    string codeChallengeMethod)
```
Verifies a PKCE code verifier against the stored code challenge using the specified transform method (S256 or plain).

- **Parameters**:
  - `codeVerifier`: The verifier string submitted by the client at the token endpoint.
  - `storedCodeChallenge`: The challenge originally sent in the authorization request.
  - `codeChallengeMethod`: The challenge method (`"S256"` or `"plain"`).
- **Returns**: `true` when the verifier correctly matches the challenge; `false` otherwise.
- **Throws**: Does not throw. Returns `false` for any mismatch, including unsupported challenge methods.

#### `CleanupExpiredGrantsAsync`
```csharp
public async Task CleanupExpiredGrantsAsync()
```
Removes all authorization grants and associated tokens that have exceeded their configured lifetime from the persistent store. This is intended for periodic background execution.

- **Returns**: A completed task when the cleanup operation finishes.
- **Throws**: Throws when the underlying data store operation fails.

## Usage

### Example 1: Full Authorization Flow with Consent

```csharp
// Assume AuthorizationService is injected via DI
public async Task<IActionResult> Authorize(
    AuthorizationService authService,
    string clientId,
    string redirectUri,
    string responseType,
    string scope,
    string state,
    string codeChallenge,
    string codeChallengeMethod)
{
    // Validate the incoming request
    var authRequest = await authService.ValidateAuthorizationRequestAsync(
        clientId, redirectUri, responseType, scope, state,
        codeChallenge, codeChallengeMethod);

    // Check if consent is required
    if (authService.RequireConsent)
    {
        var consentResponse = await authService.GetConsentPromptAsync();
        // Render consent view using:
        //   consentResponse.ScopesRequested
        //   authService.ClientName
        //   authService.ClientLogoUri
        return View("Consent", consentResponse);
    }

    // No consent needed — create grant immediately
    var grant = await authService.CreateAuthorizationGrantAsync(authRequest);
    var redirectUrl = $"{redirectUri}?code={grant.Code}&state={state}";
    return Redirect(redirectUrl);
}
```

### Example 2: Background Grant Cleanup

```csharp
public class GrantCleanupBackgroundService : BackgroundService
{
    private readonly AuthorizationService _authService;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(1);

    public GrantCleanupBackgroundService(AuthorizationService authService)
    {
        _authService = authService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(_cleanupInterval, stoppingToken);
            await _authService.CleanupExpiredGrantsAsync();
        }
    }
}
```

## Notes

- **Stateful service**: The properties `ClientId`, `ClientName`, `ClientLogoUri`, `ClientDescription`, `UserId`, `UserName`, `RequestedScopes`, and `RequireConsent` reflect the context of the most recently validated authorization request. Accessing them before calling `ValidateAuthorizationRequestAsync` yields default or empty values.
- **Consent preconditions**: `GetConsentPromptAsync` must only be called after a successful validation and when `RequireConsent` is `true`. Calling it in other states will throw.
- **PKCE enforcement**: `ValidatePkceCodeVerifier` is a stateless utility method. It does not depend on the current request context and can be called independently at the token endpoint.
- **Thread safety**: The service is designed for scoped lifetime (typically scoped to an HTTP request in ASP.NET Core). It is not safe for concurrent use across multiple authorization flows. Each request should receive its own instance.
- **Cleanup scope**: `CleanupExpiredGrantsAsync` operates on all grants in the store, not only those associated with the current service instance. It is suitable for invocation from a background job or scheduled task.
- **Error handling**: Validation and grant creation methods throw on failure. Callers should wrap these calls in try-catch blocks and translate exceptions into appropriate OAuth 2.0 error responses (e.g., `invalid_request`, `unauthorized_client`).
