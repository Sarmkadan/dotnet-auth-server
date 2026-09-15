# UnsupportedGrantTypeException

`UnsupportedGrantTypeException` represents an OAuth 2.0 request whose `grant_type` value is not supported by the authorization server. It is a sealed specialization of `AuthServerException`.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `unsupported_grant_type` |
| HTTP status code | `400 Bad Request` |
| Default message | `The requested grant type is not supported` |
| Error URI | `null` |

`AuthExceptionHandlingMiddleware` maps this exception to an `unsupported_grant_type` response with status code 400 and uses the exception message as the response description.

The inherited `ToErrorResponse()` method also produces an OAuth error object. Its `error_description` is the explicit `errorDescription` when one is supplied; otherwise, it defaults to `message`.

## Constructor

```csharp
public UnsupportedGrantTypeException(
    string message = "The requested grant type is not supported",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message. It must not be `null` or empty.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message` on the exception.
- `innerException` optionally preserves the exception that caused grant-type processing to fail.

The constructor fixes the OAuth error code at `unsupported_grant_type`, the HTTP status code at `400`, and the error URI at `null`. It throws `ArgumentNullException` for a `null` message and `ArgumentException` for an empty message.

## When to use it

Throw `UnsupportedGrantTypeException` when the authorization server does not recognize or implement the requested grant type. If the grant type is supported by the server but a particular client is not permitted to use it, use `UnauthorizedClientException` instead.

## Example

```csharp
if (!supportedGrantTypes.Contains(request.GrantType))
{
    throw new UnsupportedGrantTypeException(
        $"Grant type '{request.GrantType}' is not supported");
}
```

The middleware returns a response equivalent to:

```json
{
  "error": "unsupported_grant_type",
  "error_description": "Grant type 'custom_grant' is not supported"
}
```
