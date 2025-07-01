# AuthorizationController

The `AuthorizationController` handles OpenID Connect authorization flows, including initiation of authorization requests, presentation of consent prompts to users, and processing of consent decisions. It integrates with the authorization service to validate requests, retrieve client and user information, and issue authorization codes or tokens.

## API

### `AuthorizationController`
Public controller exposing endpoints for OpenID Connect authorization and consent flows. This controller is registered with the dependency injection container and relies on injected services for authorization logic, user and client repositories, and session management.

### `public async Task<IActionResult> AuthorizeAsync`
Initiates the authorization flow by validating the authorization request parameters, checking user authentication state, and either proceeding to consent or directly issuing an authorization code or token based on client configuration and user consent status.

- **Parameters**: None (parameters are bound from the HTTP request via `[FromQuery]` attributes).
- **Return value**: `Task<IActionResult>` representing the result of the authorization flow. Returns an `OkObjectResult` with an authorization code or token if the request is valid and consent is not required; otherwise, returns a `ChallengeResult` or `RedirectResult` to prompt for consent.
- **Exceptions**: Throws `InvalidOperationException` if required services or repositories are not available. Throws `ArgumentException` if required request parameters are missing or invalid.

### `public async Task<IActionResult> GetConsentPromptAsync`
Renders a consent prompt UI for the user when consent is required for the requested scopes or resources. Validates the authorization request and prepares the consent context for rendering.

- **Parameters**: None (parameters are bound from the HTTP request via `[FromQuery]` attributes).
- **Return value**: `Task<IActionResult>` representing the consent prompt view. Returns a `ViewResult` with the consent model populated from the authorization request and user context.
- **Exceptions**: Throws `InvalidOperationException` if the authorization request cannot be resolved from the session or request context.

### `public async Task<IActionResult> SubmitConsentAsync`
Processes the user’s consent decision, updates authorization state accordingly, and resumes the authorization flow by either issuing an authorization code or token or redirecting back to the client with an error.

- **Parameters**: None (parameters are bound from the form submission via `[FromForm]` attributes).
- **Return value**: `Task<IActionResult>` representing the result of consent processing. Returns a `RedirectResult` to the client redirect URI with an authorization code or error; or a `BadRequestResult` if consent data is invalid.
- **Exceptions**: Throws `InvalidOperationException` if the consent context is missing or tampered with. Throws `ArgumentException` if required consent parameters are missing or invalid.

## Usage
