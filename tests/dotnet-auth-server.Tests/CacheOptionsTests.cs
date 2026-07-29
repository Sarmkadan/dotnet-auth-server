using System;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class CacheOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeAsDefined()
    {
        var options = new CacheOptions();

        Assert.True(options.Enabled);
        Assert.Equal("Memory", options.Backend);
        Assert.Equal(3600, options.DefaultExpirationSeconds);
        Assert.Equal(10000, options.MaxEntries);
        Assert.Equal(300, options.ExpirationScanIntervalSeconds);
        Assert.Null(options.ConnectionString);

        var expirations = options.ItemExpirations;
        Assert.NotNull(expirations);
        Assert.Equal(3600, expirations.ClientSeconds);
        Assert.Equal(1800, expirations.UserSeconds);
        Assert.Equal(7200, expirations.ScopeSeconds);
        Assert.Equal(300, expirations.GrantSeconds);
        Assert.Equal(86400, expirations.JwksSeconds);
    }

    [Fact]
    public void SettingProperties_ShouldPersistValues()
    {
        var options = new CacheOptions
        {
            Enabled = false,
            Backend = "Redis",
            DefaultExpirationSeconds = 120,
            MaxEntries = 5000,
            ExpirationScanIntervalSeconds = 60,
            ConnectionString = "redis://localhost",
            ItemExpirations = new CacheItemExpirations
            {
                ClientSeconds = 600,
                UserSeconds = 300,
                ScopeSeconds = 900,
                GrantSeconds = 150,
                JwksSeconds = 43200
            }
        };

        Assert.False(options.Enabled);
        Assert.Equal("Redis", options.Backend);
        Assert.Equal(120, options.DefaultExpirationSeconds);
        Assert.Equal(5000, options.MaxEntries);
        Assert.Equal(60, options.ExpirationScanIntervalSeconds);
        Assert.Equal("redis://localhost", options.ConnectionString);

        var expirations = options.ItemExpirations;
        Assert.NotNull(expirations);
        Assert.Equal(600, expirations.ClientSeconds);
        Assert.Equal(300, expirations.UserSeconds);
        Assert.Equal(900, expirations.ScopeSeconds);
        Assert.Equal(150, expirations.GrantSeconds);
        Assert.Equal(43200, expirations.JwksSeconds);
    }

    [Fact]
    public void ItemExpirations_Defaults_ShouldMatch()
    {
        var expirations = new CacheItemExpirations();

        Assert.Equal(3600, expirations.ClientSeconds);
        Assert.Equal(1800, expirations.UserSeconds);
        Assert.Equal(7200, expirations.ScopeSeconds);
        Assert.Equal(300, expirations.GrantSeconds);
        Assert.Equal(86400, expirations.JwksSeconds);
    }

    [Fact]
    public void ItemExpirations_SetValues_ShouldPersist()
    {
        var expirations = new CacheItemExpirations
        {
            ClientSeconds = 100,
            UserSeconds = 200,
            ScopeSeconds = 300,
            GrantSeconds = 400,
            JwksSeconds = 500
        };

        Assert.Equal(100, expirations.ClientSeconds);
        Assert.Equal(200, expirations.UserSeconds);
        Assert.Equal(300, expirations.ScopeSeconds);
        Assert.Equal(400, expirations.GrantSeconds);
        Assert.Equal(500, expirations.JwksSeconds);
    }

    [Fact]
    public void BoundaryValues_ShouldBeAccepted()
    {
        var options = new CacheOptions
        {
            DefaultExpirationSeconds = int.MaxValue,
            MaxEntries = int.MaxValue,
            ExpirationScanIntervalSeconds = int.MaxValue,
            ItemExpirations = new CacheItemExpirations
            {
                ClientSeconds = int.MaxValue,
                UserSeconds = int.MaxValue,
                ScopeSeconds = int.MaxValue,
                GrantSeconds = int.MaxValue,
                JwksSeconds = int.MaxValue
            }
        };

        Assert.Equal(int.MaxValue, options.DefaultExpirationSeconds);
        Assert.Equal(int.MaxValue, options.MaxEntries);
        Assert.Equal(int.MaxValue, options.ExpirationScanIntervalSeconds);

        var expirations = options.ItemExpirations;
        Assert.Equal(int.MaxValue, expirations.ClientSeconds);
        Assert.Equal(int.MaxValue, expirations.UserSeconds);
        Assert.Equal(int.MaxValue, expirations.ScopeSeconds);
        Assert.Equal(int.MaxValue, expirations.GrantSeconds);
        Assert.Equal(int.MaxValue, expirations.JwksSeconds);
    }

    [Fact]
    public void NullConnectionString_ShouldBeAllowed()
    {
        var options = new CacheOptions
        {
            ConnectionString = null
        };

        Assert.Null(options.ConnectionString);
    }
}
