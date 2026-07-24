#nullable enable
using System;
using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class TotpCredentialTests
{
    [Fact]
    public void DefaultValues_AreInitializedCorrectly()
    {
        var credential = new TotpCredential();

        Assert.False(string.IsNullOrEmpty(credential.Id));
        Assert.Equal(DateTime.UtcNow, credential.CreatedAt, TimeSpan.FromSeconds(5));
        Assert.False(credential.IsEnabled);
        Assert.Null(credential.EnabledAt);
        Assert.Null(credential.LastUsedAt);
        Assert.Null(credential.LastAcceptedTimeStep);
        Assert.Empty(credential.BackupCodes);
    }

    [Fact]
    public void Enable_SetsIsEnabledAndEnabledAt()
    {
        var credential = new TotpCredential();

        credential.Enable();

        Assert.True(credential.IsEnabled);
        Assert.NotNull(credential.EnabledAt);
        Assert.Equal(DateTime.UtcNow, credential.EnabledAt.Value, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RecordVerification_UpdatesLastUsedAt()
    {
        var credential = new TotpCredential();

        credential.RecordVerification();

        Assert.NotNull(credential.LastUsedAt);
        Assert.Equal(DateTime.UtcNow, credential.LastUsedAt.Value, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RecordVerificationWithTimeStep_UpdatesLastUsedAtAndLastAcceptedTimeStep()
    {
        var credential = new TotpCredential();
        long timeStep = 123456L;

        credential.RecordVerification(timeStep);

        Assert.NotNull(credential.LastUsedAt);
        Assert.Equal(DateTime.UtcNow, credential.LastUsedAt.Value, TimeSpan.FromSeconds(5));
        Assert.Equal(timeStep, credential.LastAcceptedTimeStep);
    }

    [Fact]
    public void RecordVerificationWithNegativeTimeStep_ThrowsArgumentOutOfRangeException()
    {
        var credential = new TotpCredential();

        Assert.Throws<ArgumentOutOfRangeException>(() => credential.RecordVerification(-1));
    }

    [Fact]
    public void BackupCodes_ListCanBeModified()
    {
        var credential = new TotpCredential();

        credential.BackupCodes.Add("code1");
        credential.BackupCodes.Add("code2");

        Assert.Equal(2, credential.BackupCodes.Count);
        Assert.Contains("code1", credential.BackupCodes);
        Assert.Contains("code2", credential.BackupCodes);
    }

    [Fact]
    public void UserIdAndSecretKey_CanBeSetAndRetrieved()
    {
        var credential = new TotpCredential
        {
            UserId = "user123",
            SecretKey = "BASE32SECRET"
        };

        Assert.Equal("user123", credential.UserId);
        Assert.Equal("BASE32SECRET", credential.SecretKey);
    }
}
