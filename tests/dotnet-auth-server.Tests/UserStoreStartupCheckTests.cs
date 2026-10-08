using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserStoreStartupCheckTests
{
    [Fact]
    public async Task StartAsync_StorageResponds_Completes()
    {
        // Arrange
        var store = new UserStore(new UserRepository(), new UserStoreOptions(), NullLogger<UserStore>.Instance);
        var check = new UserStoreStartupCheck(store, NullLogger<UserStoreStartupCheck>.Instance);

        // Act
        var exception = await Record.ExceptionAsync(() => check.StartAsync(CancellationToken.None));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public async Task StartAsync_RepositoryThrows_ThrowsInvalidOperationNamingStorageKeys()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("connection refused"));
        var store = new UserStore(repository.Object, new UserStoreOptions(), NullLogger<UserStore>.Instance);
        var check = new UserStoreStartupCheck(store, NullLogger<UserStoreStartupCheck>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("DotnetAuthServer:AuthServer:UseInMemoryDatabase", exception.Message);
        Assert.Contains("DotnetAuthServer:AuthServer:DatabaseConnectionString", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public async Task StartAsync_RepositoryHangs_TimesOutWithStorageKeyMessage()
    {
        // Arrange
        var repository = new Mock<IUserRepository>();
        repository
            .Setup(r => r.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns<string, CancellationToken>(async (_, token) =>
            {
                await Task.Delay(Timeout.Infinite, token);
                return (User?)null;
            });
        var store = new UserStore(repository.Object, new UserStoreOptions(), NullLogger<UserStore>.Instance);
        var check = new UserStoreStartupCheck(store, NullLogger<UserStoreStartupCheck>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => check.StartAsync(CancellationToken.None));

        // Assert
        Assert.Contains("did not respond", exception.Message);
        Assert.Contains("DotnetAuthServer:AuthServer:UseInMemoryDatabase", exception.Message);
    }
}
