#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Tests;

using System;
using DotnetAuthServer.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains unit tests for the <see cref="ValidationException"/> class.
/// </summary>
public sealed class ValidationExceptionTests
{
    /// <summary>
    /// Tests that the default constructor creates a ValidationException with the default message.
    /// </summary>
    [Fact]
    public void DefaultConstructor_CreatesExceptionWithDefaultMessage()
    {
        // Arrange & Act
        var exception = new ValidationException();

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Validation failed");
        exception.InnerException.Should().BeNull();
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the constructor with message and errorDescription sets the message correctly and leaves Errors empty.
    /// </summary>
    [Fact]
    public void DefaultConstructorWithErrorDescription_CreatesExceptionWithCustomDescription()
    {
        // Arrange & Act
        var exception = new ValidationException(
            message: "Custom validation message",
            errorDescription: "Custom error description"
        );

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Custom validation message");
        exception.InnerException.Should().BeNull();
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the constructor with innerException sets the InnerException correctly.
    /// </summary>
    [Fact]
    public void DefaultConstructorWithInnerException_CreatesExceptionWithInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");

        // Act
        var exception = new ValidationException(
            message: "Validation failed",
            errorDescription: null,
            innerException: innerException
        );

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Validation failed");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the constructor with fieldName, fieldValue, and validationRule creates a formatted message.
    /// </summary>
    [Fact]
    public void ParameterizedConstructor_WithFieldNameValueAndRule_CreatesExceptionWithFormattedMessage()
    {
        // Arrange & Act
        var exception = new ValidationException(
            fieldName: "email",
            fieldValue: "test@example.com",
            validationRule: "must be a valid email address"
        );

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Validation failed for email: 'test@example.com'. must be a valid email address");
        exception.InnerException.Should().BeNull();
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the constructor with fieldName, fieldValue, validationRule, and innerException sets the InnerException and formatted message.
    /// </summary>
    [Fact]
    public void ParameterizedConstructor_WithInnerException_CreatesExceptionWithInnerException()
    {
        // Arrange
        var innerException = new FormatException("Invalid format");

        // Act
        var exception = new ValidationException(
            fieldName: "username",
            fieldValue: "user@name",
            validationRule: "must contain only alphanumeric characters",
            innerException: innerException
        );

        // Assert
        exception.Should().NotBeNull();
        exception.Message.Should().Be("Validation failed for username: 'user@name'. must contain only alphanumeric characters");
        exception.InnerException.Should().BeSameAs(innerException);
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that the Errors property is initialized as an empty dictionary.
    /// </summary>
    [Fact]
    public void Errors_Property_IsInitializedAsEmptyDictionary()
    {
        // Arrange & Act
        var exception = new ValidationException();

        // Assert
        exception.Errors.Should().NotBeNull();
        exception.Errors.Should().BeEmpty();
        exception.Errors.Should().BeOfType<Dictionary<string, object>>();
    }

    /// <summary>
    /// Tests that AddError adds an error to the Errors dictionary.
    /// </summary>
    [Fact]
    public void AddError_AddsErrorToErrorsDictionary()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        exception.AddError("email", "Email is required");

        // Assert
        exception.Errors.Should().ContainKey("email");
        exception.Errors["email"].Should().Be("Email is required");
        exception.Errors.Should().HaveCount(1);
    }

    /// <summary>
    /// Tests that multiple calls to AddError store multiple errors.
    /// </summary>
    [Fact]
    public void AddError_MultipleErrors_AreStoredInDictionary()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        exception.AddError("email", "Email is required");
        exception.AddError("password", "Password must be at least 8 characters");
        exception.AddError("username", "Username must be unique");

        // Assert
        exception.Errors.Should().HaveCount(3);
        exception.Errors["email"].Should().Be("Email is required");
        exception.Errors["password"].Should().Be("Password must be at least 8 characters");
        exception.Errors["username"].Should().Be("Username must be unique");
    }

    /// <summary>
    /// Tests that AddError overwrites the error for the same field name.
    /// </summary>
    [Fact]
    public void AddError_OverwritesExistingError_WhenSameFieldNameUsed()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        exception.AddError("email", "Email is required");
        exception.AddError("email", "Email format is invalid");

        // Assert
        exception.Errors.Should().HaveCount(1);
        exception.Errors["email"].Should().Be("Email format is invalid");
    }

    /// <summary>
    /// Tests that AddError throws when fieldName is null.
    /// </summary>
    [Fact]
    public void AddError_WithNullFieldName_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        Action act = () => exception.AddError(null!, "Error message");

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that AddError allows an empty string as a field name.
    /// </summary>
    [Fact]
    public void AddError_WithEmptyFieldName_StoresEmptyStringAsKey()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        exception.AddError(string.Empty, "Error message");

        // Assert
        exception.Errors.Should().ContainKey(string.Empty);
        exception.Errors[string.Empty].Should().Be("Error message");
    }

    /// <summary>
    /// Tests that AddError allows a null error message.
    /// </summary>
    [Fact]
    public void AddError_WithNullErrorMessage_StoresNullValue()
    {
        // Arrange
        var exception = new ValidationException();

        // Act
        exception.AddError("email", null!);

        // Assert
        exception.Errors.Should().ContainKey("email");
        exception.Errors["email"].Should().BeNull();
    }

    /// <summary>
    /// Tests that the Errors dictionary is accessible and contains the added errors.
    /// </summary>
    [Fact]
    public void ErrorsDictionary_IsAccessibleAfterAddingErrors()
    {
        // Arrange
        var exception = new ValidationException();
        exception.AddError("field1", "error1");
        exception.AddError("field2", "error2");

        // Act
        var errors = exception.Errors;

        // Assert
        errors.Should().NotBeNull();
        errors.Should().HaveCount(2);
        errors.Should().ContainKeys("field1", "field2");
    }

    /// <summary>
    /// Tests that ValidationException inherits from AuthServerException.
    /// </summary>
    [Fact]
    public void Inheritance_ValidationException_InheritsFromAuthServerException()
    {
        // Arrange & Act
        var exception = new ValidationException();

        // Assert
        exception.Should().BeAssignableTo<AuthServerException>();
    }

    /// <summary>
    /// Tests that the ErrorCode property is always "invalid_request".
    /// </summary>
    [Fact]
    public void ErrorCode_IsAlwaysInvalidRequest()
    {
        // Arrange & Act
        var exception1 = new ValidationException();
        var exception2 = new ValidationException("email", "test@example.com", "must be valid");

        // Assert
        exception1.ErrorCode.Should().Be("invalid_request");
        exception2.ErrorCode.Should().Be("invalid_request");
    }

    /// <summary>
    /// Tests that the StatusCode property is always 400.
    /// </summary>
    [Fact]
    public void StatusCode_IsAlways400()
    {
        // Arrange & Act
        var exception1 = new ValidationException();
        var exception2 = new ValidationException("email", "test@example.com", "must be valid");

        // Assert
        exception1.StatusCode.Should().Be(400);
        exception2.StatusCode.Should().Be(400);
    }
}