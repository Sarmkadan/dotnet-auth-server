using System.ComponentModel.DataAnnotations;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public class DotnetAuthServerOptionsTests
{
    [Fact]
    public void Constructor_InitializesProperties()
    {
        var options = new DotnetAuthServerOptions();

        Assert.NotNull(options.AuthServer);
        Assert.NotNull(options.Cache);
        Assert.NotNull(options.Logging);
        Assert.NotNull(options.Opa);
    }

    [Fact]
    public void SectionName_IsCorrect()
    {
        Assert.Equal("DotnetAuthServer", DotnetAuthServerOptions.SectionName);
    }

    [Fact]
    public void Validation_WithValidInstance_Passes()
    {
        var options = new DotnetAuthServerOptions();
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        var isValid = Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.True(isValid);
        Assert.Empty(validationResults);
    }

    [Fact]
    public void Validation_WithNullAuthServer_Fails()
    {
        var options = new DotnetAuthServerOptions { AuthServer = null! };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.Single(validationResults);
        Assert.Contains("AuthServer", validationResults[0].MemberNames);
    }

    [Fact]
    public void Validation_WithNullCache_Fails()
    {
        var options = new DotnetAuthServerOptions { Cache = null! };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.Single(validationResults);
        Assert.Contains("Cache", validationResults[0].MemberNames);
    }

    [Fact]
    public void Validation_WithNullLogging_Fails()
    {
        var options = new DotnetAuthServerOptions { Logging = null! };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.Single(validationResults);
        Assert.Contains("Logging", validationResults[0].MemberNames);
    }

    [Fact]
    public void Validation_WithNullOpa_Fails()
    {
        var options = new DotnetAuthServerOptions { Opa = null! };
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(options);

        Validator.TryValidateObject(options, validationContext, validationResults, true);

        Assert.Single(validationResults);
        Assert.Contains("Opa", validationResults[0].MemberNames);
    }
}
