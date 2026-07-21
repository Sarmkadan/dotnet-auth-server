#nullable enable

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Handlers;

/// <summary>
/// Interface for validating OAuth2/OIDC tokens and related operations
/// </summary>
public interface ITokenValidator
{
    /// <summary>
    /// Validates a JWT token and returns the principal if valid
    /// </summary>
    Task<IntrospectionResponse> ValidateTokenAsync(string token, string? tokenTypeHint = null);

    /// <summary>
    /// Introspects a token to get its claims and validity (RFC 7662)
    /// </summary>
    IntrospectionResponse IntrospectToken(string token);

    /// <summary>
    /// Revokes a token (access token or refresh token) per RFC 7009
    /// </summary>
    Task<RevocationResult> RevokeTokenAsync(string token, string? tokenTypeHint, CancellationToken cancellationToken);
}