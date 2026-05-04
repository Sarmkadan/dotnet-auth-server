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
/// OAuth2 Token endpoint for issuing access tokens
/// </summary>
[ApiController]
[Route("oauth/token")]
public class TokenController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly ILogger<TokenController> _logger;

    public TokenController(TokenService tokenService, ILogger<TokenController> logger)
    {
        _tokenService = tokenService;
        _logger = logger;
    }

    /// <summary>
    /// Handles OAuth2 token requests
    /// </summary>
    [HttpPost]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public async Task<IActionResult> RequestToken(
        [FromForm] string? grant_type,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        [FromForm] string? code,
        [FromForm] string? redirect_uri,
        [FromForm] string? refresh_token,
        [FromForm] string? username,
        [FromForm] string? password,
        [FromForm] string? scope,
        [FromForm] string? code_verifier,
        CancellationToken cancellationToken)
    {
        try
        {
            var tokenRequest = new TokenRequest
            {
                GrantType = grant_type,
                ClientId = client_id,
                ClientSecret = client_secret,
                Code = code,
                RedirectUri = redirect_uri,
                RefreshToken = refresh_token,
                Username = username,
                Password = password,
                Scope = scope,
                CodeVerifier = code_verifier
            };

            var response = await _tokenService.HandleTokenRequestAsync(tokenRequest, cancellationToken);
            return Ok(response);
        }
        catch (AuthServerException ex)
        {
            _logger.LogWarning("Token request error: {ErrorCode} - {Message}", ex.ErrorCode, ex.Message);
            return StatusCode(ex.StatusCode, ex.ToErrorResponse());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in token endpoint");
            var errorResponse = new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            };
            return StatusCode(500, errorResponse);
        }
    }

    /// <summary>
    /// Introspects a token to get its claims and validity
    /// </summary>
    [HttpPost("introspect")]
    [Consumes("application/x-www-form-urlencoded")]
    [Produces("application/json")]
    public IActionResult IntrospectToken(
        [FromForm] string? token,
        [FromForm] string? token_type_hint,
        [FromForm] string? client_id,
        [FromForm] string? client_secret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "token parameter is required"
                });
            }

            // In a real implementation, validate the token and return its claims
            // For now, this is a placeholder
            var response = new
            {
                active = false,
                error = Constants.ErrorCodes.InvalidGrant
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error introspecting token");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            });
        }
    }

    /// <summary>
    /// Revokes a token (access token or refresh token)
    /// </summary>
    [HttpPost("revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    public IActionResult RevokeToken(
        [FromForm] string? token,
        [FromForm] string? token_type_hint,
        [FromForm] string? client_id,
        [FromForm] string? client_secret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "token parameter is required"
                });
            }

            // In a real implementation, revoke the token
            // For now, this is a placeholder
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking token");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            });
        }
    }
}
