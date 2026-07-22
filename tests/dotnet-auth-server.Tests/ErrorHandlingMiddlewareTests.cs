#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public sealed class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;
    private readonly RequestDelegate _next;
    private readonly ErrorHandlingMiddleware _middleware;

    public ErrorHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
        _next = context => throw new InvalidOperationException("Should not be called when exception is thrown");
        _middleware = new ErrorHandlingMiddleware(_next, _loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        var middleware = new ErrorHandlingMiddleware(_next, _loggerMock.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith400Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ValidationException("Invalid request parameters");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("invalid_request");
        response.ErrorDescription.Should().Be("Invalid request parameters");
        response.ErrorUri.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith401Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidClientException("Client credentials are invalid");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("invalid_client");
        response.ErrorDescription.Should().Be("Client credentials are invalid");
        response.ErrorUri.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith500Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ConfigurationException("Server is misconfigured");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("server_error");
        response.ErrorDescription.Should().Be("Server is misconfigured");
        response.ErrorUri.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWithCustomErrorCode_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new AuthServerException(
            "custom_error",
            "Custom error message",
            429,
            "Too many requests",
            "https://example.com/docs/rate-limiting"
        );

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("custom_error");
        response.ErrorDescription.Should().Be("Too many requests");
        response.ErrorUri.Should().Be("https://example.com/docs/rate-limiting");
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ReturnsBadRequestWithSnakeCaseResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Operation is not valid in the current state");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("invalid_request");
        response.ErrorDescription.Should().Be("Operation is not valid in the current state");
        response.ErrorUri.Should().BeNull();
    }

    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500WithoutLeakingInternals()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Internal server error with sensitive details: password=secret123, api_key=abc123");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Error.Should().Be("server_error");
        response.ErrorDescription.Should().Be("An internal server error occurred");
        response.ErrorUri.Should().BeNull();

        // Verify sensitive details are NOT leaked in error description
        var errorDescription = response.ErrorDescription;
        errorDescription.Should().NotContain("password");
        errorDescription.Should().NotContain("secret123");
        errorDescription.Should().NotContain("api_key");
        errorDescription.Should().NotContain("abc123");
    }

    [Fact]
    public async Task InvokeAsync_ResponseContentTypeIsJson()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Test error");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
    }

    [Fact]
    public async Task InvokeAsync_ResponseUsesSnakeCaseNamingPolicy()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ValidationException("Test validation");

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
        var response = await ReadResponseAsync(context);

        // Verify the response structure uses snake_case
        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("error");
        json.Should().Contain("error_description");
        json.Should().Contain("error_uri");
    }



    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();
        return context;
    }

    private static async Task<ErrorResponse?> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var json = await new System.IO.StreamReader(context.Response.Body).ReadToEndAsync();
        return JsonSerializer.Deserialize<ErrorResponse>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    private sealed class ErrorResponse
    {
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public string? ErrorUri { get; set; }
    }
}