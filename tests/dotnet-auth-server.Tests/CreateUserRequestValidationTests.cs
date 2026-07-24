#nullable enable
using System;
using System.Collections.Generic;
using Xunit;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Tests;

public class CreateUserRequestValidationTests
{
    private static CreateUserRequest ValidRequest => new CreateUserRequest
    {
        Username = "validUser",
        Email = "user@example.com",
        Password = "StrongP4ssword",
        Roles = new[] { "admin", "user" }
    };

    [Fact]
    public void Validate_ReturnsEmpty_WhenRequestIsValid()
    {
        var errors = ValidRequest.Validate();

        Assert.Empty(errors);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenRequestIsValid()
    {
        var result = ValidRequest.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void EnsureValid_DoesNotThrow_WhenRequestIsValid()
    {
        var ex = Record.Exception(() => ValidRequest.EnsureValid());

        Assert.Null(ex);
    }

    [Fact]
    public void Validate_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        CreateUserRequest? request = null;

        var ex = Assert.Throws<ArgumentNullException>(() => request!.Validate());

        Assert.Equal("value", ex.ParamName);
    }

    [Fact]
    public void Validate_ReturnsErrors_ForVariousInvalidFields()
    {
        var request = new CreateUserRequest
        {
            Username = "ab",               // too short
            Email = "invalid-email",       // malformed
            Password = "short",            // too short & missing requirements
            Roles = new[] { "admin", "" }  // contains empty role
        };

        var errors = request.Validate();

        var expectedMessages = new[]
        {
            "Username must be between 3 and 50 characters long.",
            "Email must be a valid email address.",
            "Password must be at least 8 characters long.",
            "Password must contain at least one uppercase letter, one lowercase letter, and one digit.",
            "Roles collection contains empty or null entries."
        };

        foreach (var expected in expectedMessages)
        {
            Assert.Contains(expected, errors);
        }

        Assert.Equal(expectedMessages.Length, errors.Count);
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WithAllErrors_WhenRequestIsInvalid()
    {
        var request = new CreateUserRequest
        {
            Username = "",                 // missing
            Email = "",                    // missing
            Password = "",                 // missing
            Roles = null
        };

        var ex = Assert.Throws<ArgumentException>(() => request.EnsureValid());

        // The message should contain all validation errors
        Assert.Contains("Username is required.", ex.Message);
        Assert.Contains("Email is required.", ex.Message);
        Assert.Contains("Password is required.", ex.Message);
        // No roles error expected because Roles is null (allowed)
    }
}
