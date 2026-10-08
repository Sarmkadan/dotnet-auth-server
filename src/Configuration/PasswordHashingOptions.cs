#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Password hashing parameters for the user store.
/// </summary>
public sealed class PasswordHashingOptions
{
    /// <summary>
    /// Hashing algorithm. Only <c>Pbkdf2</c> (PBKDF2-HMAC-SHA256) is supported.
    /// </summary>
    public string Algorithm { get; set; } = UserStoreOptionsValidator.Pbkdf2Algorithm;

    /// <summary>
    /// PBKDF2 iteration count. Must be between <see cref="UserStoreOptionsValidator.MinPbkdf2Iterations"/>
    /// and <see cref="UserStoreOptionsValidator.MaxPbkdf2Iterations"/>.
    /// </summary>
    public int Iterations { get; set; } = UserStoreOptionsValidator.MinPbkdf2Iterations;
}
