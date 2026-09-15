# UnauthorizedClientException

`UnauthorizedClientException` represents an OAuth 2.0 client that is known but is not permitted to use the requested grant type, response type, or operation. It is a sealed specialization of `AuthServerException`.

## Error details

| Property | Value |
|---|---|
| OAuth 2.0 error code | `unauthorized_client` |
| HTTP status code | `403 Forbidden` |
| Default message | `The client is not authorized to use this grant type` |
| Error URI | `null` |

The inherited `ToErrorResponse()` method produces an OAuth error object containing `error` and `error_description`. The description is the explicit `errorDescription` when one is supplied; otherwise, it defaults to `message`.

`AuthExceptionHandlingMiddleware` currently has no specific branch for `UnauthorizedClientException`. If the exception reaches that middleware, it is handled by the generic `AuthServerException` fallback and returned as a `500 server_error`, rather than using the exception's `403` status and `unauthorized_client` error code.

## Constructor

```csharp
public UnauthorizedClientException(
    string message = "The client is not authorized to use this grant type",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message`.
- `innerException` optionally preserves the exception that caused the authorization failure.

The constructor fixes the OAuth error code at `unauthorized_client`, the HTTP status code at `403`, and the error URI at `null`.

## When it is thrown

The server throws `UnauthorizedClientException` when:

- An authorization request uses a response type that is not allowed for the client.
- Response-type validation finds that the requested value is absent from the client's allowed grant types.
- Grant-type validation finds that the requested grant type is absent from the client's allowed grant types.

Use `InvalidClientException` instead when the client cannot be identified or authenticated. Use `UnauthorizedClientException` when the client is valid but lacks permission for the requested flow or operation.

## Example

```csharp
if (!client.AllowedGrantTypes.Contains(grantType))
{
    throw new UnauthorizedClientException(
        $"Client is not authorized to use grant type '{grantType}'");
}
```

Calling `ToErrorResponse()` on that exception produces an object equivalent to:

```json
{
  "error": "unauthorized_client",
  "error_description": "Client is not authorized to use grant type 'client_credentials'"
}
```
