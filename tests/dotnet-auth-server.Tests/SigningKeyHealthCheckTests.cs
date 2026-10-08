#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Diagnostics;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class SigningKeyHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ValidKey_ReturnsHealthyWithoutKeyMaterial()
    {
        // Arrange
        const string key = "unit-test-signing-key-material-0123456789";
        var check = new SigningKeyHealthCheck(new AuthServerOptions { JwtSigningKey = key });

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.DoesNotContain(key, result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_MissingKey_ReturnsUnhealthy()
    {
        // Arrange
        var check = new SigningKeyHealthCheck(new AuthServerOptions { JwtSigningKey = "   " });

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_KeyBelowMinimumLength_ReturnsUnhealthy()
    {
        // Arrange
        var check = new SigningKeyHealthCheck(new AuthServerOptions { JwtSigningKey = "too-short" });

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
    }
}
