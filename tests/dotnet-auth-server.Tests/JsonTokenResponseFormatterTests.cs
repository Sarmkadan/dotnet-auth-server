using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Formatters;
using Xunit;
using Moq;

namespace DotnetAuthServer.Tests.Formatters
{
    public class JsonTokenResponseFormatterTests
    {
        [Fact]
        public void FormatTokenResponse_Happy_PATH_Valid_RESPONSE()
        {
            // Arrange
            var response = new TokenResponse
            {
                AccessToken = "access_token",
                TokenType = "token_type",
                ExpiresIn = 3600,
                RefreshToken = "refresh_token",
                Scope = "scope"
            };
            var expectedJson = "{\"access_token\":\"access_token\",\"token_type\":\"token_type\",\"expires_in\":3600,\"refresh_token\":\"refresh_token\",\"scope\":\"scope\"}";

            // Act
            var json = JsonTokenResponseFormatter.FormatTokenResponse(response);

            // Assert
            Assert.Equal(expectedJson, json);
        }

        [Fact]
        public void FormatTokenResponse_NULL_RESPONSE_NULL()
        {
            // Arrange
            TokenResponse? response = null;

            // Act
            var json = JsonTokenResponseFormatter.FormatTokenResponse(response);

            // Assert
            Assert.Null(json);
        }

        [Fact]
        public void ParseTokenResponse_VALID_JSON_VALID_RESPONSE()
        {
            // Arrange
            var json = "{\"access_token\":\"access_token\",\"token_type\":\"token_type\",\"expires_in\":3600,\"refresh_token\":\"refresh_token\",\"scope\":\"scope\"}";
            var expectedResponse = new TokenResponse
            {
                AccessToken = "access_token",
                TokenType = "token_type",
                ExpiresIn = 3600,
                RefreshToken = "refresh_token",
                Scope = "scope"
            };

            // Act
            var response = JsonTokenResponseFormatter.ParseTokenResponse(json);

            // Assert
            Assert.Equal(expectedResponse, response);
        }

        [Fact]
        public void ParseTokenResponse_INVALID_JSON_NULL()
        {
            // Arrange
            var json = "invalid_json";

            // Act
            var response = JsonTokenResponseFormatter.ParseTokenResponse(json);

            // Assert
            Assert.Null(response);
        }
    }
}