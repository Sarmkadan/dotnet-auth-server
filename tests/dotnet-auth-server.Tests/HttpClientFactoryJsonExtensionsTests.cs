using Xunit;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using DotnetAuthServer.Integration;

namespace DotnetAuthServer.Tests
{
    public class HttpClientFactoryJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_ReturnsJsonString()
        {
            // Arrange
            var config = new HttpClientFactoryConfig();
            var expectedJson = JsonSerializer.Serialize(config);

            // Act
            var actualJson = HttpClientFactoryJsonExtensions.ToJson(config);

            // Assert
            Assert.Equal(expectedJson, actualJson);
        }

        [Fact]
        public void ToJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => HttpClientFactoryJsonExtensions.ToJson(null));
        }

        [Fact]
        public void FromJson_HappyPath_ReturnsConfig()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new HttpClientFactoryConfig());
            var expectedConfig = new HttpClientFactoryConfig();

            // Act
            var actualConfig = HttpClientFactoryJsonExtensions.FromJson(json);

            // Assert
            Assert.Equal(expectedConfig, actualConfig);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act and Assert
            Assert.Throws<ArgumentNullException>(() => HttpClientFactoryJsonExtensions.FromJson(null));
        }

        [Fact]
        public void FromJson_EmptyJson_ReturnsNull()
        {
            // Act
            var actualConfig = HttpClientFactoryJsonExtensions.FromJson("");

            // Assert
            Assert.Null(actualConfig);
        }

        [Fact]
        public void TryFromJson_HappyPath_ReturnsTrue()
        {
            // Arrange
            var json = JsonSerializer.Serialize(new HttpClientFactoryConfig());
            var expectedConfig = new HttpClientFactoryConfig();

            // Act
            var actualResult = HttpClientFactoryJsonExtensions.TryFromJson(json, out var actualConfig);

            // Assert
            Assert.True(actualResult);
            Assert.Equal(expectedConfig, actualConfig);
        }

        [Fact]
        public void TryFromJson_NullInput_ReturnsFalse()
        {
            // Act
            var actualResult = HttpClientFactoryJsonExtensions.TryFromJson(null, out _);

            // Assert
            Assert.False(actualResult);
        }

        [Fact]
        public void TryFromJson_EmptyJson_ReturnsFalse()
        {
            // Act
            var actualResult = HttpClientFactoryJsonExtensions.TryFromJson("", out _);

            // Assert
            Assert.False(actualResult);
        }
    }
}
