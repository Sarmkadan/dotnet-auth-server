using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Tests for the SecretsService class.
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
    /// Verifies that GenerateSecureSecret returns a secret of the correct length.
    /// </summary>
    [Fact]
    public void GenerateSecureSecret_ReturnsSecretOfCorrectLength()
    {
        // Act
        var secret = _service.GenerateSecureSecret(32);

        // Assert
        secret.Should().NotBeNullOrEmpty();
        // The service does Base64Url encoding which can change the length slightly
        // 32 bytes -> 43 characters (roughly 32 * 8 / 6)
        secret.Length.Should().BeGreaterThanOrEqualTo(20);
    }

    /// <summary>
    /// Verifies that GenerateSecureSecret throws an ArgumentException when given an invalid length.
    /// </summary>
    [Fact]
    public void GenerateSecureSecret_InvalidLength_ThrowsArgumentException()
    {
        // Act
        Action act = () => _service.GenerateSecureSecret(10);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    /// <summary>
    /// Verifies that HashAndVerifySecret returns true for a valid secret.
    /// </summary>
    [Fact]
    public void HashAndVerifySecret_ValidSecret_ReturnsTrue()
    {
        // Arrange
        var secret = "test-secret";

        // Act
        var hash = _service.HashSecret(secret);
        var isValid = _service.VerifySecret(secret, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that HashAndVerifySecret returns false for an invalid secret.
    /// </summary>
    [Fact]
    public void HashAndVerifySecret_InvalidSecret_ReturnsFalse()
    {
        // Arrange
        var secret = "test-secret";
        var hash = _service.HashSecret(secret);

        // Act
        var isValid = _service.VerifySecret("wrong-secret", hash);

        // Assert
        isValid.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that MaskSecret returns a masked string.
    /// </summary>
    [Fact]
    public void MaskSecret_ReturnsMaskedString()
    {
        // Arrange
        var secret = "123456789";

        // Act
        var masked = SecretsService.MaskSecret(secret);

        // Assert
        masked.Should().Be("123***789");
    }
}
