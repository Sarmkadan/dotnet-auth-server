#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Provides configurable password policy validation with clear error messages.
/// </summary>
public sealed class PasswordValidationService
{
    private readonly AuthServerOptions _options;
    private readonly PasswordPolicyOptions _passwordPolicy;

    /// <summary>
    /// Initializes a new instance of <see cref="PasswordValidationService"/>.
    /// </summary>
    public PasswordValidationService(AuthServerOptions options, PasswordPolicyOptions? passwordPolicy = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _passwordPolicy = passwordPolicy ?? new PasswordPolicyOptions();
    }

    /// <summary>
    /// Validates a password against the configured policy.
    /// </summary>
    /// <param name="password">The password to validate</param>
    /// <param name="username">Optional username to check against (for "not equal to username" policy)</param>
    /// <returns>A list of validation error messages (empty if valid)</returns>
    public IReadOnlyList<string> ValidatePassword(string password, string? username = null)
    {
        var errors = new List<string>();

        // Check minimum length
        if (_passwordPolicy.RequireMinimumLength && password.Length < _passwordPolicy.MinimumLength)
        {
            errors.Add($"Password must be at least {_passwordPolicy.MinimumLength} characters long");
        }

        // Check maximum length
        if (_passwordPolicy.RequireMaximumLength && password.Length > _passwordPolicy.MaximumLength)
        {
            errors.Add($"Password must be no more than {_passwordPolicy.MaximumLength} characters long");
        }

        // Check character class requirements
        if (_passwordPolicy.RequireLowercase && !password.Any(char.IsLower))
        {
            errors.Add("Password must contain at least one lowercase letter (a-z)");
        }

        if (_passwordPolicy.RequireUppercase && !password.Any(char.IsUpper))
        {
            errors.Add("Password must contain at least one uppercase letter (A-Z)");
        }

        if (_passwordPolicy.RequireDigit && !password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one digit (0-9)");
        }

        if (_passwordPolicy.RequireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            errors.Add("Password must contain at least one special character (!@#$%^&* etc.)");
        }

        // Check username exclusion
        if (_passwordPolicy.RequireNotEqualToUsername && !string.IsNullOrWhiteSpace(username))
        {
            if (string.Equals(password, username, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Password cannot be the same as your username");
            }

            // Also check common variations
            if (_passwordPolicy.CheckUsernameVariations &&
                (password.Equals(username + "123", StringComparison.OrdinalIgnoreCase) ||
                 password.Equals("123" + username, StringComparison.OrdinalIgnoreCase) ||
                 password.Equals(username + username, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add("Password cannot be based on your username or common variations");
            }
        }

        // Check against common patterns if enabled
        if (_passwordPolicy.CheckCommonPatterns && _passwordPolicy.CommonPatterns.Any())
        {
            var passwordLower = password.ToLowerInvariant();
            foreach (var pattern in _passwordPolicy.CommonPatterns)
            {
                if (passwordLower.Contains(pattern.ToLowerInvariant()))
                {
                    errors.Add($"Password cannot contain common pattern: '{pattern}'");
                }
            }
        }

        // Check against password history if enabled
        if (_passwordPolicy.CheckAgainstHistory && !string.IsNullOrWhiteSpace(_passwordPolicy.HistoryCheckPassword))
        {
            if (string.Equals(password, _passwordPolicy.HistoryCheckPassword, StringComparison.Ordinal))
            {
                errors.Add("Password must be different from your previous password");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a password and throws an exception with all error messages if invalid.
    /// </summary>
    public void ValidateAndThrow(string password, string? username = null)
    {
        var errors = ValidatePassword(password, username);

        if (errors.Count > 0)
        {
            var errorMessage = string.Join("; ", errors);
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                errorMessage,
                400);
        }
    }
}

/// <summary>
/// Configuration options for password policy validation.
/// </summary>
public sealed class PasswordPolicyOptions
{
    /// <summary>
    /// Gets or sets whether to require a minimum password length.
    /// Default: true
    /// </summary>
    public bool RequireMinimumLength { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum password length.
    /// Default: 8
    /// </summary>
    public int MinimumLength { get; set; } = 8;

    /// <summary>
    /// Gets or sets whether to require a maximum password length.
    /// Default: false (no maximum)
    /// </summary>
    public bool RequireMaximumLength { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum password length.
    /// Default: 100
    /// </summary>
    public int MaximumLength { get; set; } = 100;

    /// <summary>
    /// Gets or sets whether to require at least one lowercase letter.
    /// Default: false
    /// </summary>
    public bool RequireLowercase { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to require at least one uppercase letter.
    /// Default: false
    /// </summary>
    public bool RequireUppercase { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to require at least one digit.
    /// Default: false
    /// </summary>
    public bool RequireDigit { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to require at least one special character.
    /// Default: false
    /// </summary>
    public bool RequireSpecialChar { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to prevent password from being equal to username.
    /// Default: true
    /// </summary>
    public bool RequireNotEqualToUsername { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check common username variations (username123, 123username, etc.).
    /// Default: true
    /// </summary>
    public bool CheckUsernameVariations { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check against common patterns (e.g., "password", "123456").
    /// Default: true
    /// </summary>
    public bool CheckCommonPatterns { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to check against password history.
    /// Default: false
    /// </summary>
    public bool CheckAgainstHistory { get; set; } = false;

    /// <summary>
    /// Gets or sets the previous password to check against (if CheckAgainstHistory is true).
    /// Default: null
    /// </summary>
    public string? HistoryCheckPassword { get; set; }

    /// <summary>
    /// Gets or sets common patterns to check against.
    /// Default: ["password", "123456", "qwerty", "admin", "welcome", "letmein", "monkey"]
    /// </summary>
    public ICollection<string> CommonPatterns { get; set; } = new List<string>
    {
        "password", "123456", "12345678", "1234", "qwerty", "12345",
        "dragon", "baseball", "football", "letmein", "monkey", "abc123",
        "mustang", "michael", "shadow", "master", "jennifer", "111111",
        "2000", "jordan", "superman", "harley", "1234567", "freedom",
        "whatever", "trustno1", "sunshine", "iloveyou", "starwars", "login",
        "admin", "welcome", "login123", "password1", "admin123"
    };
}