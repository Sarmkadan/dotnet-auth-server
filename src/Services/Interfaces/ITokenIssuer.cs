#nullable enable

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;

/// <summary>
/// Interface for issuing and managing OAuth2/OIDC tokens
/// </summary>
public interface ITokenIssuer
{
    /// <summary>
    /// Handles token request and returns a token response
    /// </summary>
    Task<TokenResponse> HandleTokenRequestAsync(TokenRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates client secret
    /// </summary>
    bool ValidateClientSecret(Client client, string? providedSecret);

    /// <summary>
    /// Hashes a client secret using SHA256
    /// </summary>
    string HashClientSecret(string secret);
}