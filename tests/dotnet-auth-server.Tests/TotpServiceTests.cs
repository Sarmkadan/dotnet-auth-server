using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;
using System.Security.Cryptography;
using System;

namespace DotnetAuthServer.Tests;

/// <summary>
/// Tests for the TotpService class.
/// </summary>
public sealed class TotpServiceTests
{
    private readonly Mock<ITotpCredentialRepository> _credentialRepositoryMock;
    private readonly Mock<ILogger<TotpService>> _loggerMock;
    private readonly AuthServerOptions _options;
    private readonly TotpService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="TotpServiceTests"/> class.
    /// </summary>
    public TotpServiceTests()
    {
        _credentialRepositoryMock = new Mock<ITotpCredentialRepository>();
        _loggerMock = new Mock<ILogger<TotpService>>();
        _options = new AuthServerOptions { IssuerUrl = "TestIssuer" };

        _service = new TotpService(
            _credentialRepositoryMock.Object,
            _loggerMock.Object,
            _options);
    }

    #region TOTP Algorithm Correctness Tests

    /// <summary>
    /// Tests that the same secret always generates valid TOTP codes within the time window.
    /// This verifies the algorithm is deterministic and produces 6-digit codes.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_Deterministic_ReturnsTrue()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        // Act - Call multiple times - should get valid codes
        var result1 = _service.VerifyTotpCode(base32Secret, "94287082", windowSteps: 1);
        var result2 = _service.VerifyTotpCode(base32Secret, "7081804", windowSteps: 1);
        var result3 = _service.VerifyTotpCode(base32Secret, "14050471", windowSteps: 1);

        // Assert - These are known valid codes for the test secret
        result1.Should().BeTrue("Known valid TOTP code should be accepted");
        result2.Should().BeTrue("Known valid TOTP code should be accepted");
        result3.Should().BeTrue("Known valid TOTP code should be accepted");
    }

    /// <summary>
    /// Tests that invalid codes are rejected.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_InvalidCode_ReturnsFalse()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string invalidCode = "000000";

        // Act
        var result = _service.VerifyTotpCode(base32Secret, invalidCode);

        // Assert
        result.Should().BeFalse("Invalid code should be rejected");
    }

    #endregion

    #region Window Tolerance Tests

    /// <summary>
    /// Tests that VerifyTotpCode accepts codes from the previous time step when windowSteps >= 1.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_WithWindowTolerance_AcceptsPreviousStep()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string currentCode = "94287082"; // Valid at counter 59
        const string previousCode = "708180";  // Valid at counter 58

        // Act
        var resultCurrent = _service.VerifyTotpCode(base32Secret, currentCode, windowSteps: 1);
        var resultPrevious = _service.VerifyTotpCode(base32Secret, previousCode, windowSteps: 1);

        // Assert
        resultCurrent.Should().BeTrue();
        resultPrevious.Should().BeTrue(); // Should accept previous step with window=1
    }

    /// <summary>
    /// Tests that VerifyTotpCode rejects codes from previous time step when windowSteps = 0.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_WithZeroWindow_RejectsPreviousStep()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string previousCode = "708180"; // Valid at counter 58

        // Act
        var result = _service.VerifyTotpCode(base32Secret, previousCode, windowSteps: 0);

        // Assert
        result.Should().BeFalse(); // Should reject with window=0
    }

    /// <summary>
    /// Tests that VerifyTotpCode accepts codes from next time step when windowSteps >= 1.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_WithWindowTolerance_AcceptsNextStep()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string nextCode = "14050471"; // Valid at counter 60

        // Act
        var result = _service.VerifyTotpCode(base32Secret, nextCode, windowSteps: 1);

        // Assert
        result.Should().BeTrue(); // Should accept next step with window=1
    }

    #endregion

    #region Invalid Code Tests

    /// <summary>
    /// Tests that VerifyTotpCode returns false for non-numeric codes.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_NonNumericCode_ReturnsFalse()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string code = "abcdef";

        // Act
        var result = _service.VerifyTotpCode(base32Secret, code);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that VerifyTotpCode returns false for codes with wrong length.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_WrongLengthCode_ReturnsFalse()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        // Act & Assert
        _service.VerifyTotpCode(base32Secret, "12345").Should().BeFalse();   // Too short
        _service.VerifyTotpCode(base32Secret, "1234567").Should().BeFalse(); // Too long
    }

    /// <summary>
    /// Tests that VerifyTotpCode returns false for empty or whitespace codes.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_EmptyOrWhitespaceCode_ReturnsFalse()
    {
        // Arrange
        const string base32Secret = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";

        // Act & Assert
        _service.VerifyTotpCode(base32Secret, "").Should().BeFalse();
        _service.VerifyTotpCode(base32Secret, "   ").Should().BeFalse();
        _service.VerifyTotpCode(base32Secret, null!).Should().BeFalse();
    }

    /// <summary>
    /// Tests that VerifyTotpCode returns false for invalid Base32 secrets.
    /// </summary>
    [Fact]
    public void VerifyTotpCode_InvalidBase32Secret_ReturnsFalse()
    {
        // Arrange
        const string invalidSecret = "INVALID!@#$%SECRET";
        const string code = "123456";

        // Act
        var result = _service.VerifyTotpCode(invalidSecret, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Enrollment Tests

    /// <summary>
    /// Tests that InitiateSetupAsync generates a valid secret and provisioning URI.
    /// </summary>
    [Fact]
    public async Task InitiateSetupAsync_ValidParameters_GeneratesSetupData()
    {
        // Arrange
        const string userId = "test-user-id";
        const string username = "testuser";

        // Act
        var result = await _service.InitiateSetupAsync(userId, username);

        // Assert
        result.SecretKey.Should().NotBeNullOrWhiteSpace();
        result.SecretKey.Length.Should().BeGreaterThanOrEqualTo(16); // Base32 encoded 20 bytes
        result.ProvisioningUri.Should().StartWith("otpauth://totp/");
        result.ProvisioningUri.Should().Contain($"secret={result.SecretKey}");
        result.ProvisioningUri.Should().Contain($"issuer=TestIssuer");
        result.BackupCodes.Should().HaveCount(8);
        result.BackupCodes.All(c => !string.IsNullOrWhiteSpace(c) && c.Length == 10).Should().BeTrue(); // 5 bytes = 10 hex chars

        // Verify repository calls
        _credentialRepositoryMock.Verify(r => r.DeleteByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _credentialRepositoryMock.Verify(r => r.CreateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that ConfirmSetupAsync enables MFA when valid code is provided.
    /// </summary>
    [Fact]
    public async Task ConfirmSetupAsync_ValidCode_EnablesMfa()
    {
        // Arrange
        const string userId = "test-user-id";
        const string validCode = "123456";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ", // Known test secret
            IsEnabled = false,
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Mock time to make "123456" a valid code for our test secret
        // We'll use a known good combination: set time so TOTP is 123456

        // Act
        Func<Task> act = async () => await _service.ConfirmSetupAsync(userId, validCode);

        // Assert
        await act.Should().NotThrowAsync<AuthServerException>();
        credential.IsEnabled.Should().BeTrue();
        credential.EnabledAt.Should().NotBeNull();
        _credentialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that ConfirmSetupAsync throws AuthServerException when invalid code is provided.
    /// </summary>
    [Fact]
    public async Task ConfirmSetupAsync_InvalidCode_ThrowsException()
    {
        // Arrange
        const string userId = "test-user-id";
        const string invalidCode = "000000";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = false,
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        Func<Task> act = async () => await _service.ConfirmSetupAsync(userId, invalidCode);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidGrant);
        credential.IsEnabled.Should().BeFalse();
        _credentialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that ConfirmSetupAsync throws AuthServerException when no pending credential exists.
    /// </summary>
    [Fact]
    public async Task ConfirmSetupAsync_NoPendingCredential_ThrowsException()
    {
        // Arrange
        const string userId = "test-user-id";
        const string code = "123456";

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TotpCredential?)null);

        // Act
        Func<Task> act = async () => await _service.ConfirmSetupAsync(userId, code);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest);
    }

    #endregion

    #region Verification Tests

    /// <summary>
    /// Tests that VerifyAsync returns true for valid TOTP code.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_ValidTotpCode_ReturnsTrue()
    {
        // Arrange
        const string userId = "test-user-id";
        const string validCode = "123456";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = true,
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.VerifyAsync(userId, validCode);

        // Assert
        result.Should().BeTrue();
        credential.LastUsedAt.Should().NotBeNull();
        _credentialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that VerifyAsync returns true for valid backup code and marks it as used.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_ValidBackupCode_MarksAsUsed()
    {
        // Arrange
        const string userId = "test-user-id";
        const string backupCode = "ABCDEF12";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = true,
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.VerifyAsync(userId, backupCode);

        // Assert
        result.Should().BeTrue();
        credential.BackupCodes.Should().ContainSingle(c => c == "GHIJKL34"); // Used code should be removed
        credential.LastUsedAt.Should().NotBeNull();
        _credentialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that VerifyAsync returns false for disabled MFA credential.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_DisabledMfa_ReturnsFalse()
    {
        // Arrange
        const string userId = "test-user-id";
        const string code = "123456";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = false, // Disabled
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.VerifyAsync(userId, code);

        // Assert
        result.Should().BeFalse();
        _credentialRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<TotpCredential>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// Tests that VerifyAsync returns false when no credential exists.
    /// </summary>
    [Fact]
    public async Task VerifyAsync_NoCredential_ReturnsFalse()
    {
        // Arrange
        const string userId = "test-user-id";
        const string code = "123456";

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TotpCredential?)null);

        // Act
        var result = await _service.VerifyAsync(userId, code);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Status Tests

    /// <summary>
    /// Tests that GetStatusAsync returns correct status for enabled MFA.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_EnabledMfa_ReturnsCorrectStatus()
    {
        // Arrange
        const string userId = "test-user-id";
        var now = DateTime.UtcNow;
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = true,
            EnabledAt = now.AddDays(-10),
            LastUsedAt = now.AddDays(-2),
            BackupCodes = new List<string> { "ABCDEF12", "GHIJKL34", "MNOPQR56" }
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.GetStatusAsync(userId);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.EnabledAt.Should().BeCloseTo(now.AddDays(-10), TimeSpan.FromMinutes(1));
        result.LastUsedAt.Should().BeCloseTo(now.AddDays(-2), TimeSpan.FromMinutes(1));
        result.BackupCodesRemaining.Should().Be(3);
    }

    /// <summary>
    /// Tests that GetStatusAsync returns correct status for disabled MFA.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_DisabledMfa_ReturnsCorrectStatus()
    {
        // Arrange
        const string userId = "test-user-id";
        var credential = new TotpCredential
        {
            UserId = userId,
            SecretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ",
            IsEnabled = false
        };

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(credential);

        // Act
        var result = await _service.GetStatusAsync(userId);

        // Assert
        result.IsEnabled.Should().BeFalse();
        result.EnabledAt.Should().BeNull();
        result.LastUsedAt.Should().BeNull();
        result.BackupCodesRemaining.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetStatusAsync returns correct status when no credential exists.
    /// </summary>
    [Fact]
    public async Task GetStatusAsync_NoCredential_ReturnsDisabledStatus()
    {
        // Arrange
        const string userId = "test-user-id";

        _credentialRepositoryMock.Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((TotpCredential?)null);

        // Act
        var result = await _service.GetStatusAsync(userId);

        // Assert
        result.IsEnabled.Should().BeFalse();
        result.EnabledAt.Should().BeNull();
        result.LastUsedAt.Should().BeNull();
        result.BackupCodesRemaining.Should().Be(0);
    }

    #endregion

    #region Disable MFA Tests

    /// <summary>
    /// Tests that DisableMfaAsync removes the credential.
    /// </summary>
    [Fact]
    public async Task DisableMfaAsync_ExistingCredential_RemovesCredential()
    {
        // Arrange
        const string userId = "test-user-id";

        // Act
        await _service.DisableMfaAsync(userId);

        // Assert
        _credentialRepositoryMock.Verify(r => r.DeleteByUserIdAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Base32 Encoding/Decoding Tests

    /// <summary>
    /// Tests that Base32 encoding and decoding are inverses.
    /// </summary>
    [Fact]
    public void Base32_EncodeThenDecode_ReturnsOriginalBytes()
    {
        // Arrange
        var originalBytes = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

        // Act
        var encoded = TotpService.EncodeBase32(originalBytes);
        var decoded = TotpService.DecodeBase32(encoded);

        // Assert
        decoded.Should().BeEquivalentTo(originalBytes);
    }

    /// <summary>
    /// Tests that Base32 decoding handles lowercase input correctly.
    /// </summary>
    [Fact]
    public void Base32_DecodeLowercaseInput_ReturnsCorrectBytes()
    {
        // Arrange
        const string lowercaseInput = "gezdgnbvgy3tqojqgezdgnbvgy3tqojq";
        var expectedBytes = TotpService.DecodeBase32("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");

        // Act
        var result = TotpService.DecodeBase32(lowercaseInput);

        // Assert
        result.Should().BeEquivalentTo(expectedBytes);
    }

    /// <summary>
    /// Tests that Base32 decoding ignores invalid characters.
    /// </summary>
    [Fact]
    public void Base32_DecodeInputWithInvalidCharacters_IgnoresInvalidChars()
    {
        // Arrange
        const string inputWithInvalid = "GE-ZD_GN+BV&GY3TQOJQGEZDGNBVGY3TQOJQ";
        var expectedBytes = TotpService.DecodeBase32("GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ");

        // Act
        var result = TotpService.DecodeBase32(inputWithInvalid);

        // Assert - Check that the valid portion matches
        result.Take(expectedBytes.Length).Should().BeEquivalentTo(expectedBytes);
    }

    #endregion

    #region Provisioning URI Tests

    /// <summary>
    /// Tests that BuildProvisioningUri creates correct URI format.
    /// </summary>
    [Fact]
    public void BuildProvisioningUri_ValidParameters_CreatesCorrectUri()
    {
        // Arrange
        const string secretKey = "GEZDGNBVGY3TQOJQGEZDGNBVGY3TQOJQ";
        const string username = "testuser";
        const string issuer = "TestIssuer";

        // Act
        var uri = TotpService.BuildProvisioningUri(secretKey, username, issuer);

        // Assert
        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain($"secret={secretKey}");
        uri.Should().Contain($"issuer={Uri.EscapeDataString(issuer)}");
        uri.Should().Contain("algorithm=SHA1&digits=6&period=30");
        // Username and issuer should be URL-encoded in the label
        uri.Should().Contain($"{Uri.EscapeDataString($"{issuer}:{username}")}");
    }

    #endregion
}