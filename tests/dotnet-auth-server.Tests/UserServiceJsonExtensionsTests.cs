using System;
using System.Text.Json;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public class UserServiceJsonExtensionsTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly AuthServerOptions _options;
    private readonly UserService _userService;

    public UserServiceJsonExtensionsTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _loggerMock = new Mock<ILogger<UserService>>();
        _options = new AuthServerOptions();
        
        _userService = new UserService(
            _userRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _options,
            _loggerMock.Object);
    }

    [Fact]
    public void ToJson_WithValidUserService_ReturnsEmptyJsonObject()
    {
        // Act
        var result = _userService.ToJson();

        // Assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsEmptyJsonObject()
    {
        // Act
        var result = _userService.ToJson(indented: true);

        // Assert
        Assert.Equal("{}", result);
    }

    [Fact]
    public void ToJson_NullUserService_ThrowsArgumentNullException()
    {
        // Arrange
        UserService? nullUserService = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullUserService!.ToJson());
    }

    [Fact]
    public void FromJson_WhenCalled_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        // This is expected because UserService has no parameterless constructor.
        Assert.Throws<InvalidOperationException>(() => UserServiceJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_WhenCalled_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => UserServiceJsonExtensions.TryFromJson(json, out var value));
    }

    [Fact]
    public void TryFromJson_WithEmptyJson_ReturnsFalse()
    {
        // Act
        var success = UserServiceJsonExtensions.TryFromJson(string.Empty, out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }
}
