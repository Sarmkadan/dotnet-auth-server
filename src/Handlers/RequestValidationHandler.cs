// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Handler for comprehensive OAuth2 request validation.
/// Validates authorization requests, token requests, and other OAuth2 operations
/// for correctness and security before processing.
/// </summary>
public class RequestValidationHandler
{
    private readonly ILogger<RequestValidationHandler> _logger;

    // Maximum allowed request component sizes to prevent DOS attacks
    private const int MaxScopeLength = 500;
    private const int MaxStateLength = 500;
    private const int MaxRedirectUriLength = 2000;

    public RequestValidationHandler(ILogger<RequestValidationHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Validates an authorization request for structural integrity and required parameters.
    /// </summary>
    public void ValidateAuthorizationRequest(AuthorizationRequest request)
    {
        if (request == null)
            throw new AuthServerException(
                "invalid_request",
                "Authorization request is null",
                400);

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new InvalidClientException("client_id is required");

        if (string.IsNullOrWhiteSpace(request.ResponseType))
            throw new AuthServerException(
                "invalid_request",
                "response_type is required",
                400);

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
            throw new AuthServerException(
                "invalid_request",
                "redirect_uri is required",
                400);

        // Validate sizes to prevent DOS
        if ((request.Scope?.Length ?? 0) > MaxScopeLength)
            throw new AuthServerException(
                "invalid_request",
                "Scope exceeds maximum length",
                400);

        if ((request.State?.Length ?? 0) > MaxStateLength)
            throw new AuthServerException(
                "invalid_request",
                "State exceeds maximum length",
                400);

        if (request.RedirectUri.Length > MaxRedirectUriLength)
            throw new AuthServerException(
                "invalid_request",
                "Redirect URI exceeds maximum length",
                400);

        _logger.LogDebug(
            "Authorization request validation successful: client={ClientId} response_type={ResponseType}",
            request.ClientId,
            request.ResponseType);
    }

    /// <summary>
    /// Validates a token request for correctness.
    /// </summary>
    public void ValidateTokenRequest(TokenRequest request)
    {
        if (request == null)
            throw new AuthServerException(
                "invalid_request",
                "Token request is null",
                400);

        if (string.IsNullOrWhiteSpace(request.GrantType))
            throw new AuthServerException(
                "invalid_request",
                "grant_type is required",
                400);

        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new InvalidClientException("client_id is required");

        // Grant-type specific validation happens in the service layer
        _logger.LogDebug(
            "Token request validation successful: client={ClientId} grant_type={GrantType}",
            request.ClientId,
            request.GrantType);
    }

    /// <summary>
    /// Validates a consent request.
    /// </summary>
    public void ValidateConsentRequest(ConsentRequest request)
    {
        if (request == null)
            throw new AuthServerException(
                "invalid_request",
                "Consent request is null",
                400);

        if (string.IsNullOrWhiteSpace(request.ClientId))
            throw new InvalidClientException("client_id is required");

        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new AuthServerException(
                "invalid_request",
                "user_id is required for consent",
                400);

        _logger.LogDebug(
            "Consent request validation successful: client={ClientId} user={UserId}",
            request.ClientId,
            request.UserId);
    }

    /// <summary>
    /// Validates an HTTP request for security concerns.
    /// </summary>
    public void ValidateHttpRequest(HttpRequest httpRequest)
    {
        if (httpRequest == null)
            throw new ArgumentNullException(nameof(httpRequest));

        // HTTPS should be required in production for OAuth2 endpoints
        if (!httpRequest.IsHttps)
        {
            _logger.LogWarning(
                "Non-HTTPS request to OAuth2 endpoint: {Method} {Path}",
                httpRequest.Method,
                httpRequest.Path);

            // Note: Don't reject in development, but warn in production
        }

        // Validate Content-Length to prevent oversized bodies
        if (httpRequest.ContentLength > 1024 * 100) // 100 KB limit
        {
            throw new AuthServerException(
                "invalid_request",
                "Request body is too large",
                413);
        }
    }

    /// <summary>
    /// Checks if a response type is valid for OAuth2 specification.
    /// </summary>
    public bool IsValidResponseType(string responseType)
    {
        if (string.IsNullOrWhiteSpace(responseType))
            return false;

        var validTypes = new[] { "code", "token", "id_token", "code token", "code id_token", "token id_token", "code token id_token" };
        return validTypes.Contains(responseType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a grant type is valid for OAuth2 specification.
    /// </summary>
    public bool IsValidGrantType(string grantType)
    {
        if (string.IsNullOrWhiteSpace(grantType))
            return false;

        var validGrants = new[]
        {
            "authorization_code",
            "refresh_token",
            "client_credentials",
            "password",
            "urn:ietf:params:oauth:grant-type:device_flow",
            "urn:ietf:params:oauth:grant-type:jwt-bearer"
        };

        return validGrants.Contains(grantType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if scope string contains only valid characters.
    /// </summary>
    public bool IsValidScope(string scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
            return true; // Empty scope is sometimes valid

        // Scopes are space-delimited alphanumeric+underscore
        var parts = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.All(part => part.All(c => char.IsLetterOrDigit(c) || c == '_'));
    }
}
