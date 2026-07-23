#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using DotnetAuthServer.Handlers;
using FluentAssertions;
using Xunit;

public sealed class ScopeMetadataHandlerValidationTests
{
    [Fact]
    public void Validate_ValidScopeMetadata_ShouldReturnNoErrors()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "custom_scope",
            DisplayName = "Custom Scope",
            Description = "A custom scope for testing",
            RequiresConsent = true,
            Icon = "custom-icon.png",
            RelatedScopes = ["openid", "profile"]
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_NullScope_ShouldThrowArgumentNullException()
    {
        // Arrange
        ScopeMetadata? scope = null;

        // Act
        Action act = () => scope!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NullOrWhitespaceName_ShouldReturnError(string? name)
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = name!,
            DisplayName = "Valid Display Name",
            Description = "Valid description"
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("Name cannot be null or whitespace");
    }

    [Fact]
    public void Validate_NameExceeds128Chars_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = new string('a', 129),
            DisplayName = "Valid Display Name",
            Description = "Valid description"
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("Name cannot exceed 128 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NullOrWhitespaceDisplayName_ShouldReturnError(string? displayName)
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = displayName!,
            Description = "Valid description"
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("DisplayName cannot be null or whitespace");
    }

    [Fact]
    public void Validate_DisplayNameExceeds256Chars_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = new string('a', 257),
            Description = "Valid description"
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("DisplayName cannot exceed 256 characters");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_NullOrWhitespaceDescription_ShouldReturnError(string? description)
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = description!
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("Description cannot be null or whitespace");
    }

    [Fact]
    public void Validate_DescriptionExceeds2048Chars_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = new string('a', 2049)
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("Description cannot exceed 2048 characters");
    }

    [Fact]
    public void Validate_IconExceeds512Chars_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description",
            Icon = new string('a', 513)
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("Icon cannot exceed 512 characters when set");
    }

    [Fact]
    public void Validate_NullRelatedScopesCollection_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description",
            RelatedScopes = null!
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("RelatedScopes collection cannot be null");
    }

    [Fact]
    public void Validate_RelatedScopesExceeds100Items_ShouldReturnError()
    {
        // Arrange
        var relatedScopes = new System.Collections.Generic.List<string>();
        for (int i = 0; i < 101; i++)
        {
            relatedScopes.Add($"scope_{i}");
        }

        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description",
            RelatedScopes = relatedScopes
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("RelatedScopes collection cannot contain more than 100 items");
    }

    [Fact]
    public void Validate_RelatedScopeNullOrWhitespace_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description",
            RelatedScopes = [null!, "", " ", "valid_scope"]
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().HaveCount(3);
        errors.Should().Contain(e => e.Contains("RelatedScopes[0] cannot be null or whitespace"));
        errors.Should().Contain(e => e.Contains("RelatedScopes[1] cannot be null or whitespace"));
        errors.Should().Contain(e => e.Contains("RelatedScopes[2] cannot be null or whitespace"));
    }

    [Fact]
    public void Validate_RelatedScopeExceeds128Chars_ShouldReturnError()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description",
            RelatedScopes = [new string('a', 129)]
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("RelatedScopes[0] cannot exceed 128 characters");
    }

    [Fact]
    public void IsValid_ValidScopeMetadata_ShouldReturnTrue()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description"
        };

        // Act
        var isValid = scope.IsValid();

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_InvalidScopeMetadata_ShouldReturnFalse()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "",
            DisplayName = "",
            Description = ""
        };

        // Act
        var isValid = scope.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValid_NullScope_ShouldReturnFalse()
    {
        // Arrange
        ScopeMetadata? scope = null;

        // Act
        var isValid = scope.IsValid();

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_ValidScopeMetadata_ShouldNotThrow()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "valid_name",
            DisplayName = "Valid Display Name",
            Description = "Valid description"
        };

        // Act
        Action act = () => scope.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_NullScope_ShouldThrowArgumentNullException()
    {
        // Arrange
        ScopeMetadata? scope = null;

        // Act
        Action act = () => scope!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsureValid_InvalidScope_ShouldThrowArgumentExceptionWithDetails()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "",
            DisplayName = "",
            Description = ""
        };

        // Act
        Action act = () => scope.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*ScopeMetadata validation failed*Name cannot be null or whitespace*DisplayName cannot be null or whitespace*Description cannot be null or whitespace*");
    }

    [Fact]
    public void Validate_AllPropertiesValid_ShouldReturnEmptyList()
    {
        // Arrange
        var scope = new ScopeMetadata
        {
            Name = "test_scope",
            DisplayName = new string('a', 256),
            Description = new string('b', 2048),
            Icon = new string('c', 512),
            RelatedScopes = [
                new string('d', 128),
                new string('e', 128)
            ]
        };

        // Act
        var errors = scope.Validate();

        // Assert
        errors.Should().BeEmpty();
    }
}