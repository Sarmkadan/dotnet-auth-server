#nullable enable
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class OpaOptionsTests
{
    private static IList<ValidationResult> Validate(object instance)
    {
        var context = new ValidationContext(instance, serviceProvider: null, items: null);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void DefaultValues_ShouldMatchExpected()
    {
        // Arrange
        var options = new OpaOptions();

        // Assert
        Assert.False(options.Enabled);
        Assert.Equal("http://localhost:8181", options.BaseUrl);
        Assert.Equal("authz", options.PolicyPath);
        Assert.Equal(5, options.TimeoutSeconds);
        Assert.False(options.FailClosedOnError);
    }

    [Fact]
    public void CanAssignCustomValues()
    {
        // Arrange
        var options = new OpaOptions
        {
            Enabled = true,
            BaseUrl = "https://opa.example.com",
            PolicyPath = "custom/policy",
            TimeoutSeconds = 30,
            FailClosedOnError = true
        };

        // Assert
        Assert.True(options.Enabled);
        Assert.Equal("https://opa.example.com", options.BaseUrl);
        Assert.Equal("custom/policy", options.PolicyPath);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.True(options.FailClosedOnError);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void TimeoutSeconds_InvalidValues_ShouldFailValidation(int invalidValue)
    {
        // Arrange
        var options = new OpaOptions { TimeoutSeconds = invalidValue };

        // Act
        var results = Validate(options);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OpaOptions.TimeoutSeconds)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void BaseUrl_NullOrEmpty_ShouldFailValidation(string? invalidValue)
    {
        // Arrange
        var options = new OpaOptions { BaseUrl = invalidValue! };

        // Act
        var results = Validate(options);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OpaOptions.BaseUrl)));
    }

    [Fact]
    public void BaseUrl_InvalidUrlFormat_ShouldFailValidation()
    {
        // Arrange
        var options = new OpaOptions { BaseUrl = "not-a-valid-url" };

        // Act
        var results = Validate(options);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OpaOptions.BaseUrl)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void PolicyPath_NullOrEmpty_ShouldFailValidation(string? invalidValue)
    {
        // Arrange
        var options = new OpaOptions { PolicyPath = invalidValue! };

        // Act
        var results = Validate(options);

        // Assert
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(OpaOptions.PolicyPath)));
    }
}
