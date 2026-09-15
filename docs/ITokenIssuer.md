# ITokenIssuer

Namespace: `DotnetAuthServer.Services`

`ITokenIssuer` defines the token-issuance operations used by the OAuth 2.0 token endpoint. It accepts a `TokenRequest`, produces a `TokenResponse`, and exposes the client-secret hashing and validation operations used when authenticating confidential clients.

The built-in implementation is `TokenIssuer`.

## API

### `Task<TokenResponse> HandleTokenRequestAsync(TokenRequest request, CancellationToken cancellationToken = default)`

Validates and handles an OAuth 2.0 token request. The built-in implementation supports the `authorization_code`, `refresh_token`, `client_credentials`, and `password` grant types.

#### Parameters

- `request`: The request to validate and process. Its required fields depend on the selected grant type.
- `cancellationToken`: An optional token used to cancel asynchronous repository operations and request processing.

#### Returns

A task that resolves to a `TokenResponse` containing the issued access token and its metadata. Depending on the grant type, the response can also contain a refresh token and granted scopes.

#### Exceptions

- `ArgumentNullException`: `request` is `null`.
- `AuthServerException`: The request is invalid, the grant type is unsupported, or token issuance fails. More specific authentication exceptions derived from `AuthServerException`, such as `InvalidClientException` and `InvalidGrantException`, can be raised by the built-in implementation.
- `OperationCanceledException`: Processing is cancelled through `cancellationToken`.

### `bool ValidateClientSecret(Client client, string? providedSecret)`

Checks a supplied plaintext secret against a client's stored secret hash.

For the built-in `TokenIssuer`, public clients are accepted without a secret. Confidential clients require a nonblank secret; the supplied value is hashed with `HashClientSecret` and compared with `Client.ClientSecretHash` using an ordinal comparison. Hashing or comparison failures are logged and return `false`.

#### Parameters

- `client`: The client whose confidentiality setting and stored hash are used for validation.
- `providedSecret`: The plaintext secret supplied by the client. This value may be `null`, but a null, empty, or whitespace value is invalid for a confidential client.

#### Returns

`true` when the client is public or the supplied secret matches the stored hash; otherwise, `false`.

### `string HashClientSecret(string secret)`

Computes a SHA-256 hash of a client secret and encodes the resulting bytes as a Base64 string. The returned value is suitable for comparison with `Client.ClientSecretHash` as used by the built-in implementation.

#### Parameters

- `secret`: The plaintext client secret to hash.

#### Returns

The Base64-encoded SHA-256 hash of `secret`.

#### Exceptions

- `ArgumentNullException`: `secret` is `null`.

## Usage

The token endpoint depends on `ITokenIssuer`, allowing token issuance to be replaced or decorated without coupling the controller to `TokenIssuer`.

```csharp
public sealed class TokenRequestHandler
{
    private readonly ITokenIssuer _tokenIssuer;

    public TokenRequestHandler(ITokenIssuer tokenIssuer)
    {
        _tokenIssuer = tokenIssuer;
    }

    public Task<TokenResponse> HandleAsync(
        TokenRequest request,
        CancellationToken cancellationToken = default)
    {
        return _tokenIssuer.HandleTokenRequestAsync(request, cancellationToken);
    }
}
```

Client secrets should be hashed before their hashes are persisted:

```csharp
client.ClientSecretHash = tokenIssuer.HashClientSecret(plaintextSecret);
```

Do not log or persist `plaintextSecret`. Store only its hash and protect access to that value as credential material.

## Related Types

- `TokenIssuer`: Default implementation of this interface.
- `TokenRequest`: Describes an OAuth 2.0 token request.
- `TokenResponse`: Contains an issued token and response metadata.
- `Client`: Contains client configuration, including confidentiality and the stored client-secret hash.
- `TokenController`: Uses this interface to process requests to the token endpoint.
