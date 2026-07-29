using System.ComponentModel.DataAnnotations;
using Xunit;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Services;

namespace DotnetAuthServer.Tests;

public class AuthServerOptionsTests
{
    private AuthServerOptions CreateValidOptions()
    {
        return new AuthServerOptions
        {
            IssuerUrl = "https://example.com",
            JwtSigningKey = "super_secret_key_that_is_long_enough_32_chars!",
            DatabaseConnectionString = "Data Source=memory",
            PasswordPolicy = new PasswordPolicyOptions()
        };
    }

    [Fact]
    public void ValidOptions_PassesValidation()
    {
        // Arrange
        var options = CreateValidOptions();

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void IssuerUrl_InvalidUrl_FailsValidation()
    {
        // Arrange
        var options = CreateValidOptions();
        options.IssuerUrl = "not-a-valid-url";

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AuthServerOptions.IssuerUrl)));
    }

    [Fact]
    public void JwtSigningKey_TooShort_FailsValidation()
    {
        // Arrange
        var options = CreateValidOptions();
        options.JwtSigningKey = "short";

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AuthServerOptions.JwtSigningKey)));
    }

    [Fact]
    public void AccessTokenLifetimeSeconds_Zero_FailsValidation()
    {
        // Arrange
        var options = CreateValidOptions();
        options.AccessTokenLifetimeSeconds = 0;

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AuthServerOptions.AccessTokenLifetimeSeconds)));
    }

    [Fact]
    public void ClockSkewToleranceSeconds_Negative_FailsValidation()
    {
        // Arrange
        var options = CreateValidOptions();
        options.ClockSkewToleranceSeconds = -1;

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AuthServerOptions.ClockSkewToleranceSeconds)));
    }

    [Fact]
    public void MaxRefreshTokenGenerations_Zero_FailsValidation()
    {
        // Arrange
        var options = CreateValidOptions();
        options.MaxRefreshTokenGenerations = 0;

        // Act
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(options, new ValidationContext(options), validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, v => v.MemberNames.Contains(nameof(AuthServerOptions.MaxRefreshTokenGenerations)));
    }

    [Fact]
    public void Defaults_AreSetCorrectly()
    {
        // Arrange & Act
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://example.com",
            JwtSigningKey = "12345678901234567890123456789012",
            DatabaseConnectionString = "Data Source=memory"
        };

        // Assert
        Assert.Equal("HS256", options.JwtAlgorithm);
        Assert.Equal(3600, options.AccessTokenLifetimeSeconds);
        Assert.Equal(2592000, options.RefreshTokenLifetimeSeconds);
        Assert.Equal(300, options.AuthorizationCodeLifetimeSeconds);
        Assert.True(options.RequirePkceForAllClients);
        Assert.True(options.AutoRefreshTokenRotation);
        Assert.Equal(10, options.MaxRefreshTokenGenerations);
        Assert.Equal(300, options.ClockSkewToleranceSeconds);
    }
}
