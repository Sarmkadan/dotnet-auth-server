using System;
using System.Linq;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserStoreOptionsValidatorTests
{
    [Fact]
    public void Validate_DefaultSettingsWithInMemoryStorage_Succeeds()
    {
        // Arrange
        var validator = new UserStoreOptionsValidator(CreateAuthServerOptions());
        var options = new UserStoreOptions();

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(99_999)]
    public void Validate_IterationsBelowMinimum_FailsNamingIterationsKey(int iterations)
    {
        // Arrange
        var validator = new UserStoreOptionsValidator(CreateAuthServerOptions());
        var options = new UserStoreOptions { PasswordHashing = new PasswordHashingOptions { Iterations = iterations } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing:Iterations"));
    }

    [Fact]
    public void Validate_IterationsAboveMaximum_FailsNamingIterationsKey()
    {
        // Arrange
        var validator = new UserStoreOptionsValidator(CreateAuthServerOptions());
        var options = new UserStoreOptions
        {
            PasswordHashing = new PasswordHashingOptions { Iterations = UserStoreOptionsValidator.MaxPbkdf2Iterations + 1 }
        };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing:Iterations"));
    }

    [Fact]
    public void Validate_UnsupportedAlgorithm_FailsNamingAlgorithmKey()
    {
        // Arrange
        var validator = new UserStoreOptionsValidator(CreateAuthServerOptions());
        var options = new UserStoreOptions { PasswordHashing = new PasswordHashingOptions { Algorithm = "Argon2id" } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing:Algorithm") && f.Contains("Argon2id"));
    }

    [Fact]
    public void Validate_BlankAlgorithm_FailsNamingAlgorithmKey()
    {
        // Arrange
        var validator = new UserStoreOptionsValidator(CreateAuthServerOptions());
        var options = new UserStoreOptions { PasswordHashing = new PasswordHashingOptions { Algorithm = "  " } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing:Algorithm"));
    }

    [Fact]
    public void ValidatePasswordHashing_NullHashing_FailsNamingSectionKey()
    {
        // Arrange
        PasswordHashingOptions? hashing = null;

        // Act
        var errors = UserStoreOptionsValidator.ValidatePasswordHashing(hashing);

        // Assert
        Assert.Single(errors);
        Assert.Contains("DotnetAuthServer:UserStore:PasswordHashing", errors[0]);
    }

    [Fact]
    public void ValidatePasswordHashing_MinimumIterationsAndPbkdf2_Succeeds()
    {
        // Arrange
        var hashing = new PasswordHashingOptions
        {
            Algorithm = "pbkdf2",
            Iterations = UserStoreOptionsValidator.MinPbkdf2Iterations
        };

        // Act
        var errors = UserStoreOptionsValidator.ValidatePasswordHashing(hashing);

        // Assert
        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_NonPositiveFailedLoginThreshold_FailsNamingThresholdKey(int threshold)
    {
        // Arrange
        var authServer = CreateAuthServerOptions();
        authServer.FailedLoginAttemptThreshold = threshold;
        var validator = new UserStoreOptionsValidator(authServer);

        // Act
        var result = validator.Validate(null, new UserStoreOptions());

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:AuthServer:FailedLoginAttemptThreshold"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_NonPositiveLockoutDuration_FailsNamingLockoutKey(int minutes)
    {
        // Arrange
        var authServer = CreateAuthServerOptions();
        authServer.AccountLockoutDurationMinutes = minutes;
        var validator = new UserStoreOptionsValidator(authServer);

        // Act
        var result = validator.Validate(null, new UserStoreOptions());

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.Contains("DotnetAuthServer:AuthServer:AccountLockoutDurationMinutes"));
    }

    [Fact]
    public void Validate_PersistentStorageRequested_FailsNamingStorageKeys()
    {
        // Arrange
        var authServer = CreateAuthServerOptions();
        authServer.UseInMemoryDatabase = false;
        authServer.DatabaseConnectionString = "Server=db;Database=auth";
        var validator = new UserStoreOptionsValidator(authServer);

        // Act
        var result = validator.Validate(null, new UserStoreOptions());

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.Contains("DotnetAuthServer:AuthServer:UseInMemoryDatabase") &&
            f.Contains("DotnetAuthServer:AuthServer:DatabaseConnectionString"));
    }

    [Fact]
    public void Validate_MultipleProblems_ReportsEveryOffendingKey()
    {
        // Arrange
        var authServer = CreateAuthServerOptions();
        authServer.FailedLoginAttemptThreshold = 0;
        authServer.AccountLockoutDurationMinutes = 0;
        var validator = new UserStoreOptionsValidator(authServer);
        var options = new UserStoreOptions { PasswordHashing = new PasswordHashingOptions { Iterations = 1 } };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Equal(3, result.Failures.Count());
    }

    [Fact]
    public void Constructor_NullAuthServerOptions_ThrowsArgumentNullException()
    {
        // Arrange
        AuthServerOptions authServer = null!;

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => new UserStoreOptionsValidator(authServer));

        // Assert
        Assert.Equal("authServerOptions", exception.ParamName);
    }

    private static AuthServerOptions CreateAuthServerOptions() => new()
    {
        UseInMemoryDatabase = true,
        FailedLoginAttemptThreshold = 5,
        AccountLockoutDurationMinutes = 15
    };
}
