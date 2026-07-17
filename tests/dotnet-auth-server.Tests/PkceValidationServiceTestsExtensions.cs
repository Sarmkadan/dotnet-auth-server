#nullable enable

namespace DotnetAuthServer.Tests;

using System.Globalization;
using DotnetAuthServer.Services;
using FluentAssertions;

/// <summary>
/// Extension methods for <see cref="PkceValidationServiceTests"/> that provide additional utility and verification capabilities.
/// </summary>
/// <remarks>
/// This static class contains extension methods for test scenarios involving PKCE (Proof Key for Code Exchange) validation.
/// All methods include proper argument validation and follow .NET design guidelines.
/// </remarks>
public static class PkceValidationServiceTestsExtensions
{
    /// <summary>
    /// Creates a new instance of <see cref="PkceValidationServiceTests"/> with custom PKCE requirement configuration.
    /// </summary>
    /// <param name="tests">The test instance to extend.</param>
    /// <param name="pkceRequirement">Whether PKCE should be required for all clients.</param>
    /// <returns>A new <see cref="PkceValidationServiceTests"/> instance with updated service configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <see langword="null"/>.</exception>
    public static PkceValidationServiceTests WithPkceRequirement(this PkceValidationServiceTests tests, bool pkceRequirement)
    {
        ArgumentNullException.ThrowIfNull(tests);

        var loggerMock = new global::Moq.Mock<global::Microsoft.Extensions.Logging.ILogger<PkceValidationService>>();
        var options = new global::DotnetAuthServer.Configuration.AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "test_signing_key_must_be_at_least_32_chars",
            DatabaseConnectionString = "Data Source=:memory:",
            RequirePkceForAllClients = pkceRequirement
        };

        var serviceField = typeof(PkceValidationServiceTests).GetField(
            "_service",
            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
        serviceField?.SetValue(tests, new PkceValidationService(options, loggerMock.Object));

        return tests;
    }

    /// <summary>
    /// Creates a new instance of <see cref="PkceValidationServiceTests"/> with custom issuer URL.
    /// </summary>
    /// <param name="tests">The test instance to extend.</param>
    /// <param name="issuerUrl">The issuer URL to use for testing.</param>
    /// <returns>A new <see cref="PkceValidationServiceTests"/> instance with updated service configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="issuerUrl"/> is null or empty.</exception>
    public static PkceValidationServiceTests WithIssuerUrl(this PkceValidationServiceTests tests, string issuerUrl)
    {
        ArgumentNullException.ThrowIfNull(tests);
        ArgumentException.ThrowIfNullOrEmpty(issuerUrl);

        var loggerMock = new global::Moq.Mock<global::Microsoft.Extensions.Logging.ILogger<PkceValidationService>>();
        var options = new global::DotnetAuthServer.Configuration.AuthServerOptions
        {
            IssuerUrl = issuerUrl,
            JwtSigningKey = "test_signing_key_must_be_at_least_32_chars",
            DatabaseConnectionString = "Data Source=:memory:",
            RequirePkceForAllClients = true
        };

        var serviceField = typeof(PkceValidationServiceTests).GetField(
            "_service",
            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);
        serviceField?.SetValue(tests, new PkceValidationService(options, loggerMock.Object));

        return tests;
    }

    /// <summary>
    /// Verifies that a code verifier has the correct length according to RFC 7636.
    /// </summary>
    /// <param name="verifier">The code verifier to validate.</param>
    /// <param name="minLength">The minimum allowed length (default: 43).</param>
    /// <param name="maxLength">The maximum allowed length (default: 128).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="verifier"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when verifier length is outside bounds.</exception>
    public static void ShouldHaveValidLength(this string verifier, int minLength = 43, int maxLength = 128)
    {
        ArgumentNullException.ThrowIfNull(verifier);

        verifier.Length.Should().BeInRange(minLength, maxLength,
            $"Code verifier length must be between {minLength} and {maxLength} characters per RFC 7636");
    }

    /// <summary>
    /// Verifies that a code verifier contains only URL-safe characters as defined in RFC 7636.
    /// </summary>
    /// <param name="verifier">The code verifier to validate.</param>
    /// <param name="allowUnreserved">Whether to allow unreserved characters [A-Z][a-z][0-9]-._~.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="verifier"/> is null.</exception>
    public static void ShouldContainOnlyUrlSafeCharacters(this string verifier, bool allowUnreserved = true)
    {
        ArgumentNullException.ThrowIfNull(verifier);

        if (allowUnreserved)
        {
            verifier.Should().MatchRegex(
                @"^[A-Za-z0-9\-._~]+$",
                "Code verifier must contain only unreserved URL-safe characters per RFC 7636");
        }
        else
        {
            verifier.Should().MatchRegex(
                @"^[A-Za-z0-9\-._~]*$",
                "Code verifier must contain only URL-safe characters");
        }
    }

    /// <summary>
    /// Generates multiple code verifiers and verifies they are all unique.
    /// </summary>
    /// <param name="serviceTests">The test instance.</param>
    /// <param name="count">Number of verifiers to generate (default: 10).</param>
    /// <returns>An enumerable of generated code verifiers.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceTests"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is less than 1.</exception>
    public static global::System.Collections.Generic.IEnumerable<string> GenerateCodeVerifiers(
        this PkceValidationServiceTests serviceTests,
        int count = 10)
    {
        ArgumentNullException.ThrowIfNull(serviceTests);
        if (count < 1)
        {
            throw new global::System.ArgumentOutOfRangeException(nameof(count), "Count must be at least 1");
        }

        var service = serviceTests.GetService();
        var verifiers = new global::System.Collections.Generic.List<string>(count);

        for (var i = 0; i < count; i++)
        {
            var verifier = service.GenerateCodeVerifier();
            verifier.ShouldHaveValidLength();
            verifier.ShouldContainOnlyUrlSafeCharacters();
            verifiers.Add(verifier);
        }

        return verifiers.AsReadOnly();
    }

    /// <summary>
    /// Gets the underlying <see cref="PkceValidationService"/> instance from the test.
    /// </summary>
    /// <param name="serviceTests">The test instance.</param>
    /// <returns>The <see cref="PkceValidationService"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceTests"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the service field cannot be accessed.</exception>
    public static PkceValidationService GetService(this PkceValidationServiceTests serviceTests)
    {
        ArgumentNullException.ThrowIfNull(serviceTests);

        var serviceField = typeof(PkceValidationServiceTests).GetField(
            "_service",
            global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance);

        return serviceField?.GetValue(serviceTests) as PkceValidationService
        ?? throw new global::System.InvalidOperationException("Could not access _service field");
    }

    /// <summary>
    /// Generates a code challenge using the S256 method and verifies it's deterministic.
    /// </summary>
    /// <param name="serviceTests">The test instance.</param>
    /// <param name="verifier">The code verifier to use.</param>
    /// <returns>A tuple containing (challenge, isDeterministic).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serviceTests"/> or <paramref name="verifier"/> is null.</exception>
    public static (string Challenge, bool IsDeterministic) GenerateDeterministicS256Challenge(
        this PkceValidationServiceTests serviceTests,
        string verifier)
    {
        ArgumentNullException.ThrowIfNull(serviceTests);
        ArgumentNullException.ThrowIfNull(verifier);

        var service = serviceTests.GetService();
        var first = service.GenerateCodeChallenge(verifier, "S256");
        var second = service.GenerateCodeChallenge(verifier, "S256");

        return (first, first == second);
    }

    /// <summary>
    /// Verifies that all generated code verifiers are unique within a collection.
    /// </summary>
    /// <param name="verifiers">The collection of code verifiers to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="verifiers"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when duplicates are found or collection is empty.</exception>
    public static void ShouldAllBeUnique(this global::System.Collections.Generic.IEnumerable<string> verifiers)
    {
        ArgumentNullException.ThrowIfNull(verifiers);

        var list = verifiers.ToList();
        if (list.Count == 0)
        {
            throw new global::System.ArgumentException("Collection cannot be empty", nameof(verifiers));
        }

        var distinctCount = list.Distinct().Count();
        distinctCount.Should().Be(list.Count,
            "All generated code verifiers should be unique to prevent collisions");
    }
}