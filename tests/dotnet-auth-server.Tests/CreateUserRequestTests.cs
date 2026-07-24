namespace DotnetAuthServer.Tests;

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using DotnetAuthServer.Domain.Models;
using Xunit;

public class CreateUserRequestTests
{
    private static List<ValidationResult> ValidateModel(CreateUserRequest model)
    {
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Constructor_InitializesWithDefaultValues()
    {
        var request = new CreateUserRequest();

        Assert.NotNull(request.Roles);
        Assert.Empty(request.Roles);
        Assert.Null(request.FullName);
    }

    [Fact]
    public void ValidRequest_PassesValidation()
    {
        var request = new CreateUserRequest
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = "password123",
            FullName = "Test User",
            Roles = new List<string> { "user" }
        };

        var results = ValidateModel(request);
        Assert.Empty(results);
    }

    [Fact]
    public void Username_ShorterThanMinimumLength_FailsValidation()
    {
        var request = new CreateUserRequest
        {
            Username = "ab", // Minimum length is 3
            Email = "test@example.com",
            Password = "password123"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateUserRequest.Username)));
    }

    [Fact]
    public void Email_InvalidFormat_FailsValidation()
    {
        var request = new CreateUserRequest
        {
            Username = "validuser",
            Email = "not-an-email",
            Password = "password123"
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateUserRequest.Email)));
    }

    [Fact]
    public void Password_ShorterThanMinimumLength_FailsValidation()
    {
        var request = new CreateUserRequest
        {
            Username = "validuser",
            Email = "test@example.com",
            Password = "short" // Minimum length is 8
        };

        var results = ValidateModel(request);
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateUserRequest.Password)));
    }

    [Fact]
    public void RequiredFields_Missing_FailsValidation()
    {
        var request = new CreateUserRequest();

        var results = ValidateModel(request);
        Assert.NotEmpty(results);

        var memberNames = results.SelectMany(r => r.MemberNames).ToList();
        Assert.Contains(nameof(CreateUserRequest.Username), memberNames);
        Assert.Contains(nameof(CreateUserRequest.Email), memberNames);
        Assert.Contains(nameof(CreateUserRequest.Password), memberNames);
    }
}
