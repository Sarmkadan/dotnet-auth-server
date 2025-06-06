# TokenResponse

Represents the response received from an OAuth 2.0 token endpoint. It encapsulates the standard token properties (access token, token type, expiration, optional refresh token, scope, and ID token) as well as any custom properties returned by the authorization server. Instances are typically deserialized from the JSON response of a token request.

## API

### `public string AccessToken`
The access token issued by the authorization server. This is a required field and will never be `null` in a valid response.

- **Type:** `string`
- **Throws:** Never.

### `public string TokenType`
The type of the token (e.g., `"Bearer"`). Indicates how the access token should be used when presenting it to resource servers.

- **Type:** `string`
- **Throws:** Never.

### `public int ExpiresIn`
The lifetime of the access token in seconds. For example, a value of `3600` indicates the token expires in one hour.

- **Type:** `int`
- **Throws:** Never.

### `public string? RefreshToken`
An optional refresh token that can be used to obtain a new access token when the current one expires. May be `null` if the server does not issue refresh tokens.

- **Type:** `string?`
- **Throws:** Never.

### `public string? Scope`
An optional space-delimited list of scopes granted by the authorization server. May be `null` if the scope is identical to the requested scope or if no scope was requested.

- **Type:** `string?`
- **Throws:** Never.

### `public string? IdToken`
An optional OpenID Connect ID token, typically a JWT. Present only when the original request included the `openid` scope.

- **Type:** `string?`
- **Throws:** Never.

### `public Dictionary<string, object> CustomProperties`
A mutable dictionary containing any additional properties returned by the server that are not part of the standard OAuth 2.0 response. Keys are property names as strings; values are deserialized objects (e.g., `string`, `int`, `JToken`). This dictionary is never `null` but may be empty.

- **Type:** `Dictionary<string, object>`
- **Throws:** Never.

## Usage

### Example 1: Handling a standard token response

```csharp
// Assume tokenResponse is deserialized from the token endpoint JSON
TokenResponse tokenResponse = await client.RequestTokenAsync(...);

// Extract standard properties
string accessToken = tokenResponse.AccessToken;
string tokenType = tokenResponse.TokenType;
int expiresIn = tokenResponse.ExpiresIn;

// Optional fields
string? refreshToken = tokenResponse.RefreshToken;
string? scope = tokenResponse.Scope;
string? idToken = tokenResponse.IdToken;

// Use the access token in an HTTP request
using var httpClient = new HttpClient();
httpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(tokenType, accessToken);
```

### Example 2: Accessing custom properties

```csharp
// After receiving a token response from a server that includes extra fields
TokenResponse tokenResponse = await client.RequestTokenAsync(...);

// Check for a custom property "tenant_id"
if (tokenResponse.CustomProperties.TryGetValue("tenant_id", out object? tenantIdObj))
{
    string tenantId = tenantIdObj?.ToString() ?? "";
    Console.WriteLine($"Tenant ID: {tenantId}");
}

// Iterate over all custom properties
foreach (var kvp in tokenResponse.CustomProperties)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## Notes

- **Nullability:** The optional properties (`RefreshToken`, `Scope`, `IdToken`) may be `null`. Always check for `null` before using them, especially when passing to methods that do not accept `null`.
- **Expiration:** The `ExpiresIn` value is relative to the time the response was issued. It is the caller’s responsibility to track the absolute expiration time (e.g., by recording `DateTime.UtcNow + TimeSpan.FromSeconds(ExpiresIn)`).
- **Custom Properties:** The `CustomProperties` dictionary is mutable. Modifying it after the response is received will affect the instance. The dictionary is not thread-safe; concurrent reads and writes should be synchronized externally.
- **Thread Safety:** Instances of `TokenResponse` are not inherently thread-safe. If the same instance is accessed from multiple threads, external synchronization (e.g., locking) is required. For read-only scenarios, copying the data into an immutable structure is recommended.
- **Deserialization:** When deserializing from JSON, ensure that the `CustomProperties` dictionary is populated with any extra fields not explicitly mapped to the standard properties. Libraries like `System.Text.Json` or `Newtonsoft.Json` can be configured to capture additional properties.
