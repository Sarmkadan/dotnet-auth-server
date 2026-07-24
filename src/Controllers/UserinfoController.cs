#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Controllers;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Security;
using DotnetAuthServer.Services;

/// <summary>
/// OAuth2 UserInfo endpoint (OIDC spec).
/// Returns claims about the authenticated user based on their access token.
/// Scope claims control what information is returned (openid, profile, email, etc.)
/// </summary>
[ApiController]
[Route("oauth/userinfo")]
[Authorize(AuthenticationSchemes = "Bearer")]
public sealed class UserinfoController : ControllerBase
{
    private readonly UserinfoHandler _userinfoHandler;
    private readonly RevokedTokenStore _revokedTokenStore;
    private readonly ITokenValidator _tokenValidator;
    private readonly ILogger<UserinfoController> _logger;

    public UserinfoController(
        UserinfoHandler userinfoHandler,
        RevokedTokenStore revokedTokenStore,
        ITokenValidator tokenValidator,
        ILogger<UserinfoController> logger)
    {
        _userinfoHandler = userinfoHandler ?? throw new ArgumentNullException(nameof(userinfoHandler));
        _revokedTokenStore = revokedTokenStore ?? throw new ArgumentNullException(nameof(revokedTokenStore));
        _tokenValidator = tokenValidator ?? throw new ArgumentNullException(nameof(tokenValidator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Handles OAuth2 UserInfo requests.
    /// Validates the access token and returns user claims based on granted scopes.
    /// </summary>
    [HttpGet]
    [HttpPost]
    [Consumes(Constants.ContentTypes.ApplicationFormUrlEncoded, Constants.ContentTypes.ApplicationJson)]
    [Produces(Constants.ContentTypes.ApplicationJson)]
    public async Task<IActionResult> GetUserinfo(CancellationToken cancellationToken)
    {
        try
        {
            // Extract and validate the access token from Authorization header
            var authHeader = Request.Headers.Authorization.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(authHeader))
            {
                _logger.LogWarning("Userinfo request without Authorization header");
                Response.Headers.WWWAuthenticate = $"error=\"invalid_request\", error_description=\"Authorization header is required\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "Authorization header is required"
                });
            }

            // Validate Authorization header format: must be "Bearer <token>"
            if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Userinfo request with invalid Authorization header format: {AuthHeader}", authHeader);
                Response.Headers.WWWAuthenticate = $"error=\"invalid_request\", error_description=\"Authorization header must use Bearer token type\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidRequest,
                    error_description = "Authorization header must use Bearer token type"
                });
            }

            var token = authHeader["Bearer ".Length..];
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Userinfo request with empty Bearer token");
                Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Bearer token is required\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidToken,
                    error_description = "Bearer token is required"
                });
            }

            // Validate token is not revoked
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(token) as JwtSecurityToken;
            if (jwtToken == null)
            {
                _logger.LogWarning("Userinfo request with malformed JWT token");
                Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Token format is invalid\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidToken,
                    error_description = "Token format is invalid"
                });
            }

            // Check if token is revoked
            var jti = jwtToken.Id;
            if (!string.IsNullOrWhiteSpace(jti) && _revokedTokenStore.IsRevoked(jti))
            {
                _logger.LogWarning("Userinfo request with revoked token jti={Jti}", jti);
                Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Token has been revoked\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidToken,
                    error_description = "Token has been revoked"
                });
            }

            // Validate token type - must be access token (not ID token or refresh token)
            // Access tokens should have "typ": "at+jwt" or similar access token type
            // ID tokens have "typ": "JWT" and "token_use": "id"
            // Refresh tokens have "token_use": "refresh"
            var claims = jwtToken.Claims.ToDictionary(c => c.Type, c => c.Value);

            // Check if this is an ID token (should not be used for userinfo endpoint)
            if (claims.TryGetValue("token_use", out var tokenUse) &&
                tokenUse.Equals("id", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Userinfo request with ID token (token_use=id)");
                Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"ID tokens cannot be used for userinfo requests\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidToken,
                    error_description = "ID tokens cannot be used for userinfo requests"
                });
            }

            // Check if this is a refresh token (should not be used for userinfo endpoint)
            if (claims.TryGetValue("token_use", out tokenUse) &&
                tokenUse.Equals("refresh", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Userinfo request with refresh token (token_use=refresh)");
                Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Refresh tokens cannot be used for userinfo requests\"";
                return Unauthorized(new
                {
                    error = Constants.ErrorCodes.InvalidToken,
                    error_description = "Refresh tokens cannot be used for userinfo requests"
                });
            }

            // Validate token has "openid" scope (required for userinfo endpoint per OIDC spec)
            var scopeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == Constants.Claims.Scope)?.Value;
            if (string.IsNullOrWhiteSpace(scopeClaim))
            {
                _logger.LogWarning("Userinfo request with token missing scope claim");
                Response.Headers.WWWAuthenticate = "error=\"insufficient_scope\", error_description=\"openid scope is required\"";
                return StatusCode(403, new { error = Constants.ErrorCodes.InvalidScope, error_description = "openid scope is required" });
            }

            var scopes = new HashSet<string>(scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            if (!scopes.Contains(Constants.Scopes.OpenId, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Userinfo request with token missing openid scope. Token scopes: {Scopes}", scopeClaim);
                Response.Headers.WWWAuthenticate = "error=\"insufficient_scope\", error_description=\"openid scope is required\"";
                return StatusCode(403, new { error = Constants.ErrorCodes.InvalidScope, error_description = "openid scope is required" });
            }

            // Validate token audience if present
            if (claims.TryGetValue(Constants.Claims.Aud, out var audience) &&
                !string.IsNullOrWhiteSpace(audience))
            {
                // For userinfo endpoint, the audience should typically be the resource server
                // or the client that presented the token
                _logger.LogDebug("Token audience: {Audience}", audience);
            }

            // Get the ClaimsPrincipal from the token
            var principal = new ClaimsPrincipal(new ClaimsIdentity(jwtToken.Claims, "Bearer"));

            // Call the UserinfoHandler to get user claims based on scopes
            var userinfo = await _userinfoHandler.GetUserinfoAsync(principal, cancellationToken);

            if (userinfo == null)
            {
                _logger.LogWarning("Userinfo request for unknown user");
                return NotFound();
            }

            _logger.LogInformation("Userinfo returned for user {UserId} with scopes {Scopes}",
                userinfo.Sub, scopeClaim);

            return Ok(userinfo);
        }
        catch (SecurityTokenExpiredException ex)
        {
            _logger.LogWarning(ex, "Userinfo request with expired token");
            Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Token expired\"";
            return Unauthorized(new
            {
                error = Constants.ErrorCodes.InvalidToken,
                error_description = "Token expired: " + ex.Message
            });
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Userinfo request with invalid token");
            Response.Headers.WWWAuthenticate = $"error=\"invalid_token\", error_description=\"Token validation failed: {ex.Message}\"";
            return Unauthorized(new
            {
                error = Constants.ErrorCodes.InvalidToken,
                error_description = "Token validation failed: " + ex.Message
            });
        }
        catch (AuthServerException ex) when (ex.StatusCode == 401 || ex.StatusCode == 403)
        {
            _logger.LogWarning(ex, "Userinfo request validation error");

            // Return appropriate error based on the exception
            var errorResponse = new { error = ex.ErrorCode, error_description = ex.Message };

            if (ex.StatusCode == 401)
            {
                Response.Headers.WWWAuthenticate = $"error=\"{ex.ErrorCode}\", error_description=\"{ex.Message}\"";
                return Unauthorized(errorResponse);
            }
            else
            {
                Response.Headers.WWWAuthenticate = $"error=\"{ex.ErrorCode}\", error_description=\"{ex.Message}\"";
                return StatusCode(403, errorResponse);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in userinfo endpoint");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred"
            });
        }
    }
}