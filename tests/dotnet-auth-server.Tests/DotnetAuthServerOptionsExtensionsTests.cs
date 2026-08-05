using System;
using System.Collections.Generic;
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetAuthServer.Tests;

public class DotnetAuthServerOptionsExtensionsTests
{
    private static DotnetAuthServerOptions CreateValidOptions()
    {
        return new DotnetAuthServerOptions
        {
            AuthServer = new AuthServerOptions
            {
                IssuerUrl = "https://example.com",
                JwtSigningKey = "secret",
                AccessTokenLifetimeSeconds = 3600,
                RefreshTokenLifetimeSeconds = 7200,
                JwtAlgorithm = null,
                SupportedScopes = new List<string> { "read", "write" },
                SupportedGrantTypes = new List<string> { "authorization_code", "client_credentials" }
            },
            Cache = new CacheOptions
            {
                Backend = "Memory",
                DefaultExpirationSeconds = 300
            },
            Logging = new LoggingOptions
            {
                MinimumLevel = LogLevel.Information,
                LogSensitiveData = true
            },
            Opa = new OpaOptions
            {
                BaseUrl = "http://opa",
                PolicyPath = "/policy"
            }
        };
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidOptions()
    {
        var options = CreateValidOptions();
        Assert.True(options.IsValid());
    }

    [Fact]
    public void IsValid_ThrowsArgumentNullException_WhenOptionsNull()
    {
        Assert.Throws<ArgumentNullException>(() => DotnetAuthServerOptionsExtensions.IsValid(null!));
    }

    [Fact]
    public void GetEffectiveCacheBackend_NormalizesCase()
    {
        var options = CreateValidOptions();

        options.Cache.Backend = "memory";
        Assert.Equal("Memory", options.GetEffectiveCacheBackend());

        options.Cache.Backend = "REDIS";
        Assert.Equal("Redis", options.GetEffectiveCacheBackend());
    }

    [Fact]
    public void UsesRedisCache_ReturnsTrue_WhenBackendIsRedis()
    {
        var options = CreateValidOptions();
        options.Cache.Backend = "Redis";
        Assert.True(options.UsesRedisCache());
    }

    [Fact]
    public void GetEffectiveJwtAlgorithm_ReturnsDefault_WhenNullOrEmpty()
    {
        var options = CreateValidOptions();

        options.AuthServer.JwtAlgorithm = null;
        Assert.Equal("HS256", options.GetEffectiveJwtAlgorithm());

        options.AuthServer.JwtAlgorithm = string.Empty;
        Assert.Equal("HS256", options.GetEffectiveJwtAlgorithm());
    }

    [Fact]
    public void SupportsScope_ReturnsTrue_ForSupportedScope_IgnoresCase()
    {
        var options = CreateValidOptions();

        Assert.True(options.SupportsScope("READ"));
        Assert.False(options.SupportsScope("unknown"));
    }

    [Fact]
    public void SupportsScope_ThrowsArgumentException_WhenScopeNullOrWhiteSpace()
    {
        var options = CreateValidOptions();

        Assert.Throws<ArgumentException>(() => options.SupportsScope(null!));
        Assert.Throws<ArgumentException>(() => options.SupportsScope(" "));
    }

    [Fact]
    public void GetEffectiveMinimumLogLevel_ReturnsEnumName()
    {
        var options = CreateValidOptions();

        Assert.Equal("Information", options.GetEffectiveMinimumLogLevel());
    }

    [Fact]
    public void IsSensitiveDataLoggingEnabled_ReturnsConfiguredValue()
    {
        var options = CreateValidOptions();
        options.Logging.LogSensitiveData = false;

        Assert.False(options.IsSensitiveDataLoggingEnabled());
    }
}
