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
        return _introspectionHandler.IntrospectToken(token);
    }

    /// <summary>
    /// Introspects a token to get its claims and validity (RFC 7662)
    /// </summary>
    public IntrospectionResponse IntrospectToken(string token)
    {
        return _introspectionHandler.IntrospectToken(token);
    }

    /// <summary>
    /// Revokes a token (access token or refresh token) per RFC 7009
    /// </summary>
    public async Task<RevocationResult> RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken cancellationToken)
    {
        return await _revocationHandler.RevokeTokenAsync(token, tokenTypeHint, cancellationToken);
    }
}
