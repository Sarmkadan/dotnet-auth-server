#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Tests;

using System;
using System.Collections.Generic;
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

/// <summary>
/// Tests for the <see cref="ErrorHandlingMiddleware"/> class, verifying that it correctly handles exceptions and returns appropriate error responses.
/// </summary>
public sealed class ErrorHandlingMiddlewareTests
{
    private readonly Mock<ILogger<ErrorHandlingMiddleware>> _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErrorHandlingMiddlewareTests"/> class.
    /// Sets up a mock logger.
    /// </summary>
    public ErrorHandlingMiddlewareTests()
    {
        _loggerMock = new Mock<ILogger<ErrorHandlingMiddleware>>();
    }

    /// <summary>
/// Gets or sets the error code for the test instance (used in <see cref="ToString"/>).
/// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Gets or sets the error description for the test instance (used in <see cref="ToString"/>).
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Gets or sets the error URI for the test instance (used in <see cref="ToString"/>).
    /// </summary>
    public string? ErrorUri { get; init; }

    /// <summary>
    /// Returns a string representation of the current test instance, displaying the error, error description, and error URI.
    /// </summary>
    /// <returns>A formatted string containing the error, error description, and error URI.</returns>
    public override string ToString() =>
        $"ErrorHandlingMiddlewareTests {{ Error = {Error}, ErrorDescription = {ErrorDescription}, ErrorUri = {ErrorUri} }}";

    private ErrorHandlingMiddleware CreateMiddleware(Exception exceptionToThrow)
    {
        RequestDelegate next = _ => throw exceptionToThrow;
        return new ErrorHandlingMiddleware(next, _loggerMock.Object);
    }

    /// <summary>
    /// Tests that the ErrorHandlingMiddleware constructor initializes correctly without throwing exceptions.
    /// </summary>
    [Fact]
    public void Constructor_InitializesProperties()
    {
        // Arrange & Act
        RequestDelegate next = context => Task.CompletedTask;
        var middleware = new ErrorHandlingMiddleware(next, _loggerMock.Object);

        // Assert
        middleware.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that when an AuthServerException with 400 status is thrown, the middleware returns the correct error response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith400Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ValidationException("Invalid request parameters");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(400);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("invalid_request");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("Invalid request parameters");
        response.Should().NotContainKey("error_uri");
    }

    /// <summary>
    /// Tests that when an AuthServerException with 401 status is thrown, the middleware returns the correct error response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith401Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidClientException("Client credentials are invalid");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("invalid_client");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("Client credentials are invalid");
        response.Should().NotContainKey("error_uri");
    }

    /// <summary>
    /// Tests that when an AuthServerException with 500 status is thrown, the middleware returns the correct error response.
    /// </summary>
    [Fact]
    public async Task InvokeAsync_AuthServerExceptionWith500Status_ReturnsCorrectErrorResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ConfigurationException("Server is misconfigured");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("server_error");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("Server is misconfigured");
        response.Should().NotContainKey("error_uri");
    }

    /// <summary>
    /// Tests that when an AuthServerException with a custom error code is thrown, the middleware returns the correct error response with custom error, description, and URI.
    /// </summary>
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
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("custom_error");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("Too many requests");
        response.Should().ContainKey("error_uri");
        response!["error_uri"].Should().Be("https://example.com/docs/rate-limiting");
    }

    /// <summary>
/// Tests that when an InvalidOperationException is thrown, the middleware returns a 400 Bad Request with snake_case error response.
/// </summary>
    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ReturnsBadRequestWithSnakeCaseResponse()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new InvalidOperationException("Operation is not valid in the current state");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("invalid_request");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("Operation is not valid in the current state");
        response.Should().ContainKey("error_uri");
        response!["error_uri"].Should().BeNull();
    }

    /// <summary>
/// Tests that when an unknown exception is thrown, the middleware returns a 500 Internal Server Error without leaking internal details in the error response.
/// </summary>
    [Fact]
    public async Task InvokeAsync_UnknownException_Returns500WithoutLeakingInternals()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Internal server error with sensitive details: password=secret123, api_key=abc123");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        var response = await ReadResponseAsync(context);
        response.Should().NotBeNull();
        response.Should().ContainKey("error");
        response!["error"].Should().Be("server_error");
        response.Should().ContainKey("error_description");
        response!["error_description"].Should().Be("An internal server error occurred");
        response.Should().ContainKey("error_uri");
        response!["error_uri"].Should().BeNull();

        // Verify sensitive details are NOT leaked in error description
        var errorDescription = response!["error_description"]?.ToString();
        errorDescription.Should().NotContain("password");
        errorDescription.Should().NotContain("secret123");
        errorDescription.Should().NotContain("api_key");
        errorDescription.Should().NotContain("abc123");
    }

    /// <summary>
/// Tests that the middleware sets the response content type to application/json when handling exceptions.
/// </summary>
    [Fact]
    public async Task InvokeAsync_ResponseContentTypeIsJson()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new Exception("Test error");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
    }

    /// <summary>
/// Tests that the middleware's error response uses snake_case property names as required by the OAuth 2.0 specification.
/// </summary>
    [Fact]
    public async Task InvokeAsync_ResponseUsesSnakeCaseNamingPolicy()
    {
        // Arrange
        var context = CreateHttpContext();
        var exception = new ValidationException("Test validation");
        var middleware = CreateMiddleware(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.ContentType.Should().StartWith("application/json");
        var response = await ReadResponseAsync(context);

        // Verify the response structure uses snake_case
        var json = JsonSerializer.Serialize(response);
        json.Should().Contain("error");
        json.Should().Contain("error_description");
    }



    private static HttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new System.IO.MemoryStream();
        return context;
    }

    private static async Task<Dictionary<string, object?>?> ReadResponseAsync(HttpContext context)
    {
        context.Response.Body.Seek(0, System.IO.SeekOrigin.Begin);
        var json = await new System.IO.StreamReader(context.Response.Body).ReadToEndAsync();
        var node = System.Text.Json.Nodes.JsonNode.Parse(json);
        if (node is not System.Text.Json.Nodes.JsonObject obj) return null;
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var kvp in obj)
        {
            result[kvp.Key] = kvp.Value is null ? null : kvp.Value.GetValueKind() == JsonValueKind.Null ? null : kvp.Value.GetValueKind() == JsonValueKind.String ? kvp.Value.GetValue<string>() : kvp.Value.ToJsonString();
        }
        return result;
    }
}
