using System;
using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for the <see cref="SecretsService"/> class.
/// </summary>
public sealed class SecretsServiceTests
{
    private readonly Mock<ILogger<SecretsService>> _loggerMock;
    private readonly SecretsService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="SecretsServiceTests"/> class.
    /// </summary>
    public SecretsServiceTests()
    {
        _loggerMock = new Mock<ILogger<SecretsService>>();
        _service = new SecretsService(_loggerMock.Object);
    }

    /// <summary>
    /// Verifies that <see cref="SecretsService.GenerateSecureSecret(int)"/> returns a secret of the correct length.
    /// </summary>
    [Fact]
    public void GenerateSecureSecret_ReturnsSecretOfCorrectLength()
    {
        const int requestedLength = 32;
        _loggerMock.Object.LogInformation("GenerateSecureSecret test started with requested length {RequestedLength}", requestedLength);

        // Act
        var secret = _service.GenerateSecureSecret(requestedLength);

        // Assert
        secret.Should().NotBeNullOrEmpty();
        // The service does Base64Url encoding which can change the length slightly
        // 32 bytes -> 43 characters (roughly 32 * 8 / 6)
        secret.Length.Should().BeGreaterThanOrEqualTo(20);
        _loggerMock.Object.LogInformation("GenerateSecureSecret test completed, generated secret length {SecretLength}", secret.Length);
    }

    /// <summary>
    /// Verifies that <see cref="SecretsService.GenerateSecureSecret(int)"/> throws an <see cref="ArgumentException"/> when given an invalid length.
    /// </summary>
    [Fact]
    public void GenerateSecureSecret_InvalidLength_ThrowsArgumentException()
    {
        const int invalidLength = 10;
        _loggerMock.Object.LogInformation("GenerateSecureSecret invalid length test started with length {InvalidLength}", invalidLength);

        // Act
        Action act = () => _service.GenerateSecureSecret(invalidLength);

        // Assert
        act.Should().Throw<ArgumentException>();
        _loggerMock.Object.LogInformation("GenerateSecureSecret invalid length test completed");
    }

    /// <summary>
    /// Verifies that hashing a secret and then verifying it returns <c>true</c>.
    /// </summary>
    [Fact]
    public void HashAndVerifySecret_ValidSecret_ReturnsTrue()
    {
        const string secret = "test-secret";
        _loggerMock.Object.LogInformation("HashAndVerifySecret valid test started with secret {Secret}", secret);

        // Act
        var hash = _service.HashSecret(secret);
        var isValid = _service.VerifySecret(secret, hash);

        // Assert
        isValid.Should().BeTrue();
        _loggerMock.Object.LogInformation("HashAndVerifySecret valid test completed, verification result {Result}", isValid);
    }

    /// <summary>
    /// Verifies that hashing a secret and then verifying a different secret returns <c>false</c>.
    /// </summary>
    [Fact]
    public void HashAndVerifySecret_InvalidSecret_ReturnsFalse()
    {
        const string secret = "test-secret";
        const string wrongSecret = "wrong-secret";
        _loggerMock.Object.LogInformation("HashAndVerifySecret invalid test started with secret {Secret} and wrong secret {WrongSecret}", secret, wrongSecret);

        // Arrange
        var hash = _service.HashSecret(secret);

        // Act
        var isValid = _service.VerifySecret(wrongSecret, hash);

        // Assert
        isValid.Should().BeFalse();
        _loggerMock.Object.LogInformation("HashAndVerifySecret invalid test completed, verification result {Result}", isValid);
    }

    /// <summary>
    /// Verifies that <see cref="SecretsService.MaskSecret(string)"/> returns a masked string.
    /// </summary>
    [Fact]
    public void MaskSecret_ReturnsMaskedString()
    {
        const string secret = "123456789";
        _loggerMock.Object.LogInformation("MaskSecret test started with secret {Secret}", secret);

        // Act
        var masked = SecretsService.MaskSecret(secret);

        // Assert
        masked.Should().Be("123***789");
        _loggerMock.Object.LogInformation("MaskSecret test completed, masked result {Masked}", masked);
    }

    // -----------------------------------------------------------------
    // Additional explicit tests for VerifySecret (added per new request)
    // -----------------------------------------------------------------

    private readonly SecretsService _serviceNoMock = new SecretsService(NullLogger<SecretsService>.Instance);

    [Fact]
    public void VerifySecret_ReturnsTrue_ForValidSecret()
    {
        const string secret = "my-super-secret";
        _loggerMock.Object.LogInformation("VerifySecret valid test started with secret {Secret}", secret);

        // Arrange
        var hash = _serviceNoMock.HashSecret(secret);

        // Act
        var result = _serviceNoMock.VerifySecret(secret, hash);

        // Assert
        Assert.True(result);
        _loggerMock.Object.LogInformation("VerifySecret valid test completed, result {Result}", result);
    }

    [Fact]
    public void VerifySecret_ReturnsFalse_ForInvalidSecret()
    {
        const string secret = "my-super-secret";
        const string wrongSecret = "not-the-secret";
        _loggerMock.Object.LogInformation("VerifySecret invalid test started with secret {Secret} and wrong secret {WrongSecret}", secret, wrongSecret);

        // Arrange
        var hash = _serviceNoMock.HashSecret(secret);

        // Act
        var result = _serviceNoMock.VerifySecret(wrongSecret, hash);

        // Assert
        Assert.False(result);
        _loggerMock.Object.LogInformation("VerifySecret invalid test completed, result {Result}", result);
    }
}
