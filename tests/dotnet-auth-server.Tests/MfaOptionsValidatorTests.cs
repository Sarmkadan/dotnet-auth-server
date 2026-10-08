#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using Xunit;

/// <summary>
/// Tests for <see cref="MfaOptionsValidator"/>: each rule accepts its boundary values and
/// rejects values outside the range, naming the configuration key in the failure.
/// </summary>
public sealed class MfaOptionsValidatorTests
{
    private const string KeyPrefix = "DotnetAuthServer:Mfa:";

    [Fact]
    public void Validate_DefaultOptions_Succeeds()
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions());

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_BlankIssuer_FailsNamingIssuerKey(string? issuer)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { Issuer = issuer! });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f => f.StartsWith(KeyPrefix + "Issuer", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(8)]
    public void Validate_AllowedDigits_Succeeds(int digits)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { Digits = digits });

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(7)]
    [InlineData(9)]
    public void Validate_DigitsOtherThanSixOrEight_FailsNamingDigitsKeyAndRange(int digits)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { Digits = digits });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.StartsWith(KeyPrefix + "Digits", StringComparison.Ordinal) && f.Contains("6 or 8"));
    }

    [Theory]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(120)]
    public void Validate_TimeStepWithinRange_Succeeds(int seconds)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { TimeStepSeconds = seconds });

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(121)]
    [InlineData(-30)]
    public void Validate_TimeStepOutsideRange_FailsNamingTimeStepKeyAndRange(int seconds)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { TimeStepSeconds = seconds });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.StartsWith(KeyPrefix + "TimeStepSeconds", StringComparison.Ordinal) && f.Contains("15 and 120"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Validate_AllowedDriftWithinRange_Succeeds(int steps)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { AllowedDriftSteps = steps });

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(3)]
    [InlineData(10)]
    public void Validate_AllowedDriftOutsideRange_FailsNamingDriftKeyAndRange(int steps)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { AllowedDriftSteps = steps });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.StartsWith(KeyPrefix + "AllowedDriftSteps", StringComparison.Ordinal) && f.Contains("0 and 2"));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(20)]
    public void Validate_RecoveryCodeCountWithinRange_Succeeds(int count)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { RecoveryCodeCount = count });

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    [InlineData(-1)]
    public void Validate_RecoveryCodeCountOutsideRange_FailsNamingCountKeyAndRange(int count)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { RecoveryCodeCount = count });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.StartsWith(KeyPrefix + "RecoveryCodeCount", StringComparison.Ordinal) && f.Contains("1 and 20"));
    }

    [Theory]
    [InlineData(8)]
    [InlineData(16)]
    public void Validate_RecoveryCodeLengthAtOrAboveMinimum_Succeeds(int length)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { RecoveryCodeLength = length });

        // Assert
        Assert.False(result.Failed);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(1)]
    [InlineData(0)]
    public void Validate_RecoveryCodeLengthBelowMinimum_FailsNamingLengthKey(int length)
    {
        // Arrange
        var validator = new MfaOptionsValidator();

        // Act
        var result = validator.Validate(null, new MfaOptions { RecoveryCodeLength = length });

        // Assert
        Assert.True(result.Failed);
        Assert.Contains(result.Failures, f =>
            f.StartsWith(KeyPrefix + "RecoveryCodeLength", StringComparison.Ordinal) && f.Contains("at least 8"));
    }

    [Fact]
    public void Validate_MultipleInvalidValues_ReportsEveryFailure()
    {
        // Arrange
        var validator = new MfaOptionsValidator();
        var options = new MfaOptions { Issuer = "", Digits = 7, TimeStepSeconds = 5, AllowedDriftSteps = 9 };

        // Act
        var result = validator.Validate(null, options);

        // Assert
        Assert.True(result.Failed);
        Assert.Equal(4, result.Failures.Count());
    }
}
