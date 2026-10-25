# ClientRegistrationController

`ClientRegistrationController` serves the RFC 7591 Dynamic Client Registration endpoint, `POST /register`. It reads a JSON client-metadata document, asks `DynamicClientRegistrationService` to validate and store a new client, and returns the registered metadata. For confidential clients the response also carries a freshly generated `client_secret`, which is shown only in this response.

The class is `public sealed`, derives from `ControllerBase`, and lives in `src/Controllers/ClientRegistrationController.cs`, namespace `DotnetAuthServer.Controllers`. It is attribute-routed with `[ApiController]` and `[Route("register")]`. The controller has one action, so this document covers the whole public surface.

## Role in the system

- Accepts a client-registration request on `POST /register`.
- Delegates validation, client creation, and secret generation to `DynamicClientRegistrationService.RegisterAsync`.
- Maps `AuthServerException` failures to OAuth error JSON with the status code the exception carries.
- Maps any other failure to a generic `500 server_error`.

The controller holds no registration logic. Rules for grant types, auth methods, redirect URIs, and scopes live in `DynamicClientRegistrationService`.

The discovery documents in `Program.cs` advertise this endpoint as `registration_endpoint` (`{IssuerUrl}/register`).

## Constructor

### `public ClientRegistrationController(DynamicClientRegistrationService registrationService, ILogger<ClientRegistrationController> logger)`

Creates the controller. The ASP.NET Core container injects the parameters.

**Parameters:**
- `registrationService` – validates the request, creates the client, and returns the response.
- `logger` – logs registration failures (warning), the client-ID collision case (error), and unhandled exceptions (error).

## Endpoint

### `POST /register`

`public async Task<IActionResult> RegisterClientAsync(ClientRegistrationRequest request, CancellationToken cancellationToken)`

Registers a new OAuth 2.0 client.

- **Request content type:** `application/json` (`[Consumes]`). Other content types are rejected by the framework.
- **Response content type:** `application/json` (`[Produces]`).
- The body is bound with `[FromBody]` to `ClientRegistrationRequest`. The `CancellationToken` comes from the request.

#### Request body

Field names follow RFC 7591 §2. Property names are the JSON names shown below.

| Field | Type | Default | Use |
|---|---|---|---|
| `client_name` | `string` | none | Required. Must not be empty or whitespace. |
| `grant_types` | `string[]` | `["authorization_code"]` | Each value must be one of `authorization_code`, `client_credentials`, `implicit`, `refresh_token` (case-insensitive). |
| `redirect_uris` | `string[]` | `[]` | Each value must be an absolute URI. Required (at least one) when `grant_types` contains `authorization_code` or `implicit`. |
| `response_types` | `string[]` | `["code"]` | Stored as sent and echoed in the response. Not checked against `grant_types`. |
| `scope` | `string` | none | Space-delimited. See [Scope handling](#scope-handling). |
| `token_endpoint_auth_method` | `string` | `client_secret_basic` | One of `none`, `client_secret_basic`, `client_secret_post` (case-insensitive). `none` makes a public client. |
| `logo_uri` | `string` | none | Stored and echoed. Not checked. |
| `policy_uri` | `string` | none | Stored and echoed. Not checked. |
| `tos_uri` | `string` | none | Stored (as the client's terms-of-service URI) and echoed. Not checked. |
| `contacts` | `string[]` | `[]` | Stored and echoed. Not checked. |
| `client_uri` | `string` | none | Bound to the request model but not used by the service. Not stored and not echoed. |

Unknown JSON properties are not part of the model. The default `System.Text.Json` behaviour ignores them.

#### Successful response

**Status:** `201 Created`

The action returns `CreatedAtAction(actionName: null, value: response)`. The source does not set a `Location` header explicitly.

**Body:** `ClientRegistrationResponse`.

| Field | Present | Value |
|---|---|---|
| `client_id` | always | 32-character lowercase hex string (`Guid` with the `N` format). |
| `client_secret` | confidential clients only | 43-character Base64url string from 32 random bytes. Returned once and not retrievable later. |
| `client_secret_expires_at` | confidential clients only | `0` (no expiry). |
| `client_id_issued_at` | always | Unix time in seconds, UTC, at registration. |
| `client_name` | always | The registered name. |
| `grant_types` | always | The registered grant types, as sent. |
| `redirect_uris` | always | The registered redirect URIs, as sent. |
| `response_types` | always | The response types, as sent. |
| `scope` | only when at least one scope was kept | Space-delimited list of the kept scopes. |
| `token_endpoint_auth_method` | always | The value as sent in the request (or the default). |
| `logo_uri`, `policy_uri`, `tos_uri` | only when set | As sent. |
| `contacts` | only when non-empty | As sent. |

A confidential client is any client whose `token_endpoint_auth_method` is not `none`. The comparison is case-insensitive.

Example request (values are placeholders):

```http
POST /register HTTP/1.1
Content-Type: application/json

{
  "client_name": "Example SPA",
  "grant_types": ["authorization_code", "refresh_token"],
  "redirect_uris": ["https://app.example.com/callback"],
  "scope": "openid profile",
  "token_endpoint_auth_method": "none"
}
```

Example response (abbreviated; `client_id` and `client_id_issued_at` will differ):

```json
{
  "client_id": "3f2c9d1e8a7b4c60b5d4e3f2a1b0c9d8",
  "client_id_issued_at": 1792944000,
  "client_name": "Example SPA",
  "grant_types": ["authorization_code", "refresh_token"],
  "redirect_uris": ["https://app.example.com/callback"],
  "response_types": ["code"],
  "scope": "openid profile",
  "token_endpoint_auth_method": "none"
}
```

A public client receives no `client_secret` and no `client_secret_expires_at`.

## Scope handling

`DynamicClientRegistrationService` applies these rules to `scope`:

- If `scope` is empty or absent, the client is given every scope in `AuthServerOptions.SupportedScopes`.
- If `scope` lists scopes, the client is given only those that appear in `SupportedScopes` (case-insensitive). Unknown scopes are dropped silently. The response does not say which were dropped.
- If `scope` lists only unknown scopes, the client gets no scopes and the response omits `scope`.

## Confidentiality and PKCE

These settings are applied to the stored client. They are not returned in the response.

| Client type | `IsConfidential` | `RequirePkce` | `RequireConsent` |
|---|---|---|---|
| Public (`token_endpoint_auth_method: none`) | `false` | always `true` | `AuthServerOptions.RequireUserConsent` |
| Confidential (any other method) | `true` | `AuthServerOptions.RequirePkceForAllClients` | `AuthServerOptions.RequireUserConsent` |

For confidential clients the secret is stored as an unsalted SHA-256 hash, Base64-encoded. The plain-text secret is not stored. This differs from the PBKDF2 scheme in `SecretsService`.

## Errors

Every error response is JSON with `error` and `error_description`, the OAuth shape produced by `AuthServerException.ToErrorResponse()`. The controller returns the exception's own status code. The `error_uri` and `Details` fields are included only when the exception sets them. Registration errors do not set them.

### Validation errors (`400 Bad Request`)

The service throws `AuthServerException` with `invalid_request` for each of these. The first failing check stops validation.

| Check (in order) | `error_description` |
|---|---|
| `client_name` missing, or `redirect_uris` missing for `authorization_code` / `implicit` | `client_name is required and redirect_uris must be provided for authorization_code / implicit grant types.` |
| A `grant_types` value not in the supported list | `Grant type '<value>' is not supported by this server.` |
| `token_endpoint_auth_method` not in the supported list | `token_endpoint_auth_method '<value>' is not supported.` |
| A `redirect_uris` value that is not an absolute URI | `redirect_uri '<value>' is not a valid absolute URI.` |

Example:

```json
{
  "error": "invalid_request",
  "error_description": "Grant type 'password' is not supported by this server."
}
```

### Server errors (`500 Internal Server Error`)

| Condition | Body |
|---|---|
| `InvalidOperationException` whose message contains `already exists` (a generated `client_id` collided) | `{"error": "server_error", "error_description": "Failed to generate unique client identifier; please retry."}` |
| Any other exception | `{"error": "server_error", "error_description": "An unexpected error occurred."}` |

The 500 bodies are fixed. They do not include exception details. The exception is logged with its stack trace.

### Malformed requests

A body that is not valid JSON, or a missing body, is rejected by ASP.NET Core model binding under `[ApiController]` before the action runs. That response uses the framework's default format, not the OAuth error shape. This document does not cover that format.

## Logging

| Event | Level | Content |
|---|---|---|
| `AuthServerException` (validation failure) | Warning | `ErrorCode` and message. |
| Client-ID collision | Error | Exception with stack trace. |
| Unhandled exception | Error | Exception with stack trace. |
| Successful registration (logged by the service) | Information | Client ID and whether it is confidential. The secret is never logged. |

## Limitations

- Registration is open. The endpoint has no authentication and no initial access token. The controller applies no rate limit of its own. Whether `RateLimitingMiddleware` covers this route was not checked for this document.
- Clients are stored in memory only (see `docs/ARCHITECTURE.md`). Restarting the server removes all registered clients.
- The endpoint does not implement RFC 7592 (client configuration management). Clients cannot read, update, or delete their registration through this API.
- `client_uri` is accepted but ignored.
- `response_types` is stored without checking it against `grant_types`.
- `logo_uri`, `policy_uri`, `tos_uri`, and `contacts` are stored without format checks beyond what is noted above.
- Confidential-client secrets are hashed with unsalted SHA-256, which is weaker than the PBKDF2 scheme used elsewhere.

## Related

- `DynamicClientRegistrationService` – registration logic (`docs/DynamicClientRegistrationService.md`).
- `ClientRegistrationRequest` and `ClientRegistrationResponse` – request and response models (`docs/ClientRegistrationRequest.md`, `docs/ClientRegistrationResponse.md`).
- `AuthServerException` – source of the error responses (`docs/AuthServerException.md`).
- `docs/ARCHITECTURE.md` – design and known limitations.
