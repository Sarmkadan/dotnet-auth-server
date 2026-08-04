using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests
{
    public class AuthServerOptionsExtensionsTests
    {
        [Fact]
        public void Validate_HappyPath_ReturnsTrue()
        {
            // Arrange
            var options = new AuthServerOptions
            {
                IssuerUrl = "https://example.com",
                JwtSigningKey = "secret",
                JwtAlgorithm = "HS256",
                AccessTokenLifetimeSeconds = 3600,
                SupportedScopes = new[] { "scope1" },
                SupportedGrantTypes = new[] { "grant1" }
            };

            // Act
            var result = AuthServerOptionsExtensions.Validate(options);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void Validate_NullOptions_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => AuthServerOptionsExtensions.Validate(null));
        }

        [Fact]
        public void SupportsScope_HappyPath_ReturnsTrue()
        {
            // Arrange
            var options = new AuthServerOptions
            {
                SupportedScopes = new[] { "scope1" }
            };

            // Act
            var result = AuthServerOptionsExtensions.SupportsScope(options, "scope1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void SupportsGrantType_HappyPath_ReturnsTrue()
        {
            // Arrange
            var options = new AuthServerOptions
            {
                SupportedGrantTypes = new[] { "grant1" }
            };

            // Act
            var result = AuthServerOptionsExtensions.SupportsGrantType(options, "grant1");

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void GetAccessTokenLifetime_HappyPath_ReturnsTimeSpan()
        {
            // Arrange
            var options = new AuthServerOptions
            {
                AccessTokenLifetimeSeconds = 3600
            };

            // Act
            var result = AuthServerOptionsExtensions.GetAccessTokenLifetime(options);

            // Assert
            Assert.Equal(TimeSpan.FromHours(1), result);
        }

        [Fact]
        public void IsPkceRequired_HappyPath_ReturnsTrue()
        {
            // Arrange
            var options = new AuthServerOptions
            {
                RequirePkceForAllClients = true
            };

            // Act
            var result = AuthServerOptionsExtensions.IsPkceRequired(options);

            // Assert
            Assert.True(result);
        }
    }
}
