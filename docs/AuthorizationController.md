# AuthorizationController

`AuthorizationController` serves the OAuth 2.0 / OpenID Connect authorization endpoint, `GET /oauth/authorize`. It binds the query string to an `AuthorizationRequest`, asks `AuthorizationService` to validate it, and returns the validated details as JSON. In its current form it does not show a login or consent screen, issue an authorization code, or redirect the user agent on success. The source marks this as a placeholder ("In a real implementation, this would redirect to login and consent screens").

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/AuthorizationController.cs`, namespace `DotnetAuthServer.Controllers`. It is attribute-routed with `[ApiController]` and `[Route("oauth/authorize")]`. The controller also contains two consent actions (`GET` and `POST` on `oauth/authorize/consent`). They are not part of the authorization request flow and are not covered here.

## Role in the system

- Accepts the browser's authorization request on `GET /oauth/authorize`.
- Maps the query parameters onto an `AuthorizationRequest` domain model.
- Delegates all protocol checks to `AuthorizationService.ValidateAuthorizationRequestAsync`.
- Converts `AuthServerException` failures into either an OAuth error redirect or an OAuth error JSON response.

The controller holds no protocol logic of its own. Validation rules live in `AuthorizationService`.

## Constructor

### `public AuthorizationController(AuthorizationService authorizationService, ConsentService consentService, IUserRepository userRepository, ILogger<AuthorizationController> logger)`

Creates the controller. The ASP.NET Core container injects the parameters.

**Parameters:**
- `authorizationService` – validates authorization requests. Used by `GET /oauth/authorize`.
- `consentService` – used only by the consent actions.
- `userRepository` – used only by the consent actions.
- `logger` – logs validation warnings and unexpected errors.

The GET action uses `authorizationService`, `logger`, and `HttpContext`. `consentService` and `userRepository` are not touched by it.

## Endpoint

### `GET /oauth/authorize`

`public async Task<IActionResult> AuthorizeAsync(...)`

Handles an authorization request. All inputs are query-string parameters bound with `[FromQuery]`. The action takes a `CancellationToken` that ASP.NET Core supplies from the request.

#### Query parameters

| Parameter | Type | Use |
|---|---|---|
| `client_id` | `string?` | Client identifier. Required. |
| `response_type` | `string?` | Required. Must be one of `code`, `token`, `id_token`, `code id_token`, `code token`, `id_token token`, `code id_token token` (case-insensitive), and allowed for the client. |
| `redirect_uri` | `string?` | Required. Must exactly match one of the client's registered redirect URIs (case-insensitive). |
| `scope` | `string?` | Required (non-empty). Space-separated. Each scope must appear in `AuthServerOptions.SupportedScopes` (case-insensitive). Maximum 500 characters. |
| `state` | `string?` | Opaque value from the client. Maximum 500 characters. Echoed only in error redirects (see [Error responses](#error-responses)). |
| `nonce` | `string?` | Bound to `AuthorizationRequest.Nonce`. Not checked and not returned. |
| `code_challenge` | `string?` | PKCE challenge. When present, must be 43–128 characters from `[A-Za-z0-9-._~]`. |
| `code_challenge_method` | `string?` | `plain` is rejected. Other values are not checked. |
| `display` | `string?` | Bound. Not used. |
| `prompt` | `string?` | Bound. Not used. |
| `max_age` | `int?` | Bound. Not used. A non-integer value fails model binding before the action runs (see [Binding failures](#binding-failures)). |
| `login_hint` | `string?` | Bound. Not used. |

#### Validation order

`AuthorizationService.ValidateAuthorizationRequestAsync` runs these checks in order and stops at the first failure:

| # | Check | Error code | HTTP status |
|---|---|---|---|
| 1 | `client_id`, `response_type`, `redirect_uri`, and `scope` are all non-empty | `invalid_request` | 400 |
| 2 | Client exists and is active | `invalid_client` | 401 |
| 3 | `response_type` is one of the supported values | `unsupported_response_type` | 400 |
| 4 | `redirect_uri` is registered for the client | `invalid_request` | 400 |
| 5 | `scope` is at most 500 characters | `invalid_request` | 400 |
| 6 | `state` is at most 500 characters | `invalid_request` | 400 |
| 7 | Every requested scope is in `SupportedScopes` | `invalid_scope` | 400 |
| 8 | The client is allowed to use `response_type` | `unauthorized_client` | 403 |
| 9 | `code_challenge_method` is not `plain` | `invalid_request` | 400 |
| 10 | If `code_challenge` is present, it has a valid format | `invalid_request` | 400 |
| 11 | Public clients (`IsConfidential == false`) send a `code_challenge` | `invalid_request` | 400 |
| 12 | With `RequirePkceForAllClients`, clients that have `RequirePkce` send a `code_challenge` | `invalid_request` | 400 |

Check 1 reports `"Authorization request is missing required parameters"`. Check 2 reports `"Client not found or inactive"`. Check 4 reports `"The redirect_uri is not registered"`. The other messages are in the source.

#### Success response

When validation passes, the action returns `200 OK` with a JSON body. The property names are camelCase.

```json
{
  "message": "Authorization request validated",
  "clientId": "web-app",
  "redirectUri": "https://app.example.com/callback",
  "requestedScopes": ["openid", "profile"],
  "requiresPkce": true
}
```

- `requestedScopes` – the `scope` value split on spaces.
- `requiresPkce` – `true` if a non-empty `code_challenge` was supplied. It does not report whether PKCE is required for the client.

No authorization code is issued, and no redirect happens.

#### Error responses

Errors take one of three paths, depending on the exception and on whether `redirect_uri` is present in the query.

**1. `AuthServerException` with a non-empty `redirect_uri` query parameter.** The action returns a `302 Found` redirect to:

```
{redirect_uri}{?|&}error={error}&error_description={escaped description}[&state={state}]
```

- The separator is `&` if `redirect_uri` already contains `?`, and `?` otherwise.
- `error_description` is the exception's `ErrorDescription`, or its `Message` if that is null, URL-escaped.
- `error` and `state` are appended without encoding (see [Known issues](#known-issues)).
- `redirect_uri` and `state` come from the raw query string, not from the validated request.

**2. `AuthServerException` without `redirect_uri`.** The action returns the exception's `StatusCode` with the OAuth JSON body from `ToErrorResponse()`:

```json
{
  "error": "invalid_request",
  "error_description": "Authorization request is missing required parameters"
}
```

**3. Any other exception.** The action logs the exception and returns `500 Internal Server Error`:

```json
{
  "error": "server_error",
  "error_description": "An unexpected error occurred"
}
```

#### Binding failures

Because the class has `[ApiController]`, model binding errors are handled before the action runs. For example, `max_age=abc` produces a `400 Bad Request` with an RFC 7807 `ValidationProblemDetails` body. That body does not use the `error` / `error_description` shape. No `ApiBehaviorOptions` override is configured in `Program.cs`, so this is the default behavior.

## Examples

Valid request with PKCE:

```http
GET /oauth/authorize?client_id=web-app&response_type=code&redirect_uri=https%3A%2F%2Fapp.example.com%2Fcallback&scope=openid%20profile&state=xyz&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM&code_challenge_method=S256
```

Response: `200 OK` with the success body shown above.

Request without a `code_challenge` for a public client:

```http
GET /oauth/authorize?client_id=spa-client&response_type=code&redirect_uri=https%3A%2F%2Fspa.example.com%2Fcb&scope=openid&state=xyz
```

Response: `302 Found`, redirecting to `https://spa.example.com/cb?error=invalid_request&error_description=PKCE%20is%20required%20for%20public%20clients&state=xyz`.

## Known issues

These behaviors come from reading the source. They have not been changed or tested here.

1. **Error redirects go to an unvalidated `redirect_uri`.** Path 1 redirects to the query's `redirect_uri` for every `AuthServerException`, including failures from checks 2 and 4 that happen before the URI is checked against the client. A request with an unknown `client_id`, or with a `redirect_uri` that is not registered, gets a `302` to that attacker-chosen URI carrying `error` parameters. OAuth 2.1 requires redirecting only to a registered URI.
2. **Unencoded values in the error redirect.** `error` and `state` are concatenated into the URL without escaping. A `state` value that contains `&` or `#` (for example, `a%26error%3Dx` after decoding becomes `a&error=x`) adds or changes parameters in the redirect.
3. **`code_challenge_method` is not restricted to `S256`.** Only `plain` is rejected. Any other value, such as `foo`, passes validation when a valid `code_challenge` is present.
4. **Success response is not an OAuth response.** A valid request returns `200` JSON instead of redirecting with a code, so the endpoint cannot yet complete the authorization code flow.

## Related

- [AuthorizationService](AuthorizationService.md) – validation and grant creation.
- [PkceValidationService](PkceValidationService.md) – PKCE generation and verification.
- [consent](consent.md) – consent behavior.
