using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using DotnetAuthServer.Configuration;
using Newtonsoft.Json;

namespace DotnetAuthServer.Tests
{
    public class OpaOptionsJsonExtensionsTests
    {
        [Fact]
        public void ToJson_Happy_PATH()
        {
            // Arrange
            var opaOptions = new OpaOptions();
            // Act
            var json = OpaOptionsJsonExtensions.ToJson(opaOptions);
            // Assert
            Assert.NotNull(json);
        }

        [Fact]
        public void FromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            // Act
            var opaOptions = OpaOptionsJsonExtensions.FromJson(json);
            // Assert
            Assert.NotNull(opaOptions);
        }

        [Fact]
        public void TryFromJson_HAPPY_PATH()
        {
            // Arrange
            var json = "{}";
            OpaOptions? opaOptions;
            // Act
            var result = OpaOptionsJsonExtensions.TryFromJson(json, out opaOptions);
            // Assert
            Assert.True(result);
            Assert.NotNull(opaOptions);
        }
    }
}