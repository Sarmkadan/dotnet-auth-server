#nullable enable
using System;
using System.Collections.Generic;
using Xunit;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Generic_SuccessResponse_ReturnsSuccessTrue_WithDataAndMessage()
    {
        // Arrange
        var data = "sample data";
        var message = "operation completed";

        // Act
        var response = ApiResponse<string>.SuccessResponse(data, message);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(data, response.Data);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Error);
        Assert.Null(response.Code);
        Assert.Null(response.TraceId);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Fact]
    public void Generic_SuccessResponse_AllowsNullData()
    {
        // Act
        var response = ApiResponse<object>.SuccessResponse(null);

        // Assert
        Assert.True(response.Success);
        Assert.Null(response.Data);
        Assert.Null(response.Message);
        Assert.Null(response.Error);
    }

    [Fact]
    public void Generic_ErrorResponse_ReturnsSuccessFalse_WithErrorMessageAndCode()
    {
        // Arrange
        var error = "invalid request";
        var message = "validation failed";
        var code = 400;

        // Act
        var response = ApiResponse<int>.ErrorResponse(error, message, code);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(error, response.Error);
        Assert.Equal(message, response.Message);
        Assert.Equal(code, response.Code);
        Assert.Null(response.Data);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Fact]
    public void NonGeneric_SuccessResponse_ReturnsSuccessTrue_WithMessage()
    {
        // Arrange
        var message = "all good";

        // Act
        var response = ApiResponse.SuccessResponse(message);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(message, response.Message);
        Assert.Null(response.Error);
        Assert.Null(response.Code);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Fact]
    public void NonGeneric_ErrorResponse_ReturnsSuccessFalse_WithErrorAndCode()
    {
        // Arrange
        var error = "server error";
        var message = "unexpected failure";
        var code = 500;

        // Act
        var response = ApiResponse.ErrorResponse(error, message, code);

        // Assert
        Assert.False(response.Success);
        Assert.Equal(error, response.Error);
        Assert.Equal(message, response.Message);
        Assert.Equal(code, response.Code);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }

    [Fact]
    public void PaginatedResponse_Create_ReturnsCorrectPaginationMetadata()
    {
        // Arrange
        var items = new List<string> { "a", "b", "c" };
        int pageNumber = 2;
        int pageSize = 3;
        int totalCount = 7;

        // Act
        var response = PaginatedResponse<string>.Create(items, pageNumber, pageSize, totalCount);

        // Assert
        Assert.True(response.Success);
        Assert.Equal(items, response.Items);
        Assert.Equal(pageNumber, response.PageNumber);
        Assert.Equal(pageSize, response.PageSize);
        Assert.Equal(totalCount, response.TotalCount);
        Assert.Equal(3, response.TotalPages); // (7 + 3 - 1) / 3 = 3
        Assert.True(response.HasNextPage);
        Assert.True(response.HasPreviousPage);
        Assert.InRange(response.Timestamp, DateTime.UtcNow.AddSeconds(-1), DateTime.UtcNow);
    }
}
