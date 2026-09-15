# InvalidScopeException

`InvalidScopeException` represents an OAuth 2.0 request whose requested scope is invalid, unknown, malformed, or not allowed for the client. It is a sealed specialization of `AuthServerException`.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `invalid_scope` |
| HTTP status code | `400 Bad Request` |
| Default message | `The requested scope is invalid, unknown, or malformed` |
| Error URI | `null` |

`AuthExceptionHandlingMiddleware` serializes the exception as an `invalid_scope` response with status code 400. The exception message is used as the response's error description.

## Constructor

```csharp
public InvalidScopeException(
    string message = "The requested scope is invalid, unknown, or malformed",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message. It must not be `null` or empty.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message` on the exception.
- `innerException` optionally preserves the exception that caused the scope-validation failure.

The constructor throws `ArgumentNullException` for a `null` message and `ArgumentException` for an empty message.

## When it is thrown

The server throws `InvalidScopeException` when:

- An authorization request includes a scope that the authorization server does not support.
- A client requests one or more scopes that are not included in its allowed scopes.

## Example

```csharp
if (!supportedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
    throw new InvalidScopeException($"Scope '{scope}' is not supported");
```

This produces an OAuth 2.0 error response equivalent to:

```json
{
  "error": "invalid_scope",
  "error_description": "Scope 'admin' is not supported"
}
```
