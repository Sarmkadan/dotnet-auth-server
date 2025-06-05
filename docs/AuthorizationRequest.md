# AuthorizationRequest

Represents an incoming authorization request as defined by OAuth 2.0 and OpenID Connect. This class encapsulates all standard and custom parameters sent by the client to the authorization endpoint, provides validation logic, and exposes convenience members for inspecting the request’s characteristics such as PKCE usage, requested scopes, and OpenID Connect intent.

## API

### `public string? ClientId`
The client identifier issued to the client during registration. Corresponds to the `client_id` parameter. May be `null` if omitted from the request.

### `public string? ResponseType`
The requested response type, typically `"code"`, `"token"`, `"id_token"`, or combinations thereof. Corresponds to the `response_type` parameter. May be `null` if omitted.

### `public string? RedirectUri`
The redirection URI to which the authorization server will send the user-agent after completing the interaction. Corresponds to the `redirect_uri` parameter. May be `null` if omitted.

### `public string? Scope`
A space-delimited string of scope values requested by the client. Corresponds to the `scope` parameter. May be `null` if omitted.

### `public string? State`
An opaque value used by the client to maintain state between the request and callback. Corresponds to the `state` parameter. May be `null` if omitted.

### `public string? Nonce`
A string value used to associate a client session with an ID Token and mitigate replay attacks. Corresponds to the `nonce` parameter. Required for the implicit flow when `response_type` includes `id_token`. May be `null` if omitted.

### `public string? CodeChallenge`
The code challenge derived from the code verifier for PKCE (Proof Key for Code Exchange). Corresponds to the `code_challenge` parameter. May be `null` if PKCE is not used.

### `public string? CodeChallengeMethod`
The method used to derive the code challenge, either `"S256"` (SHA-256) or `"plain"`. Corresponds to the `code_challenge_method` parameter. May be `null` if PKCE is not used.

### `public string? Display`
A string specifying how the authorization server should display the authentication and consent UI. Defined values include `"page"`, `"popup"`, `"touch"`, and `"wap"`. Corresponds to the `display` parameter. May be `null` if omitted.

### `public string? Prompt`
A space-delimited, case-sensitive list of ASCII string values that specifies whether the authorization server prompts the end-user for re-authentication and consent. Defined values include `"none"`, `"login"`, `"consent"`, and `"select_account"`. Corresponds to the `prompt` parameter. May be `null` if omitted.

### `public int? MaxAge`
The maximum allowable elapsed time in seconds since the last active authentication of the end-user. Corresponds to the `max_age` parameter. May be `null` if omitted.

### `public string? UiLocales`
A space-delimited list of BCP 47 language tags for the UI locales requested by the client. Corresponds to the `ui_locales` parameter. May be `null` if omitted.

### `public string? AcrValues`
A space-delimited string of Authentication Context Class Reference values requested by the client. Corresponds to the `acr_values` parameter. May be `null` if omitted.

### `public string? LoginHint`
A hint to the authorization server about the login identifier the end-user might use. Corresponds to the `login_hint` parameter. May be `null` if omitted.

### `public string? IdTokenHint`
A previously issued ID Token passed as a hint about the end-user’s current or past session. Corresponds to the `id_token_hint` parameter. May be `null` if omitted.

### `public Dictionary<string, string> CustomParameters`
A dictionary of additional parameters sent in the authorization request that are not part of the standard OAuth 2.0 or OpenID Connect parameter set. Keys and values are strings. Returns an empty dictionary if no custom parameters were present.

### `public bool IsValid`
Indicates whether the authorization request passes basic structural validation. Returns `true` when required parameters are present and well-formed according to the specified `response_type`; otherwise `false`. Does not perform client-specific or policy-level validation.

### `public IEnumerable<string> GetRequestedScopes`
Returns the set of individual scope values parsed from the `Scope` property. Splits the space-delimited string into an enumerable collection of distinct scope strings. Returns an empty enumerable if `Scope` is `null` or empty.

### `public bool HasPkce`
Returns `true` when both `CodeChallenge` and `CodeChallengeMethod` are non-null, indicating the client has supplied PKCE parameters. Returns `false` otherwise.

### `public bool IsOpenIdRequest`
Returns `true` when the `Scope` property contains the `"openid"` scope value, indicating this is an OpenID Connect request rather than a plain OAuth 2.0 request. Returns `false` otherwise.

## Usage

### Example 1: Basic Authorization Code Flow with PKCE

```csharp
// Parse an incoming HTTP request into an AuthorizationRequest instance
var request = AuthorizationRequest.FromHttpRequest(httpRequest);

if (!request.IsValid)
{
    // Return an error response to the client
    return BadRequest("invalid_request");
}

if (request.HasPkce)
{
    // Store code_challenge and code_challenge_method for later verification
    await StorePkceParametersAsync(
        request.ClientId,
        request.CodeChallenge,
        request.CodeChallengeMethod);
}

var scopes = request.GetRequestedScopes();
if (scopes.Contains("openid"))
{
    // Handle as an OpenID Connect request
    var nonce = request.Nonce;
    // ... generate id_token with nonce
}

// Proceed with authorization flow
```

### Example 2: Handling Custom Parameters and Prompt Logic

```csharp
var request = AuthorizationRequest.FromHttpRequest(httpRequest);

// Inspect custom parameters for proprietary extensions
if (request.CustomParameters.TryGetValue("tenant_id", out var tenantId))
{
    // Apply tenant-specific authorization policies
    await ValidateTenantAccessAsync(request.ClientId, tenantId);
}

// Evaluate prompt parameter
if (request.Prompt == "none")
{
    // Attempt silent authentication; fail if user is not already logged in
    var existingSession = await GetExistingSessionAsync(request.IdTokenHint);
    if (existingSession == null)
    {
        return Error("login_required");
    }
}

// Respect max_age if provided
if (request.MaxAge.HasValue)
{
    var lastAuthTime = await GetLastAuthenticationTimeAsync();
    var elapsed = DateTime.UtcNow - lastAuthTime;
    if (elapsed.TotalSeconds > request.MaxAge.Value)
    {
        // Force re-authentication
        return Challenge();
    }
}
```

## Notes

- **Validation scope of `IsValid`**: This property performs structural validation only (e.g., required parameters present, `response_type` combinations are legal). It does not validate client registration status, redirect URI matching, or policy constraints. Callers must layer additional validation on top.
- **`GetRequestedScopes` parsing**: The method splits on spaces and returns distinct values. Duplicate scope entries in the raw string are collapsed. The returned enumerable is a snapshot; subsequent changes to the `Scope` property are not reflected.
- **PKCE detection via `HasPkce`**: Returns `true` only when both `CodeChallenge` and `CodeChallengeMethod` are non-null. A request with only one of the two parameters present is considered incomplete and `HasPkce` returns `false`. Such requests should typically be rejected during validation.
- **`IsOpenIdRequest`**: Relies solely on the presence of `"openid"` in the `Scope` string. It does not inspect `ResponseType` for `id_token`. A request may have `response_type=code` and still be an OpenID Connect request if `openid` is in scope.
- **Thread safety**: All public members are read-only properties or methods that return computed values without mutating internal state. Instances are effectively immutable after construction and safe for concurrent read access across multiple threads.
- **`CustomParameters`**: The dictionary is populated at construction time from unrecognized query parameters. It is not validated against any schema. Callers should treat values as untrusted input and apply appropriate sanitization before use.
