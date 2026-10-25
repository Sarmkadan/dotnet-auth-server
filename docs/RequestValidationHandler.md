# RequestValidationHandler

Sealed handler that checks OAuth 2.0 / OpenID Connect requests before the rest of the pipeline processes them. The `Validate*` methods return `void` and throw on the first failed rule. The `Is*` methods return `bool` and never throw. The handler is stateless apart from its logger, and it is registered as a scoped service in `Program.cs`.

The class is `public sealed` in namespace `DotnetAuthServer.Handlers` (`src/Handlers/RequestValidationHandler.cs`). Validation failures are logged at `Warning`, and successful authorization-request validation is logged at `Information`.

## Behavior notes

- **Exception types.** Rule failures throw `AuthServerException` with error code `invalid_request` and status 400, except where noted. `ErrorHandlingMiddleware` turns these into OAuth `{"error","error_description"}` responses. Two derived types are used for client-level failures:
  - `InvalidClientException`: `invalid_client`, status 401.
  - `UnauthorizedClientException`: `unauthorized_client`, status 403.
- **Argument checks.** `null` arguments throw `ArgumentNullException`. Empty or whitespace-only strings are treated as missing values and produce the OAuth error above, not an `ArgumentException`. The exception is `ValidateResponseTypeAllowed`, which uses `ArgumentException.ThrowIfNullOrEmpty` on `responseType` (see below).
- **Length limits.** These are private constants that protect against oversized input. They are not configurable:

  | Parameter | Maximum length |
  |---|---|
  | `scope` | 500 |
  | `state` | 500 |
  | `redirect_uri` | 2000 |
  | `nonce` | 500 |
  | `login_hint` | 1000 |

- **Redirect URI matching** is exact string matching through `Client.IsRedirectUriValid`, which blocks prefix and subdomain tricks.
- **Non-HTTPS requests** are logged as warnings by `ValidateHttpRequest` and are not rejected. Only body size is enforced there.
- **`Is*` helpers** compare values case-insensitively where noted, but they do not normalize input. The caller's value is checked as given.

## API

### Constructor

#### `RequestValidationHandler(ILogger<RequestValidationHandler> logger)`
Creates the handler. Throws `ArgumentNullException` if `logger` is null.

### Methods

#### `void ValidateAuthorizationRequest(AuthorizationRequest request, Client client)`
Runs the full authorization-request check. Rules are evaluated in this order:

1. `request` and `client` must not be null (`ArgumentNullException`).
2. `client_id` is required. Failure throws `InvalidClientException` ("client_id is required").
3. `response_type` is required (`invalid_request`).
4. `redirect_uri` is required (`invalid_request`).
5. Length limits apply to `scope`, `state`, `redirect_uri`, `nonce`, and `login_hint` (`invalid_request`).
6. `redirect_uri` must exactly match a URI registered on the client (`invalid_request`, "The redirect_uri is not registered").
7. `response_type` must be allowed for the client via `client.IsGrantTypeAllowed` (`UnauthorizedClientException`).
8. PKCE rules from `ValidatePkceRequirements` are applied.

Returns normally when every rule passes.

#### `void ValidatePkceRequirements(AuthorizationRequest request, Client client)`
Enforces PKCE rules for an authorization request:

- `code_challenge_method=plain` is always rejected, including for confidential clients (`invalid_request`).
- A public client (`IsConfidential == false`) must send a code challenge. `HasPkce()` checks this, and a missing challenge fails with `invalid_request`.
- If a challenge is present, `IsValidCodeChallenge` must accept it (`invalid_request`, "Invalid code_challenge format").

Throws `ArgumentNullException` if `request` or `client` is null.

#### `void ValidateResponseTypeAllowed(string responseType, ICollection<string> allowedGrantTypes)`
Checks `responseType` against a caller-supplied collection of allowed values.

- `responseType` is lowercased with `ToLowerInvariant()` and then compared to `allowedGrantTypes` with `StringComparer.OrdinalIgnoreCase`.
- The comparison is per element. A space-separated response type such as `"code id_token"` is not split and matches only if the collection contains that exact string.
- Throws `ArgumentNullException` when `responseType` is null, and `ArgumentException` when it is empty (`ThrowIfNullOrEmpty`).
- Throws `ArgumentNullException` when `allowedGrantTypes` is null.
- Throws `ArgumentException` ("Allowed grant types cannot be empty") when the collection has no items.
- Throws `UnauthorizedClientException` when the value is not in the collection.

#### `bool IsValidCodeChallenge(string? codeChallenge)`
Returns `true` when the value is 43 to 128 characters long and every character is a letter or digit (`char.IsLetterOrDigit`, which includes non-ASCII letters) or one of `-`, `.`, `_`, `~`. Returns `false` for null, empty, or whitespace-only input. The method checks format only and does not verify the code verifier.

#### `void ValidateTokenRequest(TokenRequest request)`
Checks a token request.

- `grant_type` is required (`invalid_request`, "grant_type is required").
- `client_id` is required (`InvalidClientException`).
- Throws `ArgumentNullException` if `request` is null.

On success, a `Debug` log entry is written. The method does not check that `grant_type` is a known value. Use `IsValidGrantType` for that.

#### `void ValidateConsentRequest(ConsentRequest request)`
Checks a consent request.

- `client_id` is required (`InvalidClientException`).
- `user_id` is required (`invalid_request`, "user_id is required for consent").
- Throws `ArgumentNullException` if `request` is null.

#### `void ValidateHttpRequest(HttpRequest httpRequest)`
Checks transport-level properties of an ASP.NET Core `HttpRequest`.

- Non-HTTPS requests are logged as warnings. They are not rejected.
- When `ContentLength` is greater than 102,400 bytes (100 KB), throws `AuthServerException` with `invalid_request` and status 413 ("Request body is too large"). A request with no declared length, such as a chunked request, passes this check.
- Throws `ArgumentNullException` if `httpRequest` is null.

#### `bool IsValidResponseType(string? responseType)`
Returns `true` only when the value exactly matches one of these entries, compared case-insensitively:

`code`, `token`, `id_token`, `code token`, `code id_token`, `token id_token`, `code token id_token`

Word order matters: `"token code"` returns `false`. Returns `false` for null, empty, or whitespace-only input.

#### `bool IsValidGrantType(string? grantType)`
Returns `true` only when the value exactly matches one of these grant types, compared case-insensitively:

- `authorization_code`
- `refresh_token`
- `client_credentials`
- `password`
- `urn:ietf:params:oauth:grant-type:device_flow`
- `urn:ietf:params:oauth:grant-type:jwt-bearer`

Returns `false` for null, empty, or whitespace-only input.

#### `bool IsValidScope(string? scope)`
Returns `true` when every space-separated token in `scope` contains only letters, digits, or `_`. Returns `true` for null, empty, or whitespace-only input, because an absent scope is allowed. Only the space character separates tokens, so a tab inside a token makes the scope invalid. Scope values containing `:`, `-`, or `.` (for example `read:user`) return `false`.
