#nullable enable
using System;
using System.Collections.Generic;
using Xunit;
using DotnetAuthServer.Domain.Entities;

namespace DotnetAuthServer.Tests;

public class UserExtensionsTests
{
    [Fact]
    public void HasRole_ReturnsTrueWhenUserHasRole()
    {
        // Arrange
        var user = new User
        {
            Roles = new List<string> { "Admin", "User" }
        };

        // Act
        var result = user.HasRole("admin");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAnyRole_ReturnsTrueWhenUserHasAnySpecifiedRole()
    {
        // Arrange
        var user = new User
        {
            Roles = new List<string> { "Editor" }
        };
        var rolesToCheck = new[] { "Admin", "Editor", "Viewer" };

        // Act
        var result = user.HasAnyRole(rolesToCheck);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetAttribute_ReturnsValueOrDefault()
    {
        // Arrange
        var user = new User
        {
            Attributes = new Dictionary<string, object>
            {
                { "age", 30 }
            }
        };

        // Act & Assert
        int age = user.GetAttribute<int>("age");
        Assert.Equal(30, age);

        string missing = user.GetAttribute<string>("nonexistent", "default");
        Assert.Equal("default", missing);
    }

    [Fact]
    public void SetAttribute_UpdatesValueAndUpdatedAt()
    {
        // Arrange
        var user = new User
        {
            Attributes = new Dictionary<string, object>()
        };
        var before = DateTime.UtcNow;

        // Act
        user.SetAttribute("key", "value");

        // Assert
        Assert.True(user.Attributes.ContainsKey("key"));
        Assert.Equal("value", user.Attributes["key"]);
        Assert.True(user.UpdatedAt >= before);
    }

    [Fact]
    public void IsAdmin_ReturnsTrueWhenUserHasAdminRole()
    {
        // Arrange
        var user = new User
        {
            Roles = new List<string> { "Administrator" }
        };

        // Act
        var result = user.IsAdmin();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetDisplayName_ReturnsFullNameOrUsername()
    {
        // FullName present
        var userWithFullName = new User
        {
            FullName = "John Doe",
            Username = "jdoe"
        };
        Assert.Equal("John Doe", userWithFullName.GetDisplayName());

        // FullName null
        var userWithoutFullName = new User
        {
            FullName = null,
            Username = "jdoe"
        };
        Assert.Equal("jdoe", userWithoutFullName.GetDisplayName());
    }

    [Fact]
    public void CanAuthenticate_ReturnsTrueWhenAllConditionsMet_AndFalseWhenNot()
    {
        // Arrange - all good
        var user = new User
        {
            EmailVerified = true,
            IsActive = true
            // Assume IsLocked() returns false by default
        };

        // Act & Assert - happy path
        Assert.True(user.CanAuthenticate());

        // Arrange - email not verified
        user.EmailVerified = false;

        // Act & Assert - failure path
        Assert.False(user.CanAuthenticate());
    }

    [Fact]
    public void SecondsSinceLastLogin_ReturnsCorrectValueOrNull()
    {
        // Arrange - user with a last login timestamp
        var now = DateTime.UtcNow;
        var userWithLogin = new User
        {
            LastLoginAt = now.AddMinutes(-10)
        };

        // Act
        var seconds = userWithLogin.SecondsSinceLastLogin();

        // Assert - should be roughly 600 seconds
        Assert.NotNull(seconds);
        Assert.InRange(seconds!.Value, 590, 610);

        // Arrange - user never logged in
        var userNeverLoggedIn = new User
        {
            LastLoginAt = null
        };

        // Act & Assert
        Assert.Null(userNeverLoggedIn.SecondsSinceLastLogin());
    }
}
