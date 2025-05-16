namespace DotnetAuthServer.Handlers;

using System.Linq;

/// <summary>
/// Extension methods for <see cref="JwksHandler"/> that provide convenient operations for working with JWKS (JSON Web Key Set).
/// </summary>
public static class JwksHandlerExtensions
{
    /// <summary>
    /// Gets the JWKS response and extracts the first key if available.
    /// </summary>
    /// <param name="handler">The JWKS handler instance.</param>
    /// <returns>The first key in the JWKS response, or null if no keys are available.</returns>
    public static async Task<JwkKey?> GetFirstKeyAsync(this JwksHandler handler)
    {
        var response = await handler.GetJwksAsync().ConfigureAwait(false);
        return response?.Keys?.FirstOrDefault();
    }

    /// <summary>
    /// Checks if the handler contains a key with the specified key ID (kid).
    /// </summary>
    /// <param name="handler">The JWKS handler instance.</param>
    /// <param name="kid">The key ID to search for.</param>
    /// <returns>True if a key with the specified kid exists; otherwise, false.</returns>
    public static async Task<bool> ContainsKeyIdAsync(this JwksHandler handler, string kid)
    {
        if (string.IsNullOrEmpty(kid))
        {
            return false;
        }

        return await handler.IsValidKeyIdAsync(kid).ConfigureAwait(false);
    }

    /// <summary>
    /// Gets all keys of a specific key type (kty) from the JWKS.
    /// </summary>
    /// <param name="handler">The JWKS handler instance.</param>
    /// <param name="keyType">The key type to filter by (e.g., "RSA", "EC", "oct").</param>
    /// <returns>A list of keys matching the specified key type.</returns>
    public static async Task<List<JwkKey>> GetKeysByTypeAsync(this JwksHandler handler, string keyType)
    {
        var response = await handler.GetJwksAsync().ConfigureAwait(false);
        return response?.Keys?.Where(k => string.Equals(k.Kty, keyType, StringComparison.Ordinal))
            .ToList() ?? new List<JwkKey>();
    }

    /// <summary>
    /// Gets all keys that are usable for signing (use = "sig") from the JWKS.
    /// </summary>
    /// <param name="handler">The JWKS handler instance.</param>
    /// <returns>A list of keys that can be used for signing operations.</returns>
    public static async Task<List<JwkKey>> GetSigningKeysAsync(this JwksHandler handler)
    {
        var response = await handler.GetJwksAsync().ConfigureAwait(false);
        return response?.Keys?.Where(k => string.Equals(k.Use, "sig", StringComparison.Ordinal))
            .ToList() ?? new List<JwkKey>();
    }
}