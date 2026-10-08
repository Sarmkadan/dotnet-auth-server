#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

/// <summary>
/// Tests that <see cref="MfaOptions"/> are read from <c>appsettings.json</c> and validated when the host starts,
/// using the same <see cref="MfaOptionsServiceCollectionExtensions.AddMfaOptions"/> registration as Program.cs.
/// </summary>
public sealed class MfaOptionsStartupTests
{
    [Fact]
    public async Task StartAsync_ValidMfaSection_StartsAndBindsOptions()
    {
        // Arrange
        const string json = """
            {
              "DotnetAuthServer": {
                "Mfa": { "Issuer": "Contoso", "Digits": 8, "TimeStepSeconds": 60, "AllowedDriftSteps": 2, "RecoveryCodeCount": 10, "RecoveryCodeLength": 12 }
              }
            }
            """;
        var contentRoot = CreateContentRoot(json);

        try
        {
            // Act
            using var host = BuildHost(contentRoot);
            await host.StartAsync();

            // Assert
            var options = host.Services.GetRequiredService<MfaOptions>();
            Assert.Equal("Contoso", options.Issuer);
            Assert.Equal(8, options.Digits);
            Assert.Equal(60, options.TimeStepSeconds);
            Assert.Equal(2, options.AllowedDriftSteps);
            Assert.Equal(10, options.RecoveryCodeCount);
            Assert.Equal(12, options.RecoveryCodeLength);

            await host.StopAsync();
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task StartAsync_NoMfaSection_StartsWithDefaults()
    {
        // Arrange
        var contentRoot = CreateContentRoot("{}");

        try
        {
            // Act
            using var host = BuildHost(contentRoot);
            await host.StartAsync();

            // Assert
            var options = host.Services.GetRequiredService<MfaOptions>();
            Assert.Equal(6, options.Digits);
            Assert.Equal(30, options.TimeStepSeconds);

            await host.StopAsync();
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    [Fact]
    public async Task StartAsync_InvalidMfaSection_FailsStartupNamingTheKey()
    {
        // Arrange
        const string json = """
            {
              "DotnetAuthServer": {
                "Mfa": { "Digits": 7, "TimeStepSeconds": 300 }
              }
            }
            """;
        var contentRoot = CreateContentRoot(json);

        try
        {
            // Act
            using var host = BuildHost(contentRoot);
            var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

            // Assert
            Assert.Contains("DotnetAuthServer:Mfa:Digits", exception.Message);
            Assert.Contains("DotnetAuthServer:Mfa:TimeStepSeconds", exception.Message);
        }
        finally
        {
            Directory.Delete(contentRoot, recursive: true);
        }
    }

    private static IHost BuildHost(string contentRoot) =>
        Host.CreateDefaultBuilder()
            .UseContentRoot(contentRoot)
            .UseEnvironment(Environments.Production)
            .ConfigureLogging(logging => logging.ClearProviders())
            .ConfigureServices((context, services) => services.AddMfaOptions(context.Configuration))
            .Build();

    private static string CreateContentRoot(string appSettingsJson)
    {
        var contentRoot = Path.Combine(Path.GetTempPath(), "mfa-options-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRoot);
        File.WriteAllText(Path.Combine(contentRoot, "appsettings.json"), appSettingsJson);
        return contentRoot;
    }
}
