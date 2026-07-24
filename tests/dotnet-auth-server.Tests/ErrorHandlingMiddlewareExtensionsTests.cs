using System;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ErrorHandlingMiddlewareExtensionsTests
{
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareExtensionsTests()
    {
        // Create a middleware instance with mock dependencies
        var next = new RequestDelegate(context => Task.CompletedTask);
        var loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _middleware = new ErrorHandlingMiddleware(next, loggerMock.Object);
    }

    [Fact]
    public void ToErrorResponse_WithNoError_ReturnsEmptyErrorResponse()
    {
        // Arrange
        _middleware.ClearError();

        // Act
        var result = _middleware.ToErrorResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Error);
        Assert.Null(result.ErrorDescription);
        Assert.Null(result.ErrorUri);
    }

    [Fact]
    public void ToErrorResponse_WithError_ReturnsCorrectErrorResponse()
    {
        // Arrange
        const string expectedError = "invalid_request";
        const string expectedDescription = "Invalid request parameters";
        const string expectedUri = "https://docs.example.com/errors/invalid_request";

        _middleware.SetErrorFromException(new AuthServerException(
            expectedError,
            expectedDescription,
            400,
            expectedDescription,
            expectedUri));

        // Act
        var result = _middleware.ToErrorResponse();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedError, result.Error);
        Assert.Equal(expectedDescription, result.ErrorDescription);
        Assert.Equal(expectedUri, result.ErrorUri);
    }

    [Fact]
    public void ToErrorResponse_NullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? nullMiddleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullMiddleware!.ToErrorResponse());
    }

    [Fact]
    public void SetErrorFromException_AuthServerException_SetsCorrectErrorProperties()
    {
        // Arrange
        var exception = new AuthServerException(
            "access_denied",
            "The resource owner or authorization server denied the request",
            403,
            "User does not have permission",
            "https://docs.example.com/errors/access_denied");

        // Act
        _middleware.SetErrorFromException(exception);

        // Assert - Verify through ToErrorResponse since fields are private
        var result = _middleware.ToErrorResponse();
        Assert.Equal("access_denied", result.Error);
        Assert.Equal("User does not have permission", result.ErrorDescription);
        Assert.Equal("https://docs.example.com/errors/access_denied", result.ErrorUri);
    }

    [Fact]
    public void SetErrorFromException_InvalidOperationException_SetsStandardError()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation performed");

        // Act
        _middleware.SetErrorFromException(exception);

        // Assert
        var result = _middleware.ToErrorResponse();
        Assert.Equal("invalid_request", result.Error);
        Assert.Equal("Invalid operation performed", result.ErrorDescription);
        Assert.Null(result.ErrorUri);
    }

    [Fact]
    public void SetErrorFromException_GenericException_SetsServerError()
    {
        // Arrange
        var exception = new Exception("Something went wrong");

        // Act
        _middleware.SetErrorFromException(exception);

        // Assert
        var result = _middleware.ToErrorResponse();
        Assert.Equal("server_error", result.Error);
        Assert.Equal("An internal server error occurred", result.ErrorDescription);
        Assert.Null(result.ErrorUri);
    }

    [Fact]
    public void SetErrorFromException_NullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? nullMiddleware = null;
        var exception = new Exception("test");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullMiddleware!.SetErrorFromException(exception));
    }

    [Fact]
    public void SetErrorFromException_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        var exception = new Exception("test");
        Exception? nullException = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _middleware.SetErrorFromException(nullException!));
    }

    [Fact]
    public void SerializeErrorToJson_WithError_ReturnsValidJson()
    {
        // Arrange
        _middleware.SetErrorFromException(new AuthServerException(
            "invalid_client",
            "Client authentication failed",
            401,
            "Invalid client credentials provided"));

        // Act
        var json = _middleware.SerializeErrorToJson();

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\"error\":\"invalid_client\"", json);
        Assert.Contains("\"error_description\":\"Invalid client credentials provided\"", json);
        Assert.Contains("\"error_uri\":null", json);
    }

    [Fact]
    public void SerializeErrorToJson_WithAllFields_ReturnsCompleteJson()
    {
        // Arrange
        _middleware.SetErrorFromException(new AuthServerException(
            "temporarily_unavailable",
            "Service temporarily unavailable",
            503,
            "The authorization server is currently unable to handle the request",
            "https://docs.example.com/errors/temporarily_unavailable"));

        // Act
        var json = _middleware.SerializeErrorToJson();

        // Assert
        Assert.Contains("\"error\":\"temporarily_unavailable\"", json);
        Assert.Contains("\"error_description\":\"The authorization server is currently unable to handle the request\"", json);
        Assert.Contains("\"error_uri\":\"https://docs.example.com/errors/temporarily_unavailable\"", json);
    }

    [Fact]
    public void SerializeErrorToJson_NoError_ReturnsJsonWithNullValues()
    {
        // Arrange
        _middleware.ClearError();

        // Act
        var json = _middleware.SerializeErrorToJson();

        // Assert
        Assert.Equal("{\"error\":null,\"error_description\":null,\"error_uri\":null}", json);
    }

    [Fact]
    public void SerializeErrorToJson_NullMiddleware_ThrowsArgumentNullException()
    {
        // Arrange
        ErrorHandlingMiddleware? nullMiddleware = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullMiddleware!.SerializeErrorToJson());
    }

    [Fact]
    public void HasError_NoError_ReturnsFalse()
    {
        // Arrange
        _middleware.ClearError();

        // Act
        var result = _middleware.HasError();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasError_WithError_ReturnsTrue()
    {
        // Arrange
        _middleware.SetErrorFromException(new AuthServerException("test_error", "Test description"));

        // Act
        var result = _middleware.HasError();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasError_WithErrorDescription_ReturnsTrue()
    {
        // Arrange
        _middleware.SetErrorFromException(new AuthServerException("test_error", "Test description"));

        // Act
        var result = _middleware.HasError();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasError_NullMiddleware_ReturnsFalse()
    {
        // Arrange
        ErrorHandlingMiddleware? nullMiddleware = null;

        // Act
        var result = nullMiddleware.HasError();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ClearError_ClearsAllErrorFields()
    {
        // Arrange
        _middleware.SetErrorFromException(new AuthServerException(
            "test_error",
            "Test description",
            400,
            null,
            "https://example.com"));

        // Verify error is set
        Assert.True(_middleware.HasError());
        Assert.NotNull(_middleware.ToErrorResponse().Error);

        // Act
        _middleware.ClearError();

        // Assert
        Assert.False(_middleware.HasError());
        Assert.Null(_middleware.ToErrorResponse().Error);
        Assert.Null(_middleware.ToErrorResponse().ErrorDescription);
        Assert.Null(_middleware.ToErrorResponse().ErrorUri);
    }

    [Fact]
    public void ClearError_NullMiddleware_DoesNotThrow()
    {
        // Arrange
        ErrorHandlingMiddleware? nullMiddleware = null;

        // Act
        nullMiddleware.ClearError();

        // Assert - no exception thrown
        Assert.True(true);
    }

    [Fact]
    public void ErrorResponseClass_Properties_AreSettableAndGettable()
    {
        // Arrange
        var response = new ErrorHandlingMiddlewareExtensions.ErrorResponse();

        // Act
        response.Error = "test_error";
        response.ErrorDescription = "Test description";
        response.ErrorUri = "https://example.com";

        // Assert
        Assert.Equal("test_error", response.Error);
        Assert.Equal("Test description", response.ErrorDescription);
        Assert.Equal("https://example.com", response.ErrorUri);
    }

    [Fact]
    public void ErrorResponseClass_Properties_AreNullable()
    {
        // Arrange
        var response = new ErrorHandlingMiddlewareExtensions.ErrorResponse();

        // Act & Assert
        Assert.Null(response.Error);
        Assert.Null(response.ErrorDescription);
        Assert.Null(response.ErrorUri);
    }
}