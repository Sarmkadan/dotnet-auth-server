#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Handler for comprehensive OAuth2 request validation per OAuth 2.1 BCP requirements.
/// Validates authorization requests, token requests, and other OAuth2 operations
/// for correctness and security before processing.
/// </summary>
public sealed class RequestValidationHandler
{
    private readonly ILogger<RequestValidationHandler> _logger;

    // Maximum allowed request component sizes to prevent DOS attacks per OAuth 2.1 BCP
    private const int MaxScopeLength = 500;
    private const int MaxStateLength = 500;
    private const int MaxRedirectUriLength = 2000;
    private const int MaxNonceLength = 500;
    private const int MaxLoginHintLength = 1000;

    public RequestValidationHandler(ILogger<RequestValidationHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates an authorization request for structural integrity and required parameters.
    /// Enforces OAuth 2.1 BCP security requirements including exact redirect_uri matching,
    /// PKCE validation, parameter length limits, and client validation.
    /// </summary>
    /// <param name="request">The authorization request to validate</param>
    /// <param name="client">The client making the request</param>
    /// <exception cref="AuthServerException">Thrown when validation fails</exception>
    public void ValidateAuthorizationRequest(AuthorizationRequest request, Client client)
    {
        _logger.LogInformation("Validating authorization request for client={ClientId}", request?.ClientId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);

        // Validate required parameters
        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Authorization request validation failed: client_id is required");
            throw new InvalidClientException("client_id is required");
        }

        if (string.IsNullOrWhiteSpace(request.ResponseType))
        {
            _logger.LogWarning("Authorization request validation failed: response_type is required, client={ClientId}", request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "response_type is required",
                400);
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri))
        {
            _logger.LogWarning("Authorization request validation failed: redirect_uri is required, client={ClientId}", request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "redirect_uri is required",
                400);
        }

        // Validate sizes to prevent DOS attacks per OAuth 2.1 BCP
        if ((request.Scope?.Length ?? 0) > MaxScopeLength)
        {
            _logger.LogWarning("Authorization request validation failed: scope exceeds maximum length={Length}, client={ClientId}", request.Scope?.Length, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "Scope parameter exceeds maximum length",
                400);
        }

        if ((request.State?.Length ?? 0) > MaxStateLength)
        {
            _logger.LogWarning("Authorization request validation failed: state exceeds maximum length={Length}, client={ClientId}", request.State!.Length, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "State parameter exceeds maximum length",
                400);
        }

        if (request.RedirectUri.Length > MaxRedirectUriLength)
        {
            _logger.LogWarning("Authorization request validation failed: redirect_uri exceeds maximum length={Length}, client={ClientId}", request.RedirectUri.Length, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "Redirect URI exceeds maximum length",
                400);
        }

        if ((request.Nonce?.Length ?? 0) > MaxNonceLength)
        {
            _logger.LogWarning("Authorization request validation failed: nonce exceeds maximum length={Length}, client={ClientId}", request.Nonce?.Length, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "Nonce parameter exceeds maximum length",
                400);
        }

        if ((request.LoginHint?.Length ?? 0) > MaxLoginHintLength)
        {
            _logger.LogWarning("Authorization request validation failed: login_hint exceeds maximum length={Length}, client={ClientId}", request.LoginHint?.Length, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "Login hint parameter exceeds maximum length",
                400);
        }

        // Validate redirect_uri: enforce EXACT string matching per OAuth 2.1 BCP
        // This prevents subdomain matching attacks and prefix matching attacks
        if (!client.IsRedirectUriValid(request.RedirectUri))
        {
            _logger.LogWarning("Authorization request validation failed: redirect_uri={RedirectUri} is not registered for client={ClientId}", request.RedirectUri, request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "The redirect_uri is not registered",
                400);
        }

        // Validate response_type against client's allowed grant types per OAuth 2.1 BCP
        if (!client.IsGrantTypeAllowed(request.ResponseType))
        {
            _logger.LogWarning("Authorization request validation failed: response_type={ResponseType} not allowed for client={ClientId}", request.ResponseType, request.ClientId);
            throw new UnauthorizedClientException("The response_type is not allowed for this client");
        }

        // Validate PKCE requirements per OAuth 2.1 BCP
        ValidatePkceRequirements(request, client);

        _logger.LogInformation("Authorization request validation successful: client={ClientId} response_type={ResponseType} redirect_uri={RedirectUri}", request.ClientId, request.ResponseType, request.RedirectUri);
    }

    /// <summary>
    /// Validates PKCE requirements for authorization requests per OAuth 2.1 BCP.
    /// Enforces:
    /// - Rejection of code_challenge_method=plain
    /// - PKCE required for public clients
    /// - Valid code challenge format
    /// </summary>
    /// <param name="request">The authorization request</param>
    /// <param name="client">The client making the request</param>
    /// <exception cref="AuthServerException">Thrown when validation fails</exception>
    public void ValidatePkceRequirements(AuthorizationRequest request, Client client)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(client);

        // Reject code_challenge_method=plain per OAuth 2.1 BCP requirement
        if (request.CodeChallengeMethod == Constants.PkceChallengeMethods.Plain)
        {
            _logger.LogWarning("PKCE validation failed: plain code_challenge_method not allowed, client={ClientId}", request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "code_challenge_method=plain is not supported, use S256",
                400);
        }

        // Enforce PKCE for public clients per OAuth 2.1 BCP
        if (client.IsConfidential == false && !request.HasPkce())
        {
            _logger.LogWarning("PKCE validation failed: PKCE required for public client, client={ClientId}", request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "PKCE is required for public clients",
                400);
        }

        // Validate code challenge format if PKCE is used
        if (request.HasPkce() && !IsValidCodeChallenge(request.CodeChallenge))
        {
            _logger.LogWarning("PKCE validation failed: invalid code challenge format, client={ClientId}", request.ClientId);
            throw new AuthServerException(
                "invalid_request",
                "Invalid code_challenge format",
                400);
        }
    }

    /// <summary>
    /// Validates that a response_type is allowed for the client per OAuth 2.1 BCP.
    /// </summary>
    /// <param name="responseType">The response type to validate</param>
    /// <param name="allowedGrantTypes">Collection of allowed grant types</param>
    /// <exception cref="UnauthorizedClientException">Thrown when validation fails</exception>
    public void ValidateResponseTypeAllowed(string responseType, ICollection<string> allowedGrantTypes)
    {
        ArgumentException.ThrowIfNullOrEmpty(responseType);
        ArgumentNullException.ThrowIfNull(allowedGrantTypes);

        if (allowedGrantTypes.Count == 0)
        {
            throw new ArgumentException("Allowed grant types cannot be empty", nameof(allowedGrantTypes));
        }

        // Check if the response_type is in the client's allowed grant types
        // Note: response_type values like "code" should match grant_type values like "authorization_code"
        var normalizedResponseType = responseType.ToLowerInvariant();
        var isAllowed = allowedGrantTypes.Contains(normalizedResponseType, StringComparer.OrdinalIgnoreCase);

        if (!isAllowed)
        {
            _logger.LogWarning("Response type validation failed: response_type={ResponseType} not in allowed grant types", responseType);
            throw new UnauthorizedClientException("The response_type is not allowed for this client");
        }
    }

    /// <summary>
    /// Validates PKCE code challenge format per OAuth 2.1 specification.
    /// </summary>
    /// <param name="codeChallenge">The code challenge to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidCodeChallenge(string? codeChallenge)
    {
        if (string.IsNullOrWhiteSpace(codeChallenge))
        {
            return false;
        }

        // Code challenge should be between 43-128 characters for S256
        // and exactly match code_verifier for plain
        return codeChallenge.Length >= 43 &&
               codeChallenge.Length <= 128 &&
               codeChallenge.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == '_' || c == '~');
    }

    /// <summary>
    /// Validates a token request for correctness.
    /// </summary>
    /// <param name="request">The token request to validate</param>
    /// <exception cref="AuthServerException">Thrown when validation fails</exception>
    public void ValidateTokenRequest(TokenRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.GrantType))
        {
            _logger.LogWarning("Token request validation failed: grant_type is required");
            throw new AuthServerException(
                "invalid_request",
                "grant_type is required",
                400);
        }

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Token request validation failed: client_id is required");
            throw new InvalidClientException("client_id is required");
        }

        _logger.LogDebug(
            "Token request validation successful: client={ClientId} grant_type={GrantType}",
            request.ClientId,
            request.GrantType);
    }

    /// <summary>
    /// Validates a consent request.
    /// </summary>
    /// <param name="request">The consent request to validate</param>
    /// <exception cref="AuthServerException">Thrown when validation fails</exception>
    public void ValidateConsentRequest(ConsentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.ClientId))
        {
            _logger.LogWarning("Consent request validation failed: client_id is required");
            throw new InvalidClientException("client_id is required");
        }

        if (string.IsNullOrWhiteSpace(request.UserId))
        {
            _logger.LogWarning("Consent request validation failed: user_id is required for consent");
            throw new AuthServerException(
                "invalid_request",
                "user_id is required for consent",
                400);
        }

        _logger.LogDebug(
            "Consent request validation successful: client={ClientId} user={UserId}",
            request.ClientId,
            request.UserId);
    }

    /// <summary>
    /// Validates an HTTP request for security concerns.
    /// </summary>
    /// <param name="httpRequest">The HTTP request to validate</param>
    /// <exception cref="AuthServerException">Thrown when validation fails</exception>
    public void ValidateHttpRequest(HttpRequest httpRequest)
    {
        ArgumentNullException.ThrowIfNull(httpRequest);

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
            _logger.LogWarning("Request validation failed: request body is too large, length={Length}", httpRequest.ContentLength);
            throw new AuthServerException(
                "invalid_request",
                "Request body is too large",
                413);
        }
    }

    /// <summary>
    /// Checks if a response type is valid for OAuth2 specification.
    /// </summary>
    /// <param name="responseType">The response type to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidResponseType(string? responseType)
    {
        if (string.IsNullOrWhiteSpace(responseType))
        {
            return false;
        }

        var validTypes = new[] { "code", "token", "id_token", "code token", "code id_token", "token id_token", "code token id_token" };
        return validTypes.Contains(responseType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a grant type is valid for OAuth2 specification.
    /// </summary>
    /// <param name="grantType">The grant type to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidGrantType(string? grantType)
    {
        if (string.IsNullOrWhiteSpace(grantType))
        {
            return false;
        }

        var validGrants = new[] {
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
    /// <param name="scope">The scope string to validate</param>
    /// <returns>True if valid, false otherwise</returns>
    public bool IsValidScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return true; // Empty scope is sometimes valid
        }

        // Scopes are space-delimited alphanumeric+underscore
        var parts = scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.All(part => part.All(c => char.IsLetterOrDigit(c) || c == '_'));
    }
}