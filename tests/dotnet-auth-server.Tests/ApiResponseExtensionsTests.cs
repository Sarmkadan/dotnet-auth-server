using System;
using DotnetAuthServer.Domain.Models;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ApiResponseExtensionsTests
{
    [Fact]
    public void WithData_ReturnsSuccessResponse_WithProvidedData()
    {
        // Arrange
        var source = new ApiResponse(); // source instance is only used to satisfy the extension method signature
        var expected = "test-data";

        // Act
        var result = source.WithData(expected);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(expected, result.Data);
    }

    [Fact]
    public void WithError_SetsErrorMessage_AndSuccessFalse()
    {
        // Arrange
        var source = new ApiResponse();
        var errorMessage = "something went wrong";

        // Act
        var result = source.WithError(errorMessage);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.Error);
    }

    [Fact]
    public void WithError_WithStatusCode_SetsErrorAndCode()
    {
        // Arrange
        var source = new ApiResponse();
        var errorMessage = "bad request";
        var statusCode = 400;

        // Act
        var result = source.WithError(errorMessage, statusCode);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.Success);
        Assert.Equal(errorMessage, result.Error);
        Assert.Equal(statusCode, result.Code);
    }

    [Fact]
    public void WithMessage_AppendsWhenMessageAlreadyExists_AndDoesNotDuplicate()
    {
        // Arrange
        var source = ApiResponse.ErrorResponse("err");
        source.Message = "first";

        // Act
        source.WithMessage("second");
        source.WithMessage("second"); // duplicate should be ignored

        // Assert
        Assert.Equal("first | second", source.Message);
    }

    [Fact]
    public void HasData_ReturnsTrueWhenDataIsNotNull_AndFalseWhenNull()
    {
        // Arrange
        var withData = ApiResponse<string>.SuccessResponse("value");
        var withoutData = new ApiResponse<string>(); // Data defaults to null

        // Act & Assert
        Assert.True(withData.HasData());
        Assert.False(withoutData.HasData());
    }

    [Fact]
    public void IsSuccess_ReturnsSuccessProperty()
    {
        // Arrange
        var successResponse = ApiResponse.SuccessResponse();
        var errorResponse = ApiResponse.ErrorResponse("error");

        // Act & Assert
        Assert.True(successResponse.IsSuccess());
        Assert.False(errorResponse.IsSuccess());
    }

    [Fact]
    public void UpdateData_CopiesAllProperties_AndReplacesData()
    {
        // Arrange
        var original = new ApiResponse<string>
        {
            Success = true,
            Data = "old",
            Error = "none",
            Message = "msg",
            Code = 200,
            TraceId = "trace-123",
            Timestamp = DateTime.UtcNow
        };
        var newData = "new";

        // Act
        var updated = original.UpdateData(newData);

        // Assert
        Assert.NotSame(original, updated);
        Assert.Equal(original.Success, updated.Success);
        Assert.Equal(original.Error, updated.Error);
        Assert.Equal(original.Message, updated.Message);
        Assert.Equal(original.Code, updated.Code);
        Assert.Equal(original.TraceId, updated.TraceId);
        Assert.Equal(original.Timestamp, updated.Timestamp);
        Assert.Equal(newData, updated.Data);
    }

    [Fact]
    public void WithStatusCode_SetsCodeProperty()
    {
        // Arrange
        var response = new ApiResponse();
        var code = 418;

        // Act
        response.WithStatusCode(code);

        // Assert
        Assert.Equal(code, response.Code);
    }

    [Fact]
    public void WithTraceId_SetsTraceId_ToGuidString()
    {
        // Arrange
        var response = new ApiResponse();

        // Act
        response.WithTraceId();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(response.TraceId));
        Assert.True(Guid.TryParse(response.TraceId, out _));
    }

    [Fact]
    public void WithData_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.WithData("data"));
    }

    [Fact]
    public void WithError_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.WithError("error"));
    }

    [Fact]
    public void WithError_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var response = new ApiResponse();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response.WithError(null!));
    }

    [Fact]
    public void WithMessage_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.WithMessage("msg"));
    }

    [Fact]
    public void WithMessage_NullMessage_ThrowsArgumentNullException()
    {
        // Arrange
        var response = new ApiResponse();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => response.WithMessage(null!));
    }

    [Fact]
    public void HasData_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse<string>? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.HasData());
    }

    [Fact]
    public void IsSuccess_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.IsSuccess());
    }

    [Fact]
    public void UpdateData_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse<string>? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.UpdateData("new"));
    }

    [Fact]
    public void WithStatusCode_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.WithStatusCode(200));
    }

    [Fact]
    public void WithTraceId_NullResponse_ThrowsArgumentNullException()
    {
        // Arrange
        ApiResponse? nullResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullResponse!.WithTraceId());
    }
}
