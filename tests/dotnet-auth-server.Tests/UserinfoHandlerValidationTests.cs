#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using DotnetAuthServer.Handlers;
using FluentAssertions;
using Xunit;

public sealed class UserinfoHandlerValidationTests
{
    [Fact]
    public void Validate_ValidUserinfo_ShouldReturnNoErrors()
    {
        var response = new UserinfoResponse
        {
            Sub = "user123",
            Email = "test@example.com",
            EmailVerified = true,
            PhoneNumber = "1234567890",
            PhoneNumberVerified = true,
            Address = new AddressInfo { StreetAddress = "123 Main St", Locality = "City", Region = "State", PostalCode = "12345", Country = "Country" },
            UpdatedAt = 1600000000
        };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingSub_ShouldReturnError(string? sub)
    {
        var response = new UserinfoResponse { Sub = sub! };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().ContainSingle().Which.Should().Contain("Sub claim is required");
    }

    [Fact]
    public void Validate_InvalidEmailFormat_ShouldReturnError()
    {
        var response = new UserinfoResponse { Sub = "user1", Email = "invalid-email" };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().Contain("Email format is invalid.");
    }

    [Fact]
    public void Validate_EmailVerifiedWithoutEmail_ShouldReturnError()
    {
        var response = new UserinfoResponse { Sub = "user1", EmailVerified = true };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().Contain("EmailVerified cannot be true when Email is not provided.");
    }

    [Fact]
    public void Validate_InvalidPhoneNumber_ShouldReturnError()
    {
        var response = new UserinfoResponse { Sub = "user1", PhoneNumber = "123" };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().Contain("PhoneNumber format is invalid.");
    }

    [Fact]
    public void Validate_AddressTooLong_ShouldReturnError()
    {
        var response = new UserinfoResponse
        {
            Sub = "user1",
            Address = new AddressInfo { StreetAddress = new string('a', 201) }
        };

        var errors = UserinfoHandlerValidation.Validate(response);

        errors.Should().Contain(e => e.Contains("StreetAddress exceeds maximum length"));
    }

    [Fact]
    public void EnsureValid_InvalidResponse_ShouldThrowArgumentException()
    {
        var response = new UserinfoResponse { Sub = "" };

        Action act = () => UserinfoHandlerValidation.EnsureValid(response);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IsValid_ValidResponse_ShouldReturnTrue()
    {
        var response = new UserinfoResponse { Sub = "user1" };

        UserinfoHandlerValidation.IsValid(response).Should().BeTrue();
    }
}
