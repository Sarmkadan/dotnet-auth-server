using System;
using System.Linq;
using System.Threading.Tasks;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data;
using DotnetAuthServer.Data.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserStoreTests
{
    [Fact]
    public void Constructor_NullRepository_ThrowsArgumentNullException()
    {
        // Arrange
        IUserRepository repository = null!;

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UserStore(repository, new UserStoreOptions(), NullLogger<UserStore>.Instance));

        // Assert
        Assert.Equal("userRepository", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new UserRepository();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UserStore(repository, null!, NullLogger<UserStore>.Instance));

        // Assert
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        // Arrange
        var repository = new UserRepository();

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new UserStore(repository, new UserStoreOptions(), null!));

        // Assert
        Assert.Equal("logger", exception.ParamName);
    }

    [Fact]
    public void Constructor_NullPasswordHashing_ThrowsOptionsValidationExceptionNotNullReference()
    {
        // Arrange
        var repository = new UserRepository();
        var options = new UserStoreOptions { PasswordHashing = null! };

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            new UserStore(repository, options, NullLogger<UserStore>.Instance));

        // Assert
        Assert.Contains(exception.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing"));
    }

    [Fact]
    public void Constructor_IterationsBelowMinimum_ThrowsOptionsValidationExceptionNamingKey()
    {
        // Arrange
        var repository = new UserRepository();
        var options = new UserStoreOptions
        {
            PasswordHashing = new PasswordHashingOptions { Iterations = 1000 }
        };

        // Act
        var exception = Assert.Throws<OptionsValidationException>(() =>
            new UserStore(repository, options, NullLogger<UserStore>.Instance));

        // Assert
        Assert.Contains(exception.Failures, f => f.Contains("DotnetAuthServer:UserStore:PasswordHashing:Iterations"));
    }

    [Fact]
    public async Task CreateAsync_ThenVerifyPassword_MatchesOnlyTheOriginalPassword()
    {
        // Arrange
        var store = new UserStore(new UserRepository(), new UserStoreOptions(), NullLogger<UserStore>.Instance);

        // Act
        var user = await store.CreateAsync("alice", "alice@example.com", "correct-horse");

        // Assert
        Assert.True(store.VerifyPassword(user, "correct-horse"));
        Assert.False(store.VerifyPassword(user, "wrong-password"));
    }

    [Fact]
    public async Task CreateAsync_UsesConfiguredIterations_ProducesDifferentHashThanDefault()
    {
        // Arrange
        var defaultStore = new UserStore(new UserRepository(), new UserStoreOptions(), NullLogger<UserStore>.Instance);
        var customOptions = new UserStoreOptions
        {
            PasswordHashing = new PasswordHashingOptions { Iterations = UserStoreOptionsValidator.MinPbkdf2Iterations + 1 }
        };
        var customStore = new UserStore(new UserRepository(), customOptions, NullLogger<UserStore>.Instance);

        // Act
        var defaultUser = await defaultStore.CreateAsync("bob", "bob@example.com", "password");
        var customUser = await customStore.CreateAsync("bob", "bob@example.com", "password");

        // Assert
        Assert.NotEqual(defaultUser.PasswordHash, customUser.PasswordHash);
    }

    [Fact]
    public void VerifyPassword_NullUser_ThrowsArgumentNullException()
    {
        // Arrange
        var store = new UserStore(new UserRepository(), new UserStoreOptions(), NullLogger<UserStore>.Instance);

        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => store.VerifyPassword(null!, "password"));

        // Assert
        Assert.Equal("user", exception.ParamName);
    }
}
