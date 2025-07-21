#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Tests for the PkceValidationService class.
/// </summary>
public sealed class PkceValidationServiceTests
{
    private readonly Mock<ILogger<PkceValidationService>> _loggerMock;
    private readonly AuthServerOptions _options;
    private readonly PkceValidationService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="PkceValidationServiceTests"/> class.
    /// </summary>
    public PkceValidationServiceTests()
    {
        _loggerMock = new Mock<ILogger<PkceValidationService>>();
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "test_signing_key_must_be_at_least_32_chars",
            DatabaseConnectionString = "Data Source=:memory:",
            RequirePkceForAllClients = true
        };
        _service = new PkceValidationService(_options, _loggerMock.Object);
    }

    /// <summary>
    /// Verifies that the GenerateCodeVerifier method returns a string within the RFC 7636 length bounds.
    /// </summary>
    [Fact]
    public void GenerateCodeVerifier_WhenCalled_ReturnsStringWithinRfc7636LengthBounds()
    {
        // Arrange & Act
        var verifier = _service.GenerateCodeVerifier();

        // Assert
        verifier.Should().NotBeNullOrWhiteSpace();
        verifier.Length.Should().BeInRange(43, 128,
            "RFC 7636 mandates code verifiers between 43 and 128 characters");
    }

    /// <summary>
    /// Verifies that the GenerateCodeVerifier method returns a string containing only URL-safe characters.
    /// </summary>
    [Fact]
    public void GenerateCodeVerifier_WhenCalled_ContainsOnlyUrlSafeCharacters()
    {
        // Arrange & Act
        var verifier = _service.GenerateCodeVerifier();

        // Assert — unreserved characters per RFC 7636: [A-Z][a-z][0-9]-._~
        verifier.Should().MatchRegex(@"^[A-Za-z0-9\-._~]+$",
            "code verifiers must use only unreserved URL-safe characters");
    }

    /// <summary>
    /// Verifies that the GenerateCodeChallenge method with the "plain" method returns the verifier unchanged.
    /// </summary>
    [Fact]
    public void GenerateCodeChallenge_WithPlainMethod_ReturnsVerifierUnchanged()
    {
        // Arrange
        var verifier = _service.GenerateCodeVerifier();

        // Act
        var challenge = _service.GenerateCodeChallenge(verifier, "plain");

        // Assert
        challenge.Should().Be(verifier,
            "plain method echoes the verifier directly without transformation");
    }

    /// <summary>
    /// Verifies that the GenerateCodeChallenge method with the "S256" method produces a deterministic base64 URL hash.
    /// </summary>
    [Fact]
    public void GenerateCodeChallenge_WithS256Method_ProducesDeterministicBase64UrlHash()
    {
        // Arrange
        var verifier = _service.GenerateCodeVerifier();

        // Act — call twice with the same verifier
        var first = _service.GenerateCodeChallenge(verifier, "S256");
        var second = _service.GenerateCodeChallenge(verifier, "S256");

        // Assert
        first.Should().Be(second, "SHA-256 hashing is deterministic");
        first.Length.Should().Be(43, "base64url-encoded SHA-256 always produces 43 characters after padding removal");
        first.Should().NotBe(verifier, "the challenge must differ from the verifier for S256");
    }

    /// <summary>
    /// Verifies that the ValidateCodeVerifier method returns true when the verifier matches the stored S256 challenge.
    /// </summary>
    [Fact]
    public void ValidateCodeVerifier_WhenVerifierMatchesStoredS256Challenge_ReturnsTrue()
    {
        // Arrange
        var verifier = _service.GenerateCodeVerifier();
        var challenge = _service.GenerateCodeChallenge(verifier, "S256");

        // Act
        var result = _service.ValidateCodeVerifier(verifier, challenge, "S256");

        // Assert
        result.Should().BeTrue("the generated challenge must be verifiable with its source verifier");
    }

    /// <summary>
    /// Verifies that the ValidateCodeVerifier method returns false when the verifier is null.
    /// </summary>
    [Fact]
    public void ValidateCodeVerifier_WhenVerifierIsNull_ReturnsFalseWithoutThrowing()
    {
        // Arrange
        var challenge = _service.GenerateCodeChallenge(_service.GenerateCodeVerifier(), "S256");

        // Act
        var result = _service.ValidateCodeVerifier(null, challenge, "S256");

        // Assert
        result.Should().BeFalse("a null verifier cannot satisfy any challenge");
    }

    /// <summary>
    /// Verifies that the IsPkceRequired method returns true when the global requirement is enabled and the client is confidential.
    /// </summary>
    [Fact]
    public void IsPkceRequired_WhenGlobalRequirementEnabled_ReturnsTrueForConfidentialClients()
    {
        // Arrange — RequirePkceForAllClients is true in constructor setup

        // Act
        var result = _service.IsPkceRequired(isConfidentialClient: true);

        // Assert
        result.Should().BeTrue(
            "the server-wide PKCE flag overrides per-client confidentiality settings");
    }

    /// <summary>
    /// Verifies that the IsPkceRequired method returns true when the global requirement is disabled and the client is public.
    /// </summary>
    [Fact]
    public void IsPkceRequired_WhenGlobalRequirementDisabledAndClientIsPublic_ReturnsTrue()
    {
        // Arrange
        _options.RequirePkceForAllClients = false;
        var service = new PkceValidationService(_options, _loggerMock.Object);

        // Act — public clients (SPAs, mobile apps) always require PKCE
        var result = service.IsPkceRequired(isConfidentialClient: false);

        // Assert
        result.Should().BeTrue(
            "public clients cannot securely store secrets, so PKCE is mandatory");
    }

    /// <summary>
    /// Verifies that the IsPkceRequired method returns false when the global requirement is disabled and the client is confidential.
    /// </summary>
    [Fact]
    public void IsPkceRequired_WhenGlobalRequirementDisabledAndClientIsConfidential_ReturnsFalse()
    {
        // Arrange
        _options.RequirePkceForAllClients = false;
        var service = new PkceValidationService(_options, _loggerMock.Object);

        // Act
        var result = service.IsPkceRequired(isConfidentialClient: true);

        // Assert
        result.Should().BeFalse(
            "confidential clients may authenticate via client secret when global PKCE is not enforced");
    }
}
