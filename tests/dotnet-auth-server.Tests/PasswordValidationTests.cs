#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for password validation with configurable policies.
/// </summary>
public sealed class PasswordValidationTests
{
    private AuthServerOptions _options;
    private PasswordValidationService _validator;

    /// <summary>
    /// Initializes a new instance of the <see cref="PasswordValidationTests"/> class.
    /// </summary>
    public PasswordValidationTests()
    {
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = new string('x', 32),
            DatabaseConnectionString = ""
        };
        // Configure password policy for testing
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireDigit = false,
            RequireSpecialChar = false,
            RequireNotEqualToUsername = false,
            CheckCommonPatterns = false,
            CheckAgainstHistory = false
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);
    }

    /// <summary>
    /// Tests that a password meeting minimum length requirement passes validation.
    /// </summary>
    [Fact]
    public void ValidatePassword_MinimumLength_Valid()
    {
        // Arrange
        var password = "ValidPass123!";

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that a password shorter than minimum length fails validation.
    /// </summary>
    [Fact]
    public void ValidatePassword_TooShort_Fails()
    {
        // Arrange
        var password = "Short1!";
        _options.PasswordPolicy.MinimumLength = 8;

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must be at least 8 characters long");
    }

    /// <summary>
    /// Tests that a password equal to username fails validation.
    /// </summary>
    [Fact]
    public void ValidatePassword_EqualToUsername_Fails()
    {
        // Arrange
        var username = "john";
        var password = "john";
        _options.PasswordPolicy.RequireNotEqualToUsername = true;
        _options.PasswordPolicy.MinimumLength = 1; // Allow short passwords for this test

        // Act
        var errors = _validator.ValidatePassword(password, username);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password cannot be the same as your username");
    }

    /// <summary>
    /// Tests that a password with username variation fails validation.
    /// </summary>
    [Fact]
    public void ValidatePassword_UsernameVariation_Fails()
    {
        // Arrange
        var username = "admin";
        var password = "admin123";
        _options.PasswordPolicy.RequireMinimumLength = true;
        _options.PasswordPolicy.MinimumLength = 1;
        _options.PasswordPolicy.CheckUsernameVariations = true;

        // Act
        var errors = _validator.ValidatePassword(password, username);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password cannot be based on your username or common variations");
    }

    /// <summary>
    /// Tests that a password without lowercase letter fails when lowercase is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_NoLowercase_Fails()
    {
        // Arrange
        var password = "UPPERCASE123!";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = true,
            RequireUppercase = false,
            RequireDigit = false,
            RequireSpecialChar = false
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must contain at least one lowercase letter (a-z)");
    }

    /// <summary>
    /// Tests that a password without uppercase letter fails when uppercase is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_NoUppercase_Fails()
    {
        // Arrange
        var password = "lowercase123!";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = false,
            RequireUppercase = true,
            RequireDigit = false,
            RequireSpecialChar = false
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must contain at least one uppercase letter (A-Z)");
    }

    /// <summary>
    /// Tests that a password without digit fails when digit is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_NoDigit_Fails()
    {
        // Arrange
        var password = "NoDigitsHere!";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireDigit = true,
            RequireSpecialChar = false
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must contain at least one digit (0-9)");
    }

    /// <summary>
    /// Tests that a password without special character fails when special char is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_NoSpecialChar_Fails()
    {
        // Arrange
        var password = "NoSpecialChar123";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = false,
            RequireUppercase = false,
            RequireDigit = false,
            RequireSpecialChar = true
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must contain at least one special character (!@#$%^&* etc.)");
    }

    /// <summary>
    /// Tests that a password with all character classes passes when all are required.
    /// </summary>
    [Fact]
    public void ValidatePassword_AllCharacterClasses_Valid()
    {
        // Arrange
        var password = "StrongPass123!";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireDigit = true,
            RequireSpecialChar = true
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that multiple validation errors are all reported.
    /// </summary>
    [Fact]
    public void ValidatePassword_MultipleErrors_AllReported()
    {
        // Arrange
        var password = "short";
        var username = "john";
        _options.PasswordPolicy = new PasswordPolicyOptions
        {
            RequireMinimumLength = true,
            MinimumLength = 8,
            RequireLowercase = true,
            RequireUppercase = true,
            RequireDigit = true,
            RequireNotEqualToUsername = true,
            CheckUsernameVariations = false,
            CheckCommonPatterns = false,
            CheckAgainstHistory = false
        };
        _validator = new PasswordValidationService(_options, _options.PasswordPolicy);

        // Act
        var errors = _validator.ValidatePassword(password, username);

        // Assert
        errors.Should().HaveCount(5);
        errors.Should().Contain("Password must be at least 8 characters long");
        errors.Should().Contain("Password must contain at least one lowercase letter (a-z)");
        errors.Should().Contain("Password must contain at least one uppercase letter (A-Z)");
        errors.Should().Contain("Password must contain at least one digit (0-9)");
        errors.Should().Contain("Password cannot be the same as your username");
    }

    /// <summary>
    /// Tests that ValidateAndThrow throws exception with error message.
    /// </summary>
    [Fact]
    public void ValidateAndThrow_InvalidPassword_ThrowsWithErrorMessage()
    {
        // Arrange
        var password = "weak";
        var username = "testuser";
        _options.PasswordPolicy.MinimumLength = 8;
        _options.PasswordPolicy.RequireLowercase = true;

        // Act
        Action act = () => _validator.ValidateAndThrow(password, username);

        // Assert
        act.Should().Throw<AuthServerException>()
            .Where(ex => ex.ErrorCode == Constants.ErrorCodes.InvalidRequest)
            .And.Message.Should().Contain("Password must be at least 8 characters long")
            .And.Contain("Password must contain at least one lowercase letter (a-z)");
    }

    /// <summary>
    /// Tests that a valid password passes ValidateAndThrow without exception.
    /// </summary>
    [Fact]
    public void ValidateAndThrow_ValidPassword_NoException()
    {
        // Arrange
        var password = "ValidPass123!";
        var username = "testuser";
        _options.PasswordPolicy.MinimumLength = 8;
        _options.PasswordPolicy.RequireLowercase = true;
        _options.PasswordPolicy.RequireUppercase = true;
        _options.PasswordPolicy.RequireDigit = true;
        _options.PasswordPolicy.RequireSpecialChar = true;

        // Act
        Action act = () => _validator.ValidateAndThrow(password, username);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that common pattern detection works.
    /// </summary>
    [Fact]
    public void ValidatePassword_CommonPattern_Fails()
    {
        // Arrange
        var password = "Password123!";
        _options.PasswordPolicy.CheckCommonPatterns = true;

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password cannot contain common pattern: 'password'");
    }

    /// <summary>
    /// Tests that password history check works.
    /// </summary>
    [Fact]
    public void ValidatePassword_HistoryCheck_Fails()
    {
        // Arrange
        var password = "OldPass123!";
        _options.PasswordPolicy.CheckAgainstHistory = true;
        _options.PasswordPolicy.HistoryCheckPassword = "OldPass123!";

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must be different from your previous password");
    }

    /// <summary>
    /// Tests that maximum length validation works.
    /// </summary>
    [Fact]
    public void ValidatePassword_MaximumLength_Fails()
    {
        // Arrange
        var password = new string('a', 101);
        _options.PasswordPolicy.RequireMaximumLength = true;
        _options.PasswordPolicy.MaximumLength = 100;

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must be no more than 100 characters long");
    }

    /// <summary>
    /// Tests that default policy allows simple passwords when no requirements are set.
    /// </summary>
    [Fact]
    public void ValidatePassword_DefaultPolicy_AllowsSimplePasswords()
    {
        // Arrange
        var password = "simple";

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that password with only special chars (but no letters/digits) fails when special char is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_OnlySpecialChars_FailsWhenSpecialRequired()
    {
        // Arrange
        var password = "!!!@@@###";
        _options.PasswordPolicy.RequireSpecialChar = true;
        _options.PasswordPolicy.RequireLowercase = true;
        _options.PasswordPolicy.RequireUppercase = true;
        _options.PasswordPolicy.RequireDigit = true;

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().HaveCount(4);
    }

    /// <summary>
    /// Tests that password with only letters and digits (no special chars) fails when special char is required.
    /// </summary>
    [Fact]
    public void ValidatePassword_LettersAndDigits_FailsWhenSpecialRequired()
    {
        // Arrange
        var password = "NoSpecial123";
        _options.PasswordPolicy.RequireSpecialChar = true;

        // Act
        var errors = _validator.ValidatePassword(password);

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Be("Password must contain at least one special character (!@#$%^&* etc.)");
    }
}
