# AuthorizationRequest

Represents an OAuth2/OIDC authorization request. This model captures the parameters of an authorization request as defined by the OAuth 2.0 and OpenID Connect specifications.

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `ClientId` | `string?` | Client identifier. |
| `ResponseType` | `string?` | Response type (`code`, `token`, `id_token`, `code id_token`, etc.). |
| `RedirectUri` | `string?` | Redirect URI where the authorization code/token will be sent. |
| `Scope` | `string?` | Requested scopes (space-separated). |
| `State` | `string?` | CSRF token for state verification. |
| `Nonce` | `string?` | Nonce for ID token validation (OIDC). |
| `CodeChallenge` | `string?` | PKCE code challenge. |
| `CodeChallengeMethod` | `string?` | PKCE code challenge method (`plain` or `S256`). |
| `Display` | `string?` | Display parameter (`page`, `popup`, `touch`, `wap`). |
| `Prompt` | `string?` | Prompt parameter (`none`, `login`, `consent`, `select_account`). |
| `MaxAge` | `int?` | Maximum age of authentication (in seconds). |
| `UiLocales` | `string?` | Preferred UI locale. |
| `AcrValues` | `string?` | Preferred language for messages. |
| `LoginHint` | `string?` | Hint about the user's identity (email, login, etc.). |
| `IdTokenHint` | `string?` | Hint about the user's preferred identity provider. |
| `CustomParameters` | `Dictionary<string, string>` | Additional custom parameters. Defaults to an empty dictionary. |

## Methods

### IsValid()

Validates that the authorization request contains the required parameters.

Returns `true` when all of the following are non-empty (not whitespace):
- `ClientId`
- `ResponseType`
- `RedirectUri`
- `Scope`

### GetRequestedScopes()

Gets the requested scopes as a list.

Splits the `Scope` value on spaces, removing empty entries. Returns an empty sequence when `Scope` is null or whitespace.

### HasPkce()

Checks whether PKCE is requested.

Returns `true` when `CodeChallenge` is non-empty (not whitespace).

### IsOpenIdRequest()

Checks whether this is an OpenID Connect request.

Returns `true` when the requested scopes contain the `openid` scope (case-insensitive comparison).

### ToString()

Returns a string representation of the request, including `ClientId`, `ResponseType`, `RedirectUri`, `Scope`, `State`, and `Nonce`.

## Remarks

- All properties are nullable except `CustomParameters`, which defaults to an empty dictionary.
- The `IsValid()` method is the primary gate for accepting an authorization request; callers should reject requests that fail validation before proceeding with the authorization flow.