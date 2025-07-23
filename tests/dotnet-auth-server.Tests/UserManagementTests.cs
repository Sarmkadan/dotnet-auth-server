#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Tests for the UserManagementService.
/// </summary>
public sealed class UserManagementTests
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly UserManagementService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserManagementTests"/> class.
    /// </summary>
    public UserManagementTests()
    {
        _userRepository = new UserRepository();
        _refreshTokenRepository = new RefreshTokenRepository();
        _sessionRepository = new UserSessionRepository();

        var logger = new Mock<ILogger<UserManagementService>>().Object;
        var loggerFactory = new Mock<ILoggerFactory>();
        loggerFactory
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = new string('x', 32),
            DatabaseConnectionString = ""
        };

        _service = new UserManagementService(
            _userRepository,
            _refreshTokenRepository,
            _sessionRepository,
            logger,
            loggerFactory.Object,
            options);
    }

    /// <summary>
    /// Tests that creating a user with a valid request returns a user with the correct fields.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithValidRequest_ReturnsUserWithCorrectFields()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "testuser",
            Email = "testuser@example.com",
            Password = "SecurePass123!",
            FullName = "Test User",
            Roles = new[] { "viewer" }
        };

        // Act
        var result = await _service.CreateUserAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Username.Should().Be("testuser");
        result.Email.Should().Be("testuser@example.com");
        result.FullName.Should().Be("Test User");
        result.IsActive.Should().BeTrue();
        result.Roles.Should().Contain("viewer");
        result.UserId.Should().NotBeNullOrWhiteSpace();
    }

    /// <summary>
    /// Tests that creating a user with a duplicate username throws an AuthServerException.
    /// </summary>
    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ThrowsAuthServerException()
    {
        // Arrange
        var request = new CreateUserRequest
        {
            Username = "duplicate",
            Email = "dup1@example.com",
            Password = "SecurePass123!"
        };

        await _service.CreateUserAsync(request);

        var duplicateRequest = new CreateUserRequest
        {
            Username = "duplicate",
            Email = "dup2@example.com",
            Password = "SecurePass123!"
        };

        // Act
        Func<Task> act = async () => await _service.CreateUserAsync(duplicateRequest);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == Constants.ErrorCodes.InvalidRequest,
                "creating a user with an existing username must be rejected");
    }

    /// <summary>
    /// Tests that updating a user's full name is persisted.
    /// </summary>
    [Fact]
    public async Task UpdateUser_FullNameChange_IsPersisted()
    {
        // Arrange
        var created = await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "updateme",
            Email = "updateme@example.com",
            Password = "SecurePass123!"
        });

        // Act
        var updated = await _service.UpdateUserAsync(created.UserId, new UpdateUserRequest
        {
            FullName = "Updated Name"
        });

        // Assert
        updated.FullName.Should().Be("Updated Name");
    }

    /// <summary>
    /// Tests that deleting an existing user removes it from the repository.
    /// </summary>
    [Fact]
    public async Task DeleteUser_ExistingUser_RemovesFromRepository()
    {
        // Arrange
        var created = await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "todelete",
            Email = "todelete@example.com",
            Password = "SecurePass123!"
        });

        // Act
        await _service.DeleteUserAsync(created.UserId);

        // Assert
        Func<Task> act = async () => await _service.GetUserByIdAsync(created.UserId);
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.StatusCode == 404, "deleted user should no longer be found");
    }

    /// <summary>
    /// Tests that assigning a new role to a user appears in the user's roles.
    /// </summary>
    [Fact]
    public async Task AssignRole_NewRole_AppearsInUserRoles()
    {
        // Arrange
        var created = await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "roleuser",
            Email = "roleuser@example.com",
            Password = "SecurePass123!"
        });

        // Act
        await _service.AssignRoleAsync(created.UserId, "admin");
        var user = await _service.GetUserByIdAsync(created.UserId);

        // Assert
        user.Roles.Should().Contain("admin");
    }

    /// <summary>
    /// Tests that locking a user and then unlocking it clears the lockout.
    /// </summary>
    [Fact]
    public async Task LockUser_ThenUnlock_ClearsLockout()
    {
        // Arrange
        var created = await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "locktest",
            Email = "locktest@example.com",
            Password = "SecurePass123!"
        });

        // Act
        await _service.LockUserAsync(created.UserId, TimeSpan.FromMinutes(30));
        var locked = await _service.GetUserByIdAsync(created.UserId);

        await _service.UnlockUserAsync(created.UserId);
        var unlocked = await _service.GetUserByIdAsync(created.UserId);

        // Assert
        locked.IsLocked.Should().BeTrue("the account should be locked after LockUserAsync");
        unlocked.IsLocked.Should().BeFalse("the account should be unlocked after UnlockUserAsync");
        unlocked.LockedUntil.Should().BeNull("lockout expiry must be cleared on unlock");
    }

    /// <summary>
    /// Tests that getting a user by ID with a non-existent user throws a NotFound exception.
    /// </summary>
    [Fact]
    public async Task GetUserById_NonExistentUser_ThrowsNotFound()
    {
        // Arrange & Act
        Func<Task> act = async () => await _service.GetUserByIdAsync("nonexistent-id");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.StatusCode == 404);
    }

    /// <summary>
    /// Tests that searching for users by username returns matching users.
    /// </summary>
    [Fact]
    public async Task SearchUsers_ByUsername_ReturnsMatchingUsers()
    {
        // Arrange
        await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "alice_search",
            Email = "alice@example.com",
            Password = "SecurePass123!"
        });
        await _service.CreateUserAsync(new CreateUserRequest
        {
            Username = "bob_other",
            Email = "bob@example.com",
            Password = "SecurePass123!"
        });

        // Act
        var results = (await _service.SearchUsersAsync("alice")).ToList();

        // Assert
        results.Should().HaveCount(1);
        results[0].Username.Should().Be("alice_search");
    }
}
