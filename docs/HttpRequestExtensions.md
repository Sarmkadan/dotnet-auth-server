# HttpRequestExtensions

The `HttpRequestExtensions` class is a static class of extension methods on ASP.NET Core's `HttpRequest`. It reads OAuth 2.0 parameters from the query string and form body, extracts client credentials (HTTP Basic or form), resolves the client IP address, reads the bearer token, and reports whether the request arrived over a secure transport. The methods are used by the controllers, handlers, and middleware in this project. All methods are read-oriented: they do not write headers or change the request, but reading the form body has side effects described in [Remarks](#remarks).

The class lives in the `DotnetAuthServer.Extensions` namespace and is declared `public static`. Its only private member is the helper `ExtractBasicAuthCredentials`, which is not part of the public API.

## API

| Method | Returns | Reads |
|---|---|---|
| [`GetOAuthParameter`](#getoauthparameter) | `string?` | Query string, then form body |
| [`ExtractClientCredentials`](#extractclientcredentials) | `(string? ClientId, string? ClientSecret)` | `Authorization` header, then query/form |
| [`GetClientIpAddress`](#getclientipaddress) | `string?` | `X-Forwarded-For`, then connection |
| [`IsSecureTransport`](#issecuretransport) | `bool` | `IsHttps`, then `X-Forwarded-Proto` |
| [`GetBearerToken`](#getbearertoken) | `string?` | `Authorization` header |

### GetOAuthParameter

```csharp
public static string? GetOAuthParameter(this HttpRequest request, string parameterName)
```

Gets the value of an OAuth parameter. The query string is checked first. If the parameter is not there and the request has a form content type, the form body is checked. Headers are not searched.

**Parameters**

- `request`: The HTTP request to read.
- `parameterName`: The parameter name to look up.

**Returns**

The first value found, as a string, or `null` if the parameter is in neither location. When the parameter is present but empty (for example `?state=`), the empty string is returned, not `null`. If the parameter repeats, only the first value is returned.

**Exceptions**

- `ArgumentNullException`: `request` or `parameterName` is `null`.

### ExtractClientCredentials

```csharp
public static (string? ClientId, string? ClientSecret) ExtractClientCredentials(this HttpRequest request)
```

Gets the client ID and client secret. The values are first read from the `client_id` and `client_secret` query or form parameters, using the same lookup as `GetOAuthParameter`. If the `Authorization` header starts with `Basic ` (case-insensitive) and decodes, the values from the header replace both form values, even when the form values were present.

If the header is malformed (for example, the Base64 is invalid), the exception is swallowed and the form values are kept. An `Authorization` header with a non-Basic scheme, such as `Bearer`, is ignored.

**Parameters**

- `request`: The HTTP request to read.

**Returns**

A tuple `(ClientId, ClientSecret)`. Each element is `null` if no value was found. A decoded Basic header with no colon produces the whole decoded string as `ClientId` and an empty string as `ClientSecret`, not `null`.

**Exceptions**

- `ArgumentNullException`: `request` is `null`.

### GetClientIpAddress

```csharp
public static string? GetClientIpAddress(this HttpRequest request)
```

Gets the client's IP address. If the `X-Forwarded-For` header is present, the value is split on commas, trimmed, and the first non-empty entry is returned. If the header is absent, the method returns `HttpContext.Connection.RemoteIpAddress` as a string.

If `X-Forwarded-For` is present but contains no non-empty entries (for example, an empty header value), the method returns `null` and does not fall back to the connection address.

**Parameters**

- `request`: The HTTP request to read.

**Returns**

The IP address as a string, or `null` if it cannot be determined. IPv6 addresses are returned in the form produced by `IPAddress.ToString()`.

**Exceptions**

- `ArgumentNullException`: `request` is `null`.

### IsSecureTransport

```csharp
public static bool IsSecureTransport(this HttpRequest request)
```

Determines whether the request is secure. It returns `true` if `request.IsHttps` is `true`. Otherwise, if the `X-Forwarded-Proto` header is present, the result is `true` only when that header's value equals `https` (case-insensitive). If the header is absent, the result is `false`.

When `X-Forwarded-Proto` is present, its value decides the result even if `IsHttps` is `false`. A comma-separated list such as `https,http` does not match, so the method returns `false`.

**Parameters**

- `request`: The HTTP request to read.

**Returns**

`true` if the request is considered HTTPS; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `request` is `null`.

### GetBearerToken

```csharp
public static string? GetBearerToken(this HttpRequest request)
```

Gets the bearer token from the `Authorization` header. The value must start with `Bearer ` (case-insensitive). The token is everything after the first seven characters, with no trimming, so extra spaces after the scheme stay in the result.

**Parameters**

- `request`: The HTTP request to read.

**Returns**

The token, or `null` if the header is missing or does not start with `Bearer `. A header of exactly `Bearer ` returns an empty string.

**Exceptions**

- `ArgumentNullException`: `request` is `null`.

## Usage

### Reading an OAuth parameter

```csharp
public IActionResult Authorize()
{
    var responseType = Request.GetOAuthParameter("response_type");
    if (string.IsNullOrEmpty(responseType))
        return BadRequest("response_type is required.");

    return Ok();
}
```

### Authenticating a client at the token endpoint

```csharp
public IActionResult Token()
{
    var (clientId, clientSecret) = Request.ExtractClientCredentials();
    if (string.IsNullOrEmpty(clientId) || clientSecret is null)
        return Unauthorized();

    return Ok();
}
```

### Logging the caller and transport

```csharp
public void LogRequest(HttpRequest request)
{
    var ip = request.GetClientIpAddress() ?? "unknown";
    var scheme = request.IsSecureTransport() ? "HTTPS" : "HTTP";

    _logger.LogInformation("{Scheme} request from {Ip}", scheme, ip);
}
```

## Remarks

- **Trust of forwarded headers.** `GetClientIpAddress` and `IsSecureTransport` trust `X-Forwarded-For` and `X-Forwarded-Proto` as sent. Any client can set them. Use them only behind a proxy that overwrites these headers, or the returned values can be spoofed.
- **Form body side effects.** `GetOAuthParameter` and `ExtractClientCredentials` read `request.Form` when the content type is a form. ASP.NET Core caches the parsed form on the request, but the underlying body stream is consumed if it was not buffered. Exceptions from that read are not caught by these methods.
- **No percent-decoding of Basic credentials.** `ExtractClientCredentials` does not URL-decode the client ID and secret from a Basic header, as RFC 6749 section 2.3.1 would require for characters that were form-encoded. Credentials containing reserved characters may not match what was registered.
- **Basic credentials split on the first colon.** The client ID is everything before the first `:`, and the secret is everything after it, so a client ID cannot contain a colon.
- **Null checks.** Every method throws `ArgumentNullException` for a `null` request. `GetClientIpAddress` also reads `request.HttpContext`, which is always set for requests in the ASP.NET Core pipeline.
