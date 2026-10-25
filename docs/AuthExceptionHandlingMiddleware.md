# AuthExceptionHandlingMiddleware

`AuthExceptionHandlingMiddleware` is an ASP.NET Core middleware that catches `AuthServerException` derivatives thrown further down the pipeline and writes them as RFC 6749 §5.2 JSON error responses (`{"error": ..., "error_description": ...}`). Each known exception type maps to a fixed HTTP status code and OAuth error code. Any other exception is reported as a generic `500 server_error`.

Source: `src/Middleware/AuthExceptionHandlingMiddleware.cs` (namespace `DotnetAuthServer.Middleware`).

> **Note:** `Program.cs` does not register this middleware. The default pipeline registers `ErrorHandlingMiddleware` instead (`app.UseMiddleware<ErrorHandlingMiddleware>()`). Register this class explicitly with `app.UseMiddleware<AuthExceptionHandlingMiddleware>()` if you want its behavior.

## API

### `AuthExceptionHandlingMiddleware` (constructor)

```csharp
public AuthExceptionHandlingMiddleware(RequestDelegate next)
```

**Purpose**
Creates the middleware and stores the next request delegate in the pipeline. ASP.NET Core calls this constructor when the middleware is registered; application code does not normally call it directly.

**Parameters**
- `next` (`RequestDelegate`): The next middleware in the pipeline.

**Throws**
- `ArgumentNullException`: `next` is `null`.

---

### `Task InvokeAsync(HttpContext context)`

```csharp
public async Task InvokeAsync(HttpContext context)
```

**Purpose**
Invokes the rest of the pipeline inside a `try` block. If the pipeline throws an `AuthServerException`, the middleware maps it to a status code and error code (see [Exception mapping](#exception-mapping)) and writes the JSON error response. The exception is not rethrown.

**Parameters**
- `context` (`HttpContext`): The current HTTP context.

**Returns**
A `Task` that completes when the next middleware has finished, or when the error response has been written.

**Throws**
- `ArgumentNullException`: `context` is `null`.
- Any exception that is not an `AuthServerException` is not caught and propagates to the caller.

## Exception mapping

The mapping is a type-pattern `switch` in `HandleExceptionAsync`, checked in the order shown. All of the listed exception types are `sealed`, so the patterns match exactly those types.

| Exception | HTTP status | `error` | Extra headers |
|---|---|---|---|
| `InvalidGrantException` | 400 | `invalid_grant` | — |
| `InvalidClientException` | 401 | `invalid_client` | `WWW-Authenticate: Basic` |
| `UnsupportedGrantTypeException` | 400 | `unsupported_grant_type` | — |
| `InvalidScopeException` | 400 | `invalid_scope` | — |
| `ValidationException` | 400 | `invalid_request` | — |
| Any other exception (including `AuthServerException` itself and `UnauthorizedClientException`, `ConfigurationException`) | 500 | `server_error` | — |

Notes on the mapping:
- `error_description` is always the exception's `Message`, except for the 500 case, where it is the fixed text `An internal server error occurred.`. The middleware does not read `AuthServerException.ErrorDescription`, `ErrorUri`, `ErrorCode`, `StatusCode` or `Details`.
- `UnauthorizedClientException` has its own `403` / `unauthorized_client` definition on the exception class, but this middleware has no branch for it. It is returned as `500 server_error`.

## Response format

Every error response has:
- Status code from the table above.
- `Content-Type: application/json`. No charset is set.
- Body: a JSON object with two properties, serialized with the default `JsonSerializer` settings:

```json
{"error":"invalid_grant","error_description":"The authorization code has expired."}
```

For `invalid_client`, the response also includes `WWW-Authenticate: Basic`, as RFC 6749 §5.2 requires.

## Usage

### Registering the middleware

```csharp
var app = builder.Build();

app.UseMiddleware<AuthExceptionHandlingMiddleware>();
app.MapControllers();
```

Place it before the endpoints or controllers that throw `AuthServerException`. Within the pipeline, it only sees exceptions thrown by middleware that runs after it.

### Throwing an error that is returned to the client

```csharp
throw new InvalidGrantException("The refresh token has been revoked.");
// -> 400 {"error":"invalid_grant","error_description":"The refresh token has been revoked."}
```

## Remarks

- **Responses that have already started.** If the response has already started when the exception is caught, setting `StatusCode` and headers throws `InvalidOperationException`. The middleware does not handle this case.
- **Exceptions outside the mapping.** Non-`AuthServerException` exceptions are not caught, so they go to the next outer exception handler, such as `ErrorHandlingMiddleware` in the default pipeline.
