#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Integration;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for WebhookClientValidation extension methods.
/// </summary>
public sealed class WebhookClientValidationTests
{
    // -------------------------------------------------------------------------
    // Validate method tests
    // -------------------------------------------------------------------------

    [Fact]
    public void Validate_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        WebhookOptions? options = null;

        // Act
        Action act = () => options.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Validate_WithValidOptions_ReturnsEmptyList()
    {
        // Arrange
        var options = new WebhookOptions
        {
            Enabled = true,
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeEmpty("valid options should produce no validation errors");
    }

    [Fact]
    public void Validate_WithNegativeMaxRetries_ReturnsError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = -1,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(1)
            .And.ContainMatch("*MaxRetries must be non-negative*");
    }

    [Fact]
    public void Validate_WithZeroMaxRetries_ReturnsNoError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 0,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeEmpty("zero retries is valid");
    }

    [Fact]
    public void Validate_WithInitialRetryDelayBelowMinimum_ReturnsError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 50,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(1)
            .And.ContainMatch("*InitialRetryDelayMs must be at least 100ms*");
    }

    [Fact]
    public void Validate_WithInitialRetryDelayAtMinimum_ReturnsNoError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 100,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeEmpty("minimum allowed value is valid");
    }

    [Fact]
    public void Validate_WithMaxRetryDelayLessThanInitialRetryDelay_ReturnsError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 5000,
            MaxRetryDelayMs = 1000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(1)
            .And.ContainMatch("*MaxRetryDelayMs*must be greater than or equal to InitialRetryDelayMs*");
    }

    [Fact]
    public void Validate_WithMaxRetryDelayEqualToInitialRetryDelay_ReturnsNoError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 1000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().BeEmpty("equal values are valid");
    }

    [Fact]
    public void Validate_WithZeroTimeout_ReturnsError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.Zero
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(1)
            .And.ContainMatch("*Timeout must be positive*");
    }

    [Fact]
    public void Validate_WithNegativeTimeout_ReturnsError()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(-1)
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(1)
            .And.ContainMatch("*Timeout must be positive*");
    }

    [Fact]
    public void Validate_WithMultipleInvalidProperties_ReturnsAllErrors()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = -1,
            InitialRetryDelayMs = 50,
            MaxRetryDelayMs = 1000,
            Timeout = TimeSpan.Zero
        };

        // Act
        var result = options.Validate();

        // Assert
        result.Should().HaveCount(3)
            .And.Contain("MaxRetries must be non-negative, but was -1")
            .And.Contain("InitialRetryDelayMs must be at least 100ms, but was 50")
            .And.Contain("Timeout must be positive, but was 00:00:00");
    }

    // -------------------------------------------------------------------------
    // IsValid method tests
    // -------------------------------------------------------------------------

    [Fact]
    public void IsValid_WithNullOptions_ReturnsFalse()
    {
        // Arrange
        WebhookOptions? options = null;

        // Act
        var result = options.IsValid();

        // Assert
        result.Should().BeFalse("null options should be considered invalid");
    }

    [Fact]
    public void IsValid_WithValidOptions_ReturnsTrue()
    {
        // Arrange
        var options = new WebhookOptions
        {
            Enabled = true,
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.IsValid();

        // Assert
        result.Should().BeTrue("valid options should return true");
    }

    [Fact]
    public void IsValid_WithInvalidOptions_ReturnsFalse()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = -1,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = options.IsValid();

        // Assert
        result.Should().BeFalse("invalid options should return false");
    }

    // -------------------------------------------------------------------------
    // EnsureValid method tests
    // -------------------------------------------------------------------------

    [Fact]
    public void EnsureValid_WithNullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        WebhookOptions? options = null;

        // Act
        Action act = () => options.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void EnsureValid_WithValidOptions_DoesNotThrow()
    {
        // Arrange
        var options = new WebhookOptions
        {
            Enabled = true,
            MaxRetries = 3,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        Action act = () => options.EnsureValid();

        // Assert
        act.Should().NotThrow("valid options should not throw");
    }

    [Fact]
    public void EnsureValid_WithInvalidOptions_ThrowsArgumentException()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = -1,
            InitialRetryDelayMs = 1000,
            MaxRetryDelayMs = 30000,
            Timeout = TimeSpan.FromSeconds(10)
        };

        // Act
        Action act = () => options.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Webhook configuration is invalid*MaxRetries must be non-negative*");
    }

    [Fact]
    public void EnsureValid_WithMultipleInvalidProperties_ThrowsArgumentExceptionWithAllErrors()
    {
        // Arrange
        var options = new WebhookOptions
        {
            MaxRetries = -1,
            InitialRetryDelayMs = 50,
            MaxRetryDelayMs = 1000,
            Timeout = TimeSpan.Zero
        };

        // Act
        Action act = () => options.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Webhook configuration is invalid*MaxRetries must be non-negative*InitialRetryDelayMs must be at least 100ms*Timeout must be positive*");
    }
}