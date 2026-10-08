#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Configuration for <see cref="DotnetAuthServer.Data.UserStore"/>, bound from the
/// <c>DotnetAuthServer:UserStore</c> section. Lockout and storage settings are read from
/// <see cref="AuthServerOptions"/> and are validated alongside these options.
/// </summary>
public sealed class UserStoreOptions
{
    /// <summary>
    /// Configuration key prefix for this section, used in validation messages.
    /// </summary>
    public const string SectionKey = "DotnetAuthServer:UserStore";

    /// <summary>
    /// Password hashing settings applied when creating and verifying user passwords.
    /// </summary>
    [Required]
    public PasswordHashingOptions PasswordHashing { get; set; } = new();
}
