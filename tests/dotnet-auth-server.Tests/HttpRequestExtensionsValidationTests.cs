#nullable enable
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using DotnetAuthServer.Extensions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace DotnetAuthServer.Tests;

public class HttpRequestExtensionsValidationTests
{
    private static HttpRequest CreateRequest(
        string? bearerToken = null,
        string? clientId = null,
        string? clientSecret = null,
        IPAddress? remoteIp = null)
    {
        var context = new DefaultHttpContext();

        // Set remote IP address if supplied
        if (remoteIp != null)
        {
            context.Connection.RemoteIpAddress = remoteIp;
        }

        // Set Authorization header for bearer token or basic auth
        if (!string.IsNullOrEmpty(bearerToken))
        {
            context.Request.Headers["Authorization"] = $"Bearer {bearerToken}";
        }
        else if (!string.IsNullOrEmpty(clientId) || !string.IsNullOrEmpty(clientSecret))
        {
            // Basic auth expects "clientId:clientSecret" base64‑encoded
            var credentials = $"{clientId ?? string.Empty}:{clientSecret ?? string.Empty}";
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            context.Request.Headers["Authorization"] = $"Basic {encoded}";
        }

        return context.Request;
    }

    [Fact]
    public void Validate_HappyPath_ReturnsEmpty()
    {
        // Arrange: valid bearer token (>=10 chars) and non‑localhost IP
        var request = CreateRequest(
            bearerToken: new string('a', 12),
            remoteIp: IPAddress.Parse("192.168.0.1"));

        // Act
        IReadOnlyList<string> problems = request.Validate();

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void IsValid_HappyPath_ReturnsTrue()
    {
        var request = CreateRequest(
            bearerToken: new string('b', 15),
            remoteIp: IPAddress.Parse("10.0.0.5"));

        bool result = request.IsValid();

        Assert.True(result);
    }

    [Fact]
    public void EnsureValid_HappyPath_DoesNotThrow()
    {
        var request = CreateRequest(
            bearerToken: new string('c', 20),
            remoteIp: IPAddress.Parse("8.8.8.8"));

        var exception = Record.Exception(() => request.EnsureValid());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_WhitespaceClientId_AddsProblem()
    {
        var request = CreateRequest(
            clientId: "   ",
            clientSecret: "secret",
            remoteIp: IPAddress.Parse("203.0.113.1"));

        var problems = request.Validate();

        Assert.Contains("Client ID contains only whitespace characters", problems);
    }

    [Fact]
    public void Validate_LocalhostIp_AddsProblem()
    {
        var request = CreateRequest(
            bearerToken: new string('d', 11),
            remoteIp: IPAddress.Loopback); // 127.0.0.1

        var problems = request.Validate();

        Assert.Contains("IP address is localhost (::1 or 127.0.0.1)", problems);
    }

    [Fact]
    public void Validate_ShortBearerToken_AddsProblem()
    {
        var request = CreateRequest(
            bearerToken: "short",
            remoteIp: IPAddress.Parse("203.0.113.5"));

        var problems = request.Validate();

        Assert.Contains("Bearer token is too short (less than 10 characters)", problems);
    }

    [Fact]
    public void Validate_NullRequest_ThrowsArgumentNullException()
    {
        HttpRequest? request = null;

        Assert.Throws<ArgumentNullException>(() => request!.Validate());
    }

    [Fact]
    public void EnsureValid_WithProblems_ThrowsArgumentException_WithAllMessages()
    {
        var request = CreateRequest(
            clientId: "   ",
            clientSecret: "   ",
            bearerToken: "short",
            remoteIp: IPAddress.Loopback);

        var ex = Assert.Throws<ArgumentException>(() => request.EnsureValid());

        // All three problem messages should be present
        Assert.Contains("Client ID contains only whitespace characters", ex.Message);
        Assert.Contains("IP address is localhost (::1 or 127.0.0.1)", ex.Message);
        Assert.Contains("Bearer token is too short (less than 10 characters)", ex.Message);
    }
}
