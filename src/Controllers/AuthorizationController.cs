// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;

/// <summary>
/// OAuth2 Authorization endpoint for handling authorization requests
/// </summary>
[ApiController]
[Route("oauth/authorize")]
public class AuthorizationController : ControllerBase
{
    private readonly AuthorizationService _authorizationService;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        AuthorizationService authorizationService,
        ILogger<AuthorizationController> logger)
    {
        _authorizationService = authorizationService;
        _logger = logger;
    }

    /// <summary>
    /// Handles authorization requests (GET)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> AuthorizeAsync(
        [FromQuery] string? client_id,
        [FromQuery] string? response_type,
        [FromQuery] string? redirect_uri,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? nonce,
        [FromQuery] string? code_challenge,
        [FromQuery] string? code_challenge_method,
        [FromQuery] string? display,
        [FromQuery] string? prompt,
        [FromQuery] int? max_age,
        [FromQuery] string? login_hint,
        CancellationToken cancellationToken)
    {
        try
        {
            var authRequest = new AuthorizationRequest
            {
                ClientId = client_id,
                ResponseType = response_type,
                RedirectUri = redirect_uri,
                Scope = scope,
                State = state,
                Nonce = nonce,
                CodeChallenge = code_challenge,
                CodeChallengeMethod = code_challenge_method,
                Display = display,
                Prompt = prompt,
                MaxAge = max_age,
                LoginHint = login_hint
            };

            // Validate the authorization request
            var validatedRequest = await _authorizationService.ValidateAuthorizationRequestAsync(
                authRequest, cancellationToken);

            // In a real implementation, this would redirect to login and consent screens
            // For now, return the validated request details
            return Ok(new
            {
                message = "Authorization request validated",
                clientId = validatedRequest.ClientId,
                redirectUri = validatedRequest.RedirectUri,
                requestedScopes = validatedRequest.GetRequestedScopes().ToList(),
                requiresPkce = validatedRequest.HasPkce()
            });
        }
        catch (AuthServerException ex)
        {
            _logger.LogWarning("Authorization request error: {ErrorCode} - {Message}",
                ex.ErrorCode, ex.Message);

            // For authorization errors, redirect to redirect_uri with error parameters
            if (!string.IsNullOrWhiteSpace(HttpContext.Request.Query["redirect_uri"]))
            {
                var redirectUri = HttpContext.Request.Query["redirect_uri"].ToString();
                var separator = redirectUri.Contains('?') ? "&" : "?";
                var errorUri = $"{redirectUri}{separator}error={ex.ErrorCode}&error_description={Uri.EscapeDataString(ex.ErrorDescription ?? ex.Message)}";

                if (!string.IsNullOrWhiteSpace(HttpContext.Request.Query["state"]))
                    errorUri += $"&state={HttpContext.Request.Query["state"]}";

                return Redirect(errorUri);
            }

            return StatusCode(ex.StatusCode, ex.ToErrorResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in authorization endpoint");
            var errorResponse = new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Gets the consent prompt for user approval
    /// </summary>
    [HttpGet("consent")]
    public async Task<IActionResult> GetConsentPromptAsync(
        [FromQuery] string? client_id,
        [FromQuery] string? user_id,
        [FromQuery] string? scope,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(user_id))
            {
                return BadRequest(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "client_id and user_id are required"
                });
            }

            // In a real implementation, retrieve user and client details
            var consentResponse = new
            {
                clientId = client_id,
                userId = user_id,
                requestedScopes = scope?.Split(' ') ?? Array.Empty<string>()
            };

            return Ok(consentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting consent prompt");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Handles consent form submission (POST)
    /// </summary>
    [HttpPost("consent")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> SubmitConsentAsync(
        [FromForm] string? client_id,
        [FromForm] string? user_id,
        [FromForm] bool approved,
        [FromForm] string? granted_scopes,
        [FromForm] string? state,
        CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(client_id) || string.IsNullOrWhiteSpace(user_id))
            {
                return BadRequest(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "client_id and user_id are required"
                });
            }

            var consentRequest = new ConsentRequest
            {
                ClientId = client_id,
                UserId = user_id,
                Approved = approved,
                GrantedScopes = (granted_scopes ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
            };

            // In a real implementation, process the consent and issue an authorization code
            return Ok(new
            {
                message = "Consent submitted",
                clientId = client_id,
                approved = approved,
                state = state
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting consent");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            });
        }
    }
}
