// tests/dotnet-auth-server.Tests/WebhookClientTests.cs
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Events;
using DotnetAuthServer.Integration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace DotnetAuthServer.Tests;

/// <summary>
/// Simple test implementation of <see cref="IDomainEvent"/> used only for unit testing.
/// </summary>
internal sealed class TestDomainEvent : IDomainEvent
{
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    public string EventType { get; init; } = "test.event";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? RequestId { get; init; } = "req-123";
}

/// <summary>
/// Unit tests for <see cref="WebhookClient"/>.
/// </summary>
public sealed class WebhookClientTests
{
    private static HttpClient CreateHttpClient(HttpResponseMessage response, Action<HttpRequestMessage>? requestAssert = null)
    {
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, ct) => requestAssert?.Invoke(req))
            .ReturnsAsync(response)
            .Verifiable();

        return new HttpClient(handlerMock.Object);
    }

    [Fact]
    public async Task SendEventWebhookAsync_HappyPath_ReturnsSuccess()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var httpClient = CreateHttpClient(response);
        var loggerMock = new Mock<ILogger<WebhookClient>>();
        var options = new WebhookOptions { Enabled = true, Timeout = TimeSpan.FromSeconds(5) };
        var client = new WebhookClient(httpClient, loggerMock.Object, options);
        var testEvent = new TestDomainEvent();

        // Act
        var result = await client.SendEventWebhookAsync("https://example.com/webhook", testEvent);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
    }

    [Fact]
    public async Task SendEventWebhookAsync_ClientError_ReturnsFailureWithError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var httpClient = CreateHttpClient(response);
        var loggerMock = new Mock<ILogger<WebhookClient>>();
        var options = new WebhookOptions { Enabled = true, Timeout = TimeSpan.FromSeconds(5) };
        var client = new WebhookClient(httpClient, loggerMock.Object, options);
        var testEvent = new TestDomainEvent();

        // Act
        var result = await client.SendEventWebhookAsync("https://example.com/webhook", testEvent);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("HTTP 400", result.Error);
    }

    [Fact]
    public async Task SendEventWebhookAsync_NullUrl_ThrowsArgumentException()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var loggerMock = new Mock<ILogger<WebhookClient>>();
        var options = new WebhookOptions { Enabled = true };
        var client = new WebhookClient(httpClient, loggerMock.Object, options);
        var testEvent = new TestDomainEvent();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.SendEventWebhookAsync(null!, testEvent));
    }

    [Fact]
    public async Task SendEventWebhookAsync_NullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        var httpClient = new HttpClient(); // not used
        var loggerMock = new Mock<ILogger<WebhookClient>>();
        var options = new WebhookOptions { Enabled = true };
        var client = new WebhookClient(httpClient, loggerMock.Object, options);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await client.SendEventWebhookAsync("https://example.com/webhook", null!));
    }

    [Fact]
    public async Task SendEventWebhookAsync_Disabled_ReturnsFailureWithoutCallingHttp()
    {
        // Arrange
        var httpClient = new HttpClient(); // should not be called
        var loggerMock = new Mock<ILogger<WebhookClient>>();
        var options = new WebhookOptions { Enabled = false };
        var client = new WebhookClient(httpClient, loggerMock.Object, options);
        var testEvent = new TestDomainEvent();

        // Act
        var result = await client.SendEventWebhookAsync("https://example.com/webhook", testEvent);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("Webhooks disabled or URL missing", result.Error);
    }
}
