#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Extension methods for the Client entity providing additional functionality
/// </summary>
public static class ClientExtensions
{
    /// <summary>
    /// Determines if the client is a public client (not confidential)
    /// </summary>
    /// <param name="client">The client to check</param>
    /// <returns>True if the client is public; otherwise false</returns>
    public static bool IsPublicClient(this Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return !client.IsConfidential;
    }

    /// <summary>
    /// Determines if the client requires PKCE based on its configuration
    /// </summary>
    /// <param name="client">The client to check</param>
    /// <returns>True if PKCE is required; otherwise false</returns>
    public static bool RequiresPkce(this Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.IsConfidential && client.RequirePkce;
    }

    /// <summary>
    /// Gets the default token lifetime in minutes for easier configuration
    /// </summary>
    /// <param name="client">The client to get lifetime for</param>
    /// <param name="tokenType">Type of token (access or refresh)</param>
    /// <returns>Token lifetime in minutes</returns>
    public static int GetTokenLifetimeMinutes(this Client client, TokenType tokenType)
    {
        ArgumentNullException.ThrowIfNull(client);
        return tokenType switch
        {
            TokenType.Access => client.AccessTokenLifetime / 60,
            TokenType.Refresh => client.RefreshTokenLifetime / 60,
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, "Invalid token type")
        };
    }

    /// <summary>
    /// Checks if the client has any allowed CORS origins configured
    /// </summary>
    /// <param name="client">The client to check</param>
    /// <returns>True if CORS origins are configured; otherwise false</returns>
    public static bool HasCorsOrigins(this Client client)
    {
        ArgumentNullException.ThrowIfNull(client);
        return client.AllowedCorsOrigins.Count > 0;
    }
}

/// <summary>
/// Enum representing different token types for extension methods
/// </summary>
public enum TokenType
{
    Access,
    Refresh
}