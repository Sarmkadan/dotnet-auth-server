#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Services;

/// <summary>
/// OAuth2 Token endpoint for issuing access tokens
/// </summary>
[ApiController]
[Route("oauth/token")]
public sealed class TokenController : ControllerBase
{
    private readonly TokenService _tokenService;
    private readonly TokenIntrospectionHandler _introspectionHandler;
    private readonly TokenRevocationHandler _revocationHandler;
    private readonly ILogger<TokenController> _logger;

    public TokenController(
        TokenService tokenService,
        TokenIntrospectionHandler introspectionHandler,
        TokenRevocationHandler revocationHandler,
        ILogger<TokenController> logger)
    {
        _tokenService = tokenService;
        _introspectionHandler = introspectionHandler;
        _revocationHandler = revocationHandler;
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
                CodeVerifier = code_verifier,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
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
    /// Introspects a token to get its claims and validity (RFC 7662).
    /// Invalid, expired, or revoked tokens are reported as inactive.
    /// </summary>
    [HttpPost("introspect")]
    [HttpPost("/oauth/introspect")]
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

            var result = _introspectionHandler.IntrospectToken(token);

            if (!result.Active)
                return Ok(new { active = false });

            return Ok(new
            {
                active = true,
                scope = result.Scope,
                client_id = result.ClientId,
                username = result.Username,
                token_type = result.TokenType,
                exp = result.Exp,
                iat = result.Iat,
                sub = result.Sub
            });
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
    /// Revokes a token (access token or refresh token) per RFC 7009.
    /// Always returns 200 OK for valid requests, even when the token is unknown,
    /// to prevent token enumeration.
    /// </summary>
    [HttpPost("revoke")]
    [HttpPost("/oauth/revoke")]
    [Consumes("application/x-www-form-urlencoded")]
    public async Task<IActionResult> RevokeToken(
        [FromForm] string? token,
        [FromForm] string? token_type_hint,
        [FromForm] string? client_id,
        [FromForm] string? client_secret,
        CancellationToken cancellationToken)
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

            await _revocationHandler.RevokeTokenAsync(token, token_type_hint, cancellationToken);
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
