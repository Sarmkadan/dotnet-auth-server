#nullable enable

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Handlers;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for validating OAuth2/OIDC tokens and related operations
/// </summary>
public sealed class TokenValidator : ITokenValidator
{
    private readonly TokenRevocationHandler _revocationHandler;
    private readonly TokenIntrospectionHandler _introspectionHandler;
    private readonly ILogger<TokenValidator> _logger;

    public TokenValidator(
        TokenRevocationHandler revocationHandler,
        TokenIntrospectionHandler introspectionHandler,
        ILogger<TokenValidator> logger)
    {
        _revocationHandler = revocationHandler ?? throw new ArgumentNullException(nameof(revocationHandler));
        _introspectionHandler = introspectionHandler ?? throw new ArgumentNullException(nameof(introspectionHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validates a JWT token and returns the introspection response
    /// </summary>
    public async Task<IntrospectionResponse> ValidateTokenAsync(string token, string? tokenTypeHint = null)
    {
        _logger.LogInformation("Validating token with hint {TokenTypeHint}", tokenTypeHint);
        var result = _introspectionHandler.IntrospectToken(token);
        _logger.LogInformation("Token validation complete");
        return result;
    }

    /// <summary>
    /// Introspects a token to get its claims and validity (RFC 7662)
    /// </summary>
    public IntrospectionResponse IntrospectToken(string token)
    {
        _logger.LogInformation("Introspecting token");
        var result = _introspectionHandler.IntrospectToken(token);
        _logger.LogInformation("Token introspection complete");
        return result;
    }

    /// <summary>
    /// Revokes a token (access token or refresh token) per RFC 7009
    /// </summary>
    public async Task<RevocationResult> RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Revoking token with hint {TokenTypeHint}", tokenTypeHint);
        try
        {
            var result = await _revocationHandler.RevokeTokenAsync(token, tokenTypeHint, cancellationToken);
            _logger.LogInformation("Token revocation complete");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token revocation");
            throw;
        }
    }
}
