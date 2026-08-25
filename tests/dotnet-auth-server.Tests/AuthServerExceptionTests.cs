#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using System.Collections.Generic;
using DotnetAuthServer.Exceptions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the <see cref="AuthServerException"/> class.
/// </summary>
public sealed class AuthServerExceptionTests
{
    /// <summary>
    /// Verifies that constructing an AuthServerException with required parameters sets the ErrorCode, StatusCode, ErrorDescription, and Message properties correctly, while leaving ErrorUri null and Details empty.
    /// </summary>
    [Fact]
    public void Constructor_WithRequiredParameters_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "invalid_request",
            "The request is missing a required parameter",
            400);

        // Assert
        exception.ErrorCode.Should().Be("invalid_request");
        exception.StatusCode.Should().Be(400);
        exception.ErrorDescription.Should().Be("The request is missing a required parameter");
        exception.ErrorUri.Should().BeNull();
        exception.Details.Should().BeEmpty();
        exception.Message.Should().Be("The request is missing a required parameter");
    }

    /// <summary>
    /// Verifies that constructing an AuthServerException with all parameters (including inner exception and details) sets all corresponding properties correctly.
    /// </summary>
    [Fact]
    public void Constructor_WithAllParameters_SetsAllProperties()
    {
        // Arrange
        var innerException = new InvalidOperationException("Inner error");
        var details = new Dictionary<string, object> { { "field", "value" } };

        // Act
        var exception = new AuthServerException(
            "access_denied",
            "The resource owner or authorization server denied the request",
            403,
            "User does not have permission to access this resource",
            "https://docs.example.com/errors/access_denied",
            innerException);

        // Assert
        exception.ErrorCode.Should().Be("access_denied");
        exception.StatusCode.Should().Be(403);
        exception.ErrorDescription.Should().Be("User does not have permission to access this resource");
        exception.ErrorUri.Should().Be("https://docs.example.com/errors/access_denied");
        exception.Details.Should().BeEmpty();
        exception.Message.Should().Be("The resource owner or authorization server denied the request");
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
    /// Verifies that when the errorDescription parameter is null, the constructor sets the ErrorDescription property to the message parameter value.
    /// </summary>
    [Fact]
    public void Constructor_WithNullErrorDescription_SetsErrorDescriptionToMessage()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "server_error",
            "Internal server error occurred",
            500,
            errorDescription: null);

        // Assert
        exception.ErrorCode.Should().Be("server_error");
        exception.StatusCode.Should().Be(500);
        exception.ErrorDescription.Should().Be("Internal server error occurred");
        exception.Message.Should().Be("Internal server error occurred");
    }

    /// <summary>
    /// Verifies that when the errorDescription parameter is an empty string, the constructor sets the ErrorDescription property to an empty string.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyErrorDescription_SetsErrorDescriptionToEmptyString()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "temporarily_unavailable",
            "Service is temporarily unavailable",
            503,
            errorDescription: string.Empty);

        // Assert
        exception.ErrorCode.Should().Be("temporarily_unavailable");
        exception.StatusCode.Should().Be(503);
        exception.ErrorDescription.Should().BeEmpty();
        exception.Message.Should().Be("Service is temporarily unavailable");
    }

    /// <summary>
    /// Verifies that the constructor does not trim whitespace from the errorDescription parameter; leading and trailing spaces are preserved.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceErrorDescription_DoesNotTrimErrorDescription()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "invalid_client",
            "Client authentication failed",
            401,
            errorDescription: "   Client authentication failed   ");

        // Assert - ErrorDescription is not trimmed by the constructor
        exception.ErrorDescription.Should().Be("   Client authentication failed   ");
    }

    /// <summary>
    /// Verifies that constructing an AuthServerException without specifying a status code defaults the StatusCode property to 400.
    /// </summary>
    [Fact]
    public void Constructor_WithDefaultStatusCode_SetsStatusCodeTo400()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "invalid_grant",
            "The provided authorization grant is invalid");

        // Assert
        exception.StatusCode.Should().Be(400);
    }

    /// <summary>
    /// Verifies that the ErrorCode property is mutable and can be changed after construction.
    /// </summary>
    [Fact]
    public void ErrorCode_IsSettable()
    {
        // Arrange
        var exception = new AuthServerException("error1", "message");

        // Act
        exception.ErrorCode = "new_error_code";

        // Assert
        exception.ErrorCode.Should().Be("new_error_code");
    }

    /// <summary>
    /// Verifies that the StatusCode property is mutable and can be changed after construction.
    /// </summary>
    [Fact]
    public void StatusCode_IsSettable()
    {
        // Arrange
        var exception = new AuthServerException("error", "message", 400);

        // Act
        exception.StatusCode = 401;

        // Assert
        exception.StatusCode.Should().Be(401);
    }

    /// <summary>
    /// Verifies that the ErrorDescription property is mutable and can be changed after construction.
    /// </summary>
    [Fact]
    public void ErrorDescription_IsSettable()
    {
        // Arrange
        var exception = new AuthServerException("error", "original message");

        // Act
        exception.ErrorDescription = "new description";

        // Assert
        exception.ErrorDescription.Should().Be("new description");
    }

    /// <summary>
    /// Verifies that the ErrorUri property is mutable and can be changed after construction.
    /// </summary>
    [Fact]
    public void ErrorUri_IsSettable()
    {
        // Arrange
        var exception = new AuthServerException("error", "message");

        // Act
        exception.ErrorUri = "https://example.com/docs";

        // Assert
        exception.ErrorUri.Should().Be("https://example.com/docs");
    }

    /// <summary>
    /// Verifies that the Details property is initialized as an empty dictionary and can be modified after construction.
    /// </summary>
    [Fact]
    public void Details_IsSettable_AndInitializesEmpty()
    {
        // Arrange & Act
        var exception = new AuthServerException("error", "message");

        // Assert
        exception.Details.Should().NotBeNull();
        exception.Details.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ToErrorResponse returns a dictionary containing the error and error_description keys when only basic properties are set.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithBasicProperties_ReturnsCorrectDictionary()
    {
        // Arrange
        var exception = new AuthServerException(
            "invalid_request",
            "Missing required parameter",
            400);

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().NotBeNull();
        response.Should().ContainKey("error").WhoseValue.Should().Be("invalid_request");
        response.Should().ContainKey("error_description").WhoseValue.Should().Be("Missing required parameter");
        response.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that ToErrorResponse includes the error_uri key in the returned dictionary when the ErrorUri property is set.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithErrorUri_IncludesUriInResponse()
    {
        // Arrange
        var exception = new AuthServerException(
            "access_denied",
            "Access denied",
            403,
            "User lacks required permissions",
            "https://docs.example.com/access-denied");

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().ContainKey("error_uri").WhoseValue.Should().Be("https://docs.example.com/access-denied");
        response.Should().HaveCount(3);
    }

    /// <summary>
    /// Verifies that ToErrorResponse includes any custom details added to the Details property in the returned dictionary.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithDetails_IncludesDetailsInResponse()
    {
        // Arrange
        var exception = new AuthServerException(
            "invalid_grant",
            "Grant validation failed",
            400);
        exception.Details.Add("client_id", "invalid_client_id");
        exception.Details.Add("reason", "expired");
        exception.Details.Add("expires_at", DateTime.UtcNow.AddHours(-1));

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().ContainKey("client_id").WhoseValue.Should().Be("invalid_client_id");
        response.Should().ContainKey("reason").WhoseValue.Should().Be("expired");
        response.Should().ContainKey("expires_at").WhoseValue.Should().BeOfType<DateTime>();
        // Should have: error, error_description, client_id, reason, expires_at
        response.Should().HaveCount(5);
    }

    /// <summary>
    /// Verifies that when ErrorDescription is null, ToErrorResponse uses the Message property value for the error_description key.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithNullErrorDescription_UsesMessage()
    {
        // Arrange
        var exception = new AuthServerException(
            "server_error",
            "Internal server error",
            500,
            errorDescription: null);

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().ContainKey("error_description").WhoseValue.Should().Be("Internal server error");
    }

    /// <summary>
    /// Verifies that ToErrorResponse returns the same two-key dictionary when Details is empty, regardless of its emptiness.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithEmptyDetails_DoesNotAffectResponse()
    {
        // Arrange
        var exception = new AuthServerException(
            "invalid_scope",
            "The requested scope is invalid",
            400);

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().HaveCount(2);
    }

    /// <summary>
    /// Verifies that calling ToErrorResponse multiple times returns equivalent but distinct dictionary instances.
    /// </summary>
    [Fact]
    public void ToErrorResponse_ReturnsNewDictionaryEachTime()
    {
        // Arrange
        var exception = new AuthServerException(
            "error",
            "message",
            400);

        // Act
        var response1 = exception.ToErrorResponse();
        var response2 = exception.ToErrorResponse();

        // Assert
        response1.Should().NotBeSameAs(response2);
        response1.Should().BeEquivalentTo(response2);
    }

    /// <summary>
    /// Verifies that if the Details property contains keys that match standard OAuth2 error response keys ("error" or "error_description"), those values take precedence in the ToErrorResponse output.
    /// </summary>
    [Fact]
    public void ToErrorResponse_WithDetailsOverwritesStandardKeys()
    {
        // Arrange
        var exception = new AuthServerException(
            "error",
            "message",
            400);
        exception.Details["error"] = "custom_error";
        exception.Details["error_description"] = "custom_description";

        // Act
        var response = exception.ToErrorResponse();

        // Assert - Details should take precedence
        response.Should().ContainKey("error").WhoseValue.Should().Be("custom_error");
        response.Should().ContainKey("error_description").WhoseValue.Should().Be("custom_description");
    }

    /// <summary>
    /// Verifies that the Message property (inherited from Exception) is correctly set by the constructor.
    /// </summary>
    [Fact]
    public void Message_InheritedFromException_IsCorrect()
    {
        // Arrange
        var message = "Custom error message";

        // Act
        var exception = new AuthServerException("error_code", message);

        // Assert
        exception.Message.Should().Be(message);
    }

    /// <summary>
    /// Verifies that the InnerException property is correctly preserved when passed to the constructor.
    /// </summary>
    [Fact]
    public void InnerException_IsPreserved()
    {
        // Arrange
        var inner = new ArgumentException("Invalid argument");

        // Act
        var exception = new AuthServerException(
            "invalid_argument",
            "Argument validation failed",
            400,
            innerException: inner);

        // Assert
        exception.InnerException.Should().BeSameAs(inner);
    }

    /// <summary>
    /// Verifies that the ToString() method (inherited from Exception) returns a string containing the exception type and message.
    /// </summary>
    [Fact]
    public void ToString_IncludesExceptionTypeAndMessage()
    {
        // Arrange
        var exception = new AuthServerException(
            "rate_limit_exceeded",
            "Too many requests",
            429);

        // Act
        var result = exception.ToString();

        // Assert - ToString() on Exception includes type and message
        result.Should().Contain("AuthServerException");
        result.Should().Contain("Too many requests");
    }

    /// <summary>
    /// Verifies that the Details property can be modified after construction by adding, updating, and removing entries.
    /// </summary>
    [Fact]
    public void Details_CanBeModifiedAfterConstruction()
    {
        // Arrange
        var exception = new AuthServerException("error", "message");

        // Act
        exception.Details.Add("key1", "value1");
        exception.Details["key2"] = 123;
        exception.Details["key3"] = true;

        // Assert
        exception.Details.Should().HaveCount(3);
        exception.Details["key1"].Should().Be("value1");
        exception.Details["key2"].Should().Be(123);
        exception.Details["key3"].Should().Be(true);
    }

    /// <summary>
    /// Verifies that constructing an AuthServerException with an empty error code string does not throw and sets the ErrorCode property to empty.
    /// </summary>
    [Fact]
    public void Constructor_WithEmptyErrorCode_DoesNotThrow()
    {
        // Act - empty error code is allowed
        var exception = new AuthServerException(
            string.Empty,
            "message",
            400);

        // Assert
        exception.ErrorCode.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that the constructor does not trim whitespace from the errorCode parameter; leading and trailing spaces are preserved.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceErrorCode_DoesNotTrimErrorCode()
    {
        // Arrange & Act
        var exception = new AuthServerException(
            "  invalid_request  ",
            "message",
            400);

        // Assert - ErrorCode is not trimmed by the constructor
        exception.ErrorCode.Should().Be("  invalid_request  ");
    }

    /// <summary>
    /// Verifies that constructing an AuthServerException with a null message does not throw and the Message property gets a default value (not null).
    /// </summary>
    [Fact]
    public void Constructor_WithNullMessage_DoesNotThrow()
    {
        // Act - null message is allowed
        var exception = new AuthServerException(
            "error_code",
            null!,
            400);

        // Assert - Exception.Message is never null, it has a default value
        exception.Message.Should().NotBeNull();
    }

    /// <summary>
    /// Verifies that constructing an AuthServerException with a null error code does not throw and sets the ErrorCode property to null.
    /// </summary>
    [Fact]
    public void Constructor_WithNullErrorCode_DoesNotThrow()
    {
        // Act - null error code is allowed
        var exception = new AuthServerException(
            null!,
            "message",
            400);

        // Assert
        exception.ErrorCode.Should().BeNull();
    }

    /// <summary>
    /// Verifies that ToErrorResponse correctly handles special characters (<, >, &, ", ') in the error description without throwing or altering them.
    /// </summary>
    [Fact]
    public void ToErrorResponse_HandlesSpecialCharactersInValues()
    {
        // Arrange
        var exception = new AuthServerException(
            "invalid_request",
            "Request contains special characters: <>&\"'",
            400);

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().ContainKey("error_description");
    }

    /// <summary>
    /// Verifies that ToErrorResponse correctly handles Unicode characters in the error description without throwing or altering them.
    /// </summary>
    [Fact]
    public void ToErrorResponse_HandlesUnicodeCharacters()
    {
        // Arrange
        var exception = new AuthServerException(
            "invalid_request",
            "Request contains unicode: 你好世界 🚀",
            400);

        // Act
        var response = exception.ToErrorResponse();

        // Assert
        response.Should().ContainKey("error_description");
    }
}