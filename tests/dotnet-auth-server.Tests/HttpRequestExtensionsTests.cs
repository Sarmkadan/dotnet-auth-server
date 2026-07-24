#nullable enable
using System;
using System.Collections.Generic;
using DotnetAuthServer.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public class HttpRequestExtensionsTests
{
    private readonly Mock<HttpRequest> _requestMock = new();

    [Fact]
    public void GetOAuthParameter_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpRequest? nullRequest = null;
        const string parameterName = "client_id";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetOAuthParameter(parameterName));
    }

    [Fact]
    public void GetOAuthParameter_WithNullParameterName_ThrowsArgumentNullException()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        string? nullParameterName = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => request.GetOAuthParameter(nullParameterName!));
    }

    [Fact]
    public void GetOAuthParameter_WithEmptyParameterName_ReturnsNull()
    {
        // Arrange
        var request = new DefaultHttpContext().Request;
        const string emptyParameterName = "";

        // Act
        var result = request.GetOAuthParameter(emptyParameterName);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOAuthParameter_WithQueryParameter_ReturnsValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { ["client_id"] = "test_client" });
        var request = context.Request;

        // Act
        var result = request.GetOAuthParameter("client_id");

        // Assert
        result.Should().Be("test_client");
    }

    [Fact]
    public void GetOAuthParameter_WithFormParameter_ReturnsValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_secret"] = "secret123" });
        var request = context.Request;

        // Act
        var result = request.GetOAuthParameter("client_secret");

        // Assert
        result.Should().Be("secret123");
    }

    [Fact]
    public void GetOAuthParameter_WithQueryParameterPriorityOverForm_ReturnsQueryValue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { ["client_id"] = "from_query" });
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues> { ["client_id"] = "from_form" });
        var request = context.Request;

        // Act
        var result = request.GetOAuthParameter("client_id");

        // Assert
        result.Should().Be("from_query");
    }

    [Fact]
    public void GetOAuthParameter_WithNonExistentParameter_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();
        var request = context.Request;

        // Act
        var result = request.GetOAuthParameter("non_existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetOAuthParameter_WithEmptyQueryCollection_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection();
        var request = context.Request;

        // Act
        var result = request.GetOAuthParameter("any_param");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractClientCredentials_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.ExtractClientCredentials());
    }

    [Fact]
    public void ExtractClientCredentials_WithQueryParameters_ReturnsCredentials()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["client_id"] = "test_client",
            ["client_secret"] = "test_secret"
        });
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("test_client");
        clientSecret.Should().Be("test_secret");
    }

    [Fact]
    public void ExtractClientCredentials_WithFormParameters_ReturnsCredentials()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.ContentType = "application/x-www-form-urlencoded";
        context.Request.Form = new FormCollection(new Dictionary<string, StringValues>
        {
            ["client_id"] = "form_client",
            ["client_secret"] = "form_secret"
        });
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("form_client");
        clientSecret.Should().Be("form_secret");
    }

    [Fact]
    public void ExtractClientCredentials_WithBasicAuthHeader_ReturnsCredentialsFromHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("header_client:header_secret"));
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("header_client");
        clientSecret.Should().Be("header_secret");
    }

    [Fact]
    public void ExtractClientCredentials_WithBasicAuthHeaderAndQueryParameters_PrioritizesHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("header_client:header_secret"));
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["client_id"] = "query_client",
            ["client_secret"] = "query_secret"
        });
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("header_client");
        clientSecret.Should().Be("header_secret");
    }

    [Fact]
    public void ExtractClientCredentials_WithMalformedBasicAuthHeader_FallsBackToQueryParameters()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic malformed_base64!!!";
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["client_id"] = "fallback_client",
            ["client_secret"] = "fallback_secret"
        });
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("fallback_client");
        clientSecret.Should().Be("fallback_secret");
    }

    [Fact]
    public void ExtractClientCredentials_WithBasicAuthHeaderMissingColon_ReturnsEmptySecret()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("client_without_colon"));
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("client_without_colon");
        clientSecret.Should().BeEmpty();
    }

    [Fact]
    public void ExtractClientCredentials_WithNoCredentials_ReturnsNulls()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().BeNull();
        clientSecret.Should().BeNull();
    }

    [Fact]
    public void ExtractClientCredentials_WithOnlyClientId_ReturnsClientIdAndNullSecret()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Query = new QueryCollection(new Dictionary<string, StringValues> { ["client_id"] = "partial_client" });
        var request = context.Request;

        // Act
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        // Assert
        clientId.Should().Be("partial_client");
        clientSecret.Should().BeNull();
    }

    // ExtractBasicAuthCredentials is a private method and is tested indirectly through ExtractClientCredentials
    // which uses it internally

    [Fact]
    public void GetClientIpAddress_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetClientIpAddress());
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedForHeader_ReturnsFirstIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.1, 10.0.0.1, 172.16.0.1";
        var request = context.Request;

        // Act
        var result = request.GetClientIpAddress();

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedForHeaderWithEmptyEntries_ReturnsFirstNonEmptyIp()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = ",, 192.168.1.1, ,";
        var request = context.Request;

        // Act
        var result = request.GetClientIpAddress();

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedForHeaderAllEmpty_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = ",, ,";
        var request = context.Request;

        // Act
        var result = request.GetClientIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClientIpAddress_WithoutXForwardedForHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;

        // Act
        var result = request.GetClientIpAddress();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClientIpAddress_WithXForwardedForAndRemoteIp_ReturnsXForwardedFor()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Forwarded-For"] = "192.168.1.100";
        var request = context.Request;

        // Act
        var result = request.GetClientIpAddress();

        // Assert
        result.Should().Be("192.168.1.100");
    }

    [Fact]
    public void IsSecureTransport_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.IsSecureTransport());
    }

    [Fact]
    public void IsSecureTransport_WithHttps_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = true;
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSecureTransport_WithHttp_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecureTransport_WithXForwardedProtoHttps_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSecureTransport_WithXForwardedProtoHttp_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;
        context.Request.Headers["X-Forwarded-Proto"] = "http";
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsSecureTransport_WithXForwardedProtoCaseInsensitive_ReturnsTrue()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;
        context.Request.Headers["X-Forwarded-Proto"] = "HTTPS";
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsSecureTransport_WithMissingHeaders_ReturnsFalse()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.IsHttps = false;
        var request = context.Request;

        // Act
        var result = request.IsSecureTransport();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetBearerToken_WithNullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        HttpRequest? nullRequest = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullRequest!.GetBearerToken());
    }

    [Fact]
    public void GetBearerToken_WithoutAuthorizationHeader_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetBearerToken_WithNonBearerAuthorization_ReturnsNull()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Basic dXNlcjpwYXNz";
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetBearerToken_WithBearerAuthorization_ReturnsToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer my_token_123";
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().Be("my_token_123");
    }

    [Fact]
    public void GetBearerToken_WithBearerAuthorizationCaseInsensitive_ReturnsToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "BEARER my_token_456";
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().Be("my_token_456");
    }

    [Fact]
    public void GetBearerToken_WithBearerAuthorizationAndExtraSpaces_ReturnsToken()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer   spaced_token_789";
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().Be("  spaced_token_789");
    }

    [Fact]
    public void GetBearerToken_WithEmptyBearerToken_ReturnsEmptyString()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer ";
        var request = context.Request;

        // Act
        var result = request.GetBearerToken();

        // Assert
        result.Should().BeEmpty();
    }
}