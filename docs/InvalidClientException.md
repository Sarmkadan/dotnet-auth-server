# InvalidClientException

`InvalidClientException` represents a failure to identify or authenticate an OAuth 2.0 client. It is a sealed specialization of `AuthServerException`.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `invalid_client` |
| HTTP status code | `401 Unauthorized` |
| Default message | `Client authentication failed` |
| Error URI | `null` |

`AuthExceptionHandlingMiddleware` serializes the exception as an `invalid_client` response with status code 401 and adds a `WWW-Authenticate` response header. The exception message is used as the response's error description unless a separate `errorDescription` is supplied.

## Constructor

```csharp
public InvalidClientException(
    string message = "Client authentication failed",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message. It must not be `null` or empty.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message`.
- `innerException` optionally preserves the exception that caused the client-authentication failure.

The constructor throws `ArgumentNullException` for a `null` message and `ArgumentException` for an empty message.

## When it is thrown

The server throws `InvalidClientException` in these situations:

- A required `client_id` is missing from an authorization, token, or consent request.
- A client cannot be found, or an active client is required and the client is inactive.
- A confidential client does not provide a client secret.
- A supplied client secret or client credential is invalid.
- Client redirect URI validation fails because `redirect_uri` is missing, is not an absolute URI, or is not registered for the client.
- Scope or grant-type validation cannot load the referenced client.
- An authorization or consent operation cannot load the referenced client.

The exception identifies client authentication or lookup failures. Other authorization failures use more specific errors; for example, a known client that is not permitted to use a requested grant type produces `unauthorized_client`, and disallowed scopes produce `invalid_scope`.

## Example

```csharp
if (client is null || !client.IsActive)
    throw new InvalidClientException("Client not found or inactive");
```

This produces an OAuth 2.0 error response equivalent to:

```json
{
  "error": "invalid_client",
  "error_description": "Client not found or inactive"
}
```
