using System;
using System.Threading.Tasks;
using DotnetAuthServer.Middleware;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotnetAuthServer.Tests;

public class RequestContextMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NoHeader_GeneratesRequestId_AndCleansUpLogicalContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new RequestContextMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert: request id is generated and stored
        Assert.True(context.Items.ContainsKey("RequestId"));
        var requestId = context.Items["RequestId"] as string;
        Assert.NotNull(requestId);
        // GUID without hyphens is 32 characters
        Assert.Equal(32, requestId!.Length);
        Assert.Equal(requestId, context.Response.Headers["X-Request-Id"]);

        // Assert: LogicalContext is cleared after the request completes
        Assert.Null(LogicalContext.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_HeaderPresent_UsesProvidedRequestId()
    {
        // Arrange
        var expected = "custom-request-id-12345";
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-Id"] = expected;

        var middleware = new RequestContextMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert: the same id is propagated to Items and Response header
        Assert.Equal(expected, context.Items["RequestId"]);
        Assert.Equal(expected, context.Response.Headers["X-Request-Id"]);

        // LogicalContext should be cleared after the request
        Assert.Null(LogicalContext.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_HeaderPresent_EmptyString_IsPreserved()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Request-Id"] = string.Empty;

        var middleware = new RequestContextMiddleware(_ => Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert: empty string is used as request id
        Assert.Equal(string.Empty, context.Items["RequestId"]);
        Assert.Equal(string.Empty, context.Response.Headers["X-Request-Id"]);
        Assert.Null(LogicalContext.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_NextDelegateThrows_LogicalContextIsCleared()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var middleware = new RequestContextMiddleware(_ => throw new InvalidOperationException("boom"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));

        // Ensure the LogicalContext is reset even when an exception bubbles up
        Assert.Null(LogicalContext.RequestId);
    }

    [Fact]
    public async Task InvokeAsync_LogicalContextIsSetDuringNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        string? capturedDuringNext = null;

        var middleware = new RequestContextMiddleware(async ctx =>
        {
            // Inside the next delegate the LogicalContext should already contain the request id
            capturedDuringNext = LogicalContext.RequestId;
            await Task.CompletedTask;
        });

        // Act
        await middleware.InvokeAsync(context);

        // Assert: the request id seen inside the next delegate matches the one stored in Items
        var requestId = context.Items["RequestId"] as string;
        Assert.NotNull(requestId);
        Assert.Equal(requestId, capturedDuringNext);
        // After the request completes the LogicalContext must be cleared
        Assert.Null(LogicalContext.RequestId);
    }
}
