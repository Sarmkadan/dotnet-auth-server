# InvalidGrantException

`InvalidGrantException` represents an OAuth 2.0 grant that cannot be accepted. It is a sealed specialization of `AuthServerException` and can be used for invalid, expired, revoked, or mismatched grants.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `invalid_grant` |
| HTTP status code | `400 Bad Request` |
| Default message | `The provided grant is invalid, expired, revoked, or does not match the redirect URI` |
| Error URI | `null` |

`AuthExceptionHandlingMiddleware` serializes the exception as an `invalid_grant` response with status code 400. The exception message is used as the response description by the middleware.

## Constructor

```csharp
public InvalidGrantException(
    string message = "The provided grant is invalid, expired, revoked, or does not match the redirect URI",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message. It must not be `null` or empty.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message` on the exception.
- `innerException` optionally preserves the exception that caused the grant failure.

The constructor throws `ArgumentNullException` for a `null` message and `ArgumentException` for an empty message.

## When it is thrown

The server throws `InvalidGrantException` when:

- An authorization code or its required redirect URI is missing.
- An authorization code is invalid or expired.
- The supplied redirect URI does not match the authorization grant.
- A refresh token is invalid, expired, or otherwise cannot be used.
- A WebAuthn registration or authentication challenge is missing, expired, or does not match.
- WebAuthn credential, authenticator-data, origin, relying-party, algorithm, or signature validation fails.

## Example

```csharp
if (authorizationGrant is null)
    throw new InvalidGrantException("Authorization code is invalid or expired");
```

This produces an OAuth 2.0 error response equivalent to:

```json
{
  "error": "invalid_grant",
  "error_description": "Authorization code is invalid or expired"
}
```
