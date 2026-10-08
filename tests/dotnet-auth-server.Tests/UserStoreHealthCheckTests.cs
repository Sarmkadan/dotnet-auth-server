#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Diagnostics;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserStoreHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_StoreAnswersProbe_ReturnsHealthy()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);
        var check = new UserStoreHealthCheck(CreateStore(repository.Object));

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_StoreThrows_ReturnsUnhealthyWithoutExposingMessageInDescription()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Server=db.internal;Password=hunter2"));
        var check = new UserStoreHealthCheck(CreateStore(repository.Object));

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.DoesNotContain("hunter2", result.Description);
        Assert.Equal("User store probe failed.", result.Description);
    }

    [Fact]
    public async Task CheckHealthAsync_StoreHangs_ReturnsUnhealthyAfterProbeTimeout()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, CancellationToken token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return null;
            });
        var check = new UserStoreHealthCheck(CreateStore(repository.Object));

        // Act
        var result = await check.CheckHealthAsync(new HealthCheckContext());

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("did not respond", result.Description);
    }

    private static UserStore CreateStore(IUserRepository repository) =>
        new(repository, new UserStoreOptions(), NullLogger<UserStore>.Instance);
}
