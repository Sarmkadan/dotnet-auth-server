# HttpRequestExtensions

Provides a set of extension methods for the ASP.NET Core `HttpRequest` class, simplifying common authentication-related tasks such as extracting OAuth parameters, client credentials, client IP addresses, bearer tokens, and checking transport security. These methods are designed for use in middleware, controllers, or authorization handlers within the `dotnet-auth-server` project.

## API

### `GetOAuthParameter`

```csharp
public static string? GetOAuthParameter(this HttpRequest request, string parameterName)
```

Retrieves the value of an OAuth parameter from the request. The method first checks the query string, then the `Content-Type` form data (if applicable), and finally the request headers. Returns `null` if the parameter is not found.

- **Parameters**  
  - `request`: The HTTP request to inspect.  
  - `parameterName`: The name of the OAuth parameter (e.g., `"client_id"`, `"response_type"`).

- **Returns**  
  `string?` – The parameter value, or `null` if not present.

- **Throws**  
  `ArgumentNullException` if `request` is `null`.  
  `ArgumentException` if `parameterName` is `null` or empty.

### `ExtractClientCredentials`

```csharp
public static (string? ClientId, string? ClientSecret) ExtractClientCredentials(this HttpRequest request)
```

Extracts client credentials from the request. The method attempts to read credentials from the `Authorization` header (Basic authentication scheme) first; if that fails, it falls back to the request body (form-encoded `client_id` and `client_secret` parameters).

- **Parameters**  
  - `request`: The HTTP request to inspect.

- **Returns**  
  `(string? ClientId, string? ClientSecret)` – A tuple containing the client ID and client secret, each possibly `null` if not found.

- **Throws**  
  `ArgumentNullException` if `request` is `null`.

### `GetClientIpAddress`

```csharp
public static string? GetClientIpAddress(this HttpRequest request)
```

Returns the client’s IP address as a string. The method checks the `X-Forwarded-For` header first (if present), then falls back to `HttpContext.Connection.RemoteIpAddress`. Returns `null` if the IP address cannot be determined.

- **Parameters**  
  - `request`: The HTTP request to inspect.

- **Returns**  
  `string?` – The client IP address, or `null`.

- **Throws**  
  `ArgumentNullException` if `request` is `null`.

### `IsSecureTransport`

```csharp
public static bool IsSecureTransport(this HttpRequest request)
```

Determines whether the request was made over a secure transport (HTTPS). The method checks `request.IsHttps` and, if behind a reverse proxy, also inspects the `X-Forwarded-Proto` header.

- **Parameters**  
  - `request`: The HTTP request to inspect.

- **Returns**  
  `bool` – `true` if the request is considered secure; otherwise `false`.

- **Throws**  
  `ArgumentNullException` if `request` is `null`.

### `GetBearerToken`

```csharp
public static string? GetBearerToken(this HttpRequest request)
```

Extracts a Bearer token from the `Authorization` header. The header value must start with `"Bearer "` (case-insensitive). Returns the token portion, or `null` if the header is missing or malformed.

- **Parameters**  
  - `request`: The HTTP request to inspect.

- **Returns**  
  `string?` – The Bearer token, or `null`.

- **Throws**  
  `ArgumentNullException` if `request` is `null`.

## Usage

### Example 1: Validating a Bearer token and extracting client credentials

```csharp
public async Task<IActionResult> TokenEndpoint(HttpRequest request)
{
    var bearerToken = request.GetBearerToken();
    if (bearerToken == null)
        return Unauthorized("Missing Bearer token.");

    var (clientId, clientSecret) = request.ExtractClientCredentials();
    if (clientId == null || clientSecret == null)
        return BadRequest("Client credentials are required.");

    // Validate token and credentials...
    return Ok(new { access_token = "..." });
}
```

### Example 2: Logging client IP and transport security

```csharp
public void LogRequestInfo(HttpRequest request)
{
    var ip = request.GetClientIpAddress() ?? "unknown";
    var secure = request.IsSecureTransport() ? "HTTPS" : "HTTP";
    var oauthParam = request.GetOAuthParameter("response_type");

    Console.WriteLine($"[{secure}] Request from {ip}, response_type={oauthParam ?? "N/A"}");
}
```

## Notes

- All methods throw `ArgumentNullException` if the `request` parameter is `null`. Ensure the request object is not null before calling.
- `GetOAuthParameter` searches multiple locations (query, form, header). If the same parameter name appears in more than one location, the first match wins (query > form > header).
- `ExtractClientCredentials` prioritizes the `Authorization` Basic header over form parameters. If both are present, the header value is used.
- `GetClientIpAddress` relies on `X-Forwarded-For` headers. In production, ensure your reverse proxy is configured to forward trusted headers to avoid IP spoofing.
- `IsSecureTransport` respects the `X-Forwarded-Proto` header, which should only be trusted when the proxy is known and configured correctly.
- `GetBearerToken` expects exactly one `Authorization` header. Multiple headers with the same name may cause undefined behavior.
- All extension methods are stateless and thread-safe. They do not modify the `HttpRequest` object and can be called concurrently from multiple threads on the same request instance.
