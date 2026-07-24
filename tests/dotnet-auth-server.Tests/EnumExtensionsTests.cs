#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DotnetAuthServer.Extensions;
using Xunit;

namespace DotnetAuthServer.Tests;

public class EnumExtensionsTests
{
    // Sample enum used for all tests
    private enum SampleEnum
    {
        [Description("First Value")]
        First,
        Second,
        Third
    }

    [Fact]
    public void ToDescriptionString_ReturnsDescription_WhenAttributeExists()
    {
        // Arrange
        var value = SampleEnum.First;

        // Act
        var description = value.ToDescriptionString();

        // Assert
        Assert.Equal("First Value", description);
    }

    [Fact]
    public void ToDescriptionString_ReturnsName_WhenNoDescriptionAttribute()
    {
        // Arrange
        var value = SampleEnum.Second;

        // Act
        var description = value.ToDescriptionString();

        // Assert
        Assert.Equal(nameof(SampleEnum.Second), description);
    }

    [Fact]
    public void FromString_ParsesValidString_IgnoringCase()
    {
        // Act
        var result = EnumExtensions.FromString<SampleEnum>("third");

        // Assert
        Assert.Equal(SampleEnum.Third, result);
    }

    [Theory]
    [InlineData(null)]
    public void FromString_ThrowsArgumentNullException_WhenNull(string? input)
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EnumExtensions.FromString<SampleEnum>(input!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void FromString_ThrowsArgumentException_WhenEmptyOrWhitespace(string input)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => EnumExtensions.FromString<SampleEnum>(input));
        Assert.Contains("is not a valid value", ex.Message);
    }

    [Fact]
    public void FromString_ThrowsArgumentException_WhenInvalidValue()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => EnumExtensions.FromString<SampleEnum>("Invalid"));
        Assert.Contains("Invalid", ex.Message);
    }

    [Fact]
    public void GetValues_ReturnsAllEnumValues()
    {
        // Act
        IEnumerable<SampleEnum> values = EnumExtensions.GetValues<SampleEnum>();

        // Assert
        var expected = new[] { SampleEnum.First, SampleEnum.Second, SampleEnum.Third };
        Assert.Equal(expected, values);
    }

    [Theory]
    [InlineData("First", true)]
    [InlineData("second", true)]
    [InlineData("nonexistent", false)]
    public void IsValidValue_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        bool result = EnumExtensions.IsValidValue<SampleEnum>(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsValidValue_ThrowsArgumentNullException_WhenNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => EnumExtensions.IsValidValue<SampleEnum>(null!));
    }
}
