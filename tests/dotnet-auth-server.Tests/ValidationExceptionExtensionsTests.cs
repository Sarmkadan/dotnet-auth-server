#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using System.Collections.Generic;
using DotnetAuthServer.Exceptions;
using FluentAssertions;
using Xunit;

public sealed class ValidationExceptionExtensionsTests
{
    [Fact]
    public void AddErrors_AddsMultipleErrorsToException()
    {
        // Arrange
        var exception = new ValidationException();
        var errors = new Dictionary<string, string>
        {
            { "username", "Username is required" },
            { "email", "Email is invalid" },
            { "password", "Password must be at least 8 characters" }
        };

        // Act
        var result = exception.AddErrors(errors);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Errors.Should().HaveCount(3);
        exception.Errors["username"].Should().Be("Username is required");
        exception.Errors["email"].Should().Be("Email is invalid");
        exception.Errors["password"].Should().Be("Password must be at least 8 characters");
    }

    [Fact]
    public void AddErrors_WithEmptyDictionary_DoesNotAddErrors()
    {
        // Arrange
        var exception = new ValidationException();
        var errors = new Dictionary<string, string>();

        // Act
        var result = exception.AddErrors(errors);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Errors.Should().BeEmpty();
    }

    [Fact]
    public void AddErrors_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        Dictionary<string, string> errors = new() { { "field", "error" } };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.AddErrors(null!, errors));
    }

    [Fact]
    public void AddErrors_WithNullErrorsDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrors(null!));
    }

    [Fact]
    public void AddErrors_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();
        var errors = new Dictionary<string, string>
        {
            { "field", null! }
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrors(errors));
    }

    [Fact]
    public void AddErrorWithContext_AddsErrorWithContextData()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>
        {
            { "minLength", 8 },
            { "maxLength", 64 },
            { "pattern", "^[a-zA-Z0-9]+$" }
        };

        // Act
        var result = exception.AddErrorWithContext("password", "Password must contain at least one uppercase letter", contextData);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Errors.Should().ContainKey("password");
        exception.Errors["password"].Should().Be(contextData);
    }

    [Fact]
    public void AddErrorWithContext_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.AddErrorWithContext(null!, "field", "error", contextData));
    }

    [Fact]
    public void AddErrorWithContext_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext(null!, "error", contextData));
    }

    [Fact]
    public void AddErrorWithContext_WithEmptyFieldName_ThrowsArgumentException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.AddErrorWithContext(string.Empty, "error", contextData));
    }

    [Fact]
    public void AddErrorWithContext_WithWhitespaceFieldName_DoesNotThrow()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act
        var result = exception.AddErrorWithContext("   ", "error", contextData);

        // Assert
        result.Should().BeSameAs(exception);
        exception.Errors.Should().ContainKey("   ");
    }

    [Fact]
    public void AddErrorWithContext_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext("field", null!, contextData));
    }

    [Fact]
    public void AddErrorWithContext_WithNullContextData_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext("field", "error", null!));
    }

    [Fact]
    public void MergeErrors_MergesErrorsFromSourceToTarget()
    {
        // Arrange
        var target = new ValidationException();
        target.AddError("field1", "error1");
        target.AddError("field2", "error2");

        var source = new ValidationException();
        source.AddError("field3", "error3");
        source.AddError("field4", "error4");

        // Act
        var result = target.MergeErrors(source);

        // Assert
        result.Should().BeSameAs(target);
        target.Errors.Should().HaveCount(4);
        target.Errors["field1"].Should().Be("error1");
        target.Errors["field2"].Should().Be("error2");
        target.Errors["field3"].Should().Be("error3");
        target.Errors["field4"].Should().Be("error4");
    }

    [Fact]
    public void MergeErrors_WithOverlappingFields_OverwritesExistingErrors()
    {
        // Arrange
        var target = new ValidationException();
        target.AddError("username", "Username already exists");
        target.AddError("email", "Email is invalid");

        var source = new ValidationException();
        source.AddError("username", "Username must be at least 3 characters");
        source.AddError("password", "Password is too weak");

        // Act
        var result = target.MergeErrors(source);

        // Assert
        result.Should().BeSameAs(target);
        target.Errors.Should().HaveCount(3);
        target.Errors["username"].Should().Be("Username must be at least 3 characters"); // overwritten
        target.Errors["email"].Should().Be("Email is invalid"); // preserved
        target.Errors["password"].Should().Be("Password is too weak"); // added
    }

    [Fact]
    public void MergeErrors_WithEmptySource_DoesNotModifyTarget()
    {
        // Arrange
        var target = new ValidationException();
        target.AddError("field1", "error1");

        var source = new ValidationException();

        // Act
        var result = target.MergeErrors(source);

        // Assert
        result.Should().BeSameAs(target);
        target.Errors.Should().HaveCount(1);
        target.Errors["field1"].Should().Be("error1");
    }

    [Fact]
    public void MergeErrors_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.MergeErrors(null!, source));
    }

    [Fact]
    public void MergeErrors_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => target.MergeErrors(null!));
    }

    [Fact]
    public void HasError_ReturnsTrue_WhenFieldHasError()
    {
        // Arrange
        var exception = new ValidationException();
        exception.AddError("username", "Username is required");

        // Act
        var result = exception.HasError("username");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasError_ReturnsFalse_WhenFieldDoesNotHaveError()
    {
        // Arrange
        var exception = new ValidationException();
        exception.AddError("email", "Email is invalid");

        // Act
        var result = exception.HasError("username");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasError_WithNullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.HasError(null!, "field"));
    }

    [Fact]
    public void HasError_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.HasError(null!));
    }

    [Fact]
    public void HasError_WithEmptyFieldName_ThrowsArgumentException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.HasError(string.Empty));
    }

    [Fact]
    public void HasError_WithWhitespaceFieldName_DoesNotThrow()
    {
        // Arrange
        var exception = new ValidationException();
        exception.AddError("   ", "error");

        // Act
        var result = exception.HasError("   ");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void AddErrors_ChainsCorrectly()
    {
        // Arrange
        var exception = new ValidationException();
        var errors = new Dictionary<string, string> { { "field", "error" } };

        // Act
        var result = exception.AddErrors(errors);

        // Assert
        result.Should().BeSameAs(exception);
    }

    [Fact]
    public void AddErrorWithContext_ChainsCorrectly()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act
        var result = exception.AddErrorWithContext("field", "error", contextData);

        // Assert
        result.Should().BeSameAs(exception);
    }

    [Fact]
    public void MergeErrors_ChainsCorrectly()
    {
        // Arrange
        var target = new ValidationException();
        var source = new ValidationException();

        // Act
        var result = target.MergeErrors(source);

        // Assert
        result.Should().BeSameAs(target);
    }
}