#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using System.Collections.Generic;
using DotnetAuthServer.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains unit tests for the ValidationExceptionExtensions class.
/// Tests cover methods for adding, merging, and checking validation errors.
/// </summary>
public sealed class ValidationExceptionExtensionsTests
{
    /// <summary>
    /// Tests that AddErrors method correctly adds multiple errors to a ValidationException instance.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrors method does not add errors when provided with an empty dictionary.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrors method throws ArgumentNullException when the exception parameter is null.
    /// </summary>
    [Fact]
    public void AddErrors_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        Dictionary<string, string> errors = new() { { "field", "error" } };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.AddErrors(null!, errors));
    }

    /// <summary>
    /// Tests that AddErrors method throws ArgumentNullException when the errors dictionary parameter is null.
    /// </summary>
    [Fact]
    public void AddErrors_WithNullErrorsDictionary_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrors(null!));
    }

    /// <summary>
    /// Tests that AddErrors method throws ArgumentNullException when any error message in the dictionary is null.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrorWithContext method correctly adds an error with associated context data to a ValidationException.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrorWithContext method throws ArgumentNullException when the exception parameter is null.
    /// </summary>
    [Fact]
    public void AddErrorWithContext_WithNullException_ThrowsArgumentNullException()
    {
        // Arrange
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.AddErrorWithContext(null!, "field", "error", contextData));
    }

    /// <summary>
    /// Tests that AddErrorWithContext method throws ArgumentNullException when the field name parameter is null.
    /// </summary>
    [Fact]
    public void AddErrorWithContext_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext(null!, "error", contextData));
    }

    /// <summary>
    /// Tests that AddErrorWithContext method throws ArgumentException when the field name is an empty string.
    /// </summary>
    [Fact]
    public void AddErrorWithContext_WithEmptyFieldName_ThrowsArgumentException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.AddErrorWithContext(string.Empty, "error", contextData));
    }

    /// <summary>
    /// Tests that AddErrorWithContext method does not throw when the field name contains only whitespace.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrorWithContext method throws ArgumentNullException when the error message parameter is null.
    /// </summary>
    [Fact]
    public void AddErrorWithContext_WithNullErrorMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();
        var contextData = new Dictionary<string, object>();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext("field", null!, contextData));
    }

    /// <summary>
    /// Tests that AddErrorWithContext method throws ArgumentNullException when the context data parameter is null.
    /// </summary>
    [Fact]
    public void AddErrorWithContext_WithNullContextData_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.AddErrorWithContext("field", "error", null!));
    }

    /// <summary>
    /// Tests that MergeErrors method correctly merges errors from source ValidationException to target ValidationException.
    /// </summary>
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

    /// <summary>
    /// Tests that MergeErrors method overwrites existing errors when source and target have overlapping field names.
    /// </summary>
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

    /// <summary>
    /// Tests that MergeErrors method does not modify the target when the source ValidationException has no errors.
    /// </summary>
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

    /// <summary>
    /// Tests that MergeErrors method throws ArgumentNullException when the target parameter is null.
    /// </summary>
    [Fact]
    public void MergeErrors_WithNullTarget_ThrowsArgumentNullException()
    {
        // Arrange
        var source = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.MergeErrors(null!, source));
    }

    /// <summary>
    /// Tests that MergeErrors method throws ArgumentNullException when the source parameter is null.
    /// </summary>
    [Fact]
    public void MergeErrors_WithNullSource_ThrowsArgumentNullException()
    {
        // Arrange
        var target = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => target.MergeErrors(null!));
    }

    /// <summary>
    /// Tests that HasError method returns true when the specified field has an error.
    /// </summary>
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

    /// <summary>
    /// Tests that HasError method returns false when the specified field does not have an error.
    /// </summary>
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

    /// <summary>
    /// Tests that HasError method throws ArgumentNullException when the exception parameter is null.
    /// </summary>
    [Fact]
    public void HasError_WithNullException_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ValidationExceptionExtensions.HasError(null!, "field"));
    }

    /// <summary>
    /// Tests that HasError method throws ArgumentNullException when the field name parameter is null.
    /// </summary>
    [Fact]
    public void HasError_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => exception.HasError(null!));
    }

    /// <summary>
    /// Tests that HasError method throws ArgumentException when the field name is an empty string.
    /// </summary>
    [Fact]
    public void HasError_WithEmptyFieldName_ThrowsArgumentException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => exception.HasError(string.Empty));
    }

    /// <summary>
    /// Tests that HasError method does not throw when the field name contains only whitespace.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrors method returns the same exception instance for method chaining.
    /// </summary>
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

    /// <summary>
    /// Tests that AddErrorWithContext method returns the same exception instance for method chaining.
    /// </summary>
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

    /// <summary>
    /// Tests that MergeErrors method returns the same target exception instance for method chaining.
    /// </summary>
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