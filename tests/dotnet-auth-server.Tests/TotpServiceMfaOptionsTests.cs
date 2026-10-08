#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using System.Buffers.Binary;
using System.Security.Cryptography;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests that <see cref="TotpService"/> honours the configured <see cref="MfaOptions"/> instead of fixed constants.
/// </summary>
public sealed class TotpServiceMfaOptionsTests
{
    private const string UserId = "user-1";
    private const string Username = "alice";

    [Fact]
    public async Task InitiateSetupAsync_ConfiguredRecoveryCodeCountAndLength_IssuesThoseCodes()
    {
        // Arrange
        var service = CreateService(new MfaOptions { RecoveryCodeCount = 3, RecoveryCodeLength = 9 });

        // Act
        var setup = await service.InitiateSetupAsync(UserId, Username);

        // Assert
        Assert.Equal(3, setup.BackupCodes.Count);
        Assert.All(setup.BackupCodes, code => Assert.Equal(9, code.Length));
        Assert.All(setup.BackupCodes, code => Assert.Matches("^[0-9A-F]+$", code));
    }

    [Fact]
    public void BuildProvisioningUri_ConfiguredIssuerDigitsAndTimeStep_AppearsInUri()
    {
        // Arrange
        var service = CreateService(new MfaOptions { Issuer = "Contoso", Digits = 8, TimeStepSeconds = 60 });

        // Act
        var uri = service.BuildProvisioningUri("GEZDGNBVGY3TQOJQ", Username);

        // Assert
        Assert.StartsWith("otpauth://totp/Contoso%3Aalice?", uri, StringComparison.Ordinal);
        Assert.Contains("issuer=Contoso", uri, StringComparison.Ordinal);
        Assert.Contains("digits=8", uri, StringComparison.Ordinal);
        Assert.Contains("period=60", uri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task VerifyTotpCodeAsync_EightDigitsConfigured_AcceptsCurrentEightDigitCode()
    {
        // Arrange
        const int timeStep = 60;
        var service = CreateService(new MfaOptions { Digits = 8, TimeStepSeconds = timeStep });
        var setup = await service.InitiateSetupAsync(UserId, Username);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / timeStep;
        var code = ComputeTotp(setup.SecretKey, counter, 8);

        // Act
        var accepted = await service.VerifyTotpCodeAsync(UserId, setup.SecretKey, code);

        // Assert
        Assert.True(accepted);
    }

    [Fact]
    public async Task VerifyTotpCodeAsync_EightDigitsConfigured_RejectsSixDigitCode()
    {
        // Arrange
        var service = CreateService(new MfaOptions { Digits = 8 });
        var setup = await service.InitiateSetupAsync(UserId, Username);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var sixDigitCode = ComputeTotp(setup.SecretKey, counter, 6);

        // Act
        var accepted = await service.VerifyTotpCodeAsync(UserId, setup.SecretKey, sixDigitCode);

        // Assert
        Assert.False(accepted);
    }

    [Fact]
    public async Task VerifyTotpCodeAsync_ZeroDriftAndNoClockSkew_RejectsPreviousStepCode()
    {
        // Arrange
        const int timeStep = 120;
        var authOptions = new AuthServerOptions { ClockSkewToleranceSeconds = 0 };
        var service = CreateService(new MfaOptions { AllowedDriftSteps = 0, TimeStepSeconds = timeStep }, authOptions);
        var setup = await service.InitiateSetupAsync(UserId, Username);
        var previousCounter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / timeStep - 1;
        var previousCode = ComputeTotp(setup.SecretKey, previousCounter, 6);

        // Act
        var accepted = await service.VerifyTotpCodeAsync(UserId, setup.SecretKey, previousCode);

        // Assert
        Assert.False(accepted);
    }

    private static TotpService CreateService(MfaOptions mfaOptions, AuthServerOptions? authOptions = null) =>
        new(
            new TotpCredentialRepository(),
            new UserRepository(),
            NullLogger<TotpService>.Instance,
            authOptions ?? new AuthServerOptions(),
            mfaOptions);

    /// <summary>
    /// Independent RFC 6238 / RFC 4226 implementation used to produce expected codes.
    /// </summary>
    private static string ComputeTotp(string base32Secret, long counter, int digits)
    {
        var key = TotpService.DecodeBase32(base32Secret);
        var counterBytes = new byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter);

        var hash = HMACSHA1.HashData(key, counterBytes);
        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));
    }
}
