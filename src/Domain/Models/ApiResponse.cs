// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Standard API response wrapper for consistent response format across all endpoints.
/// Supports both success and error responses with metadata.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; } = true;
    public T? Data { get; set; }
    public string? Error { get; set; }
    public string? Message { get; set; }
    public int? Code { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response with data.
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response.
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string error, string? message = null, int? code = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Error = error,
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Non-generic API response for endpoints that don't return data.
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; } = true;
    public string? Message { get; set; }
    public string? Error { get; set; }
    public int? Code { get; set; }
    public string? TraceId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    public static ApiResponse ErrorResponse(string error, string? message = null, int? code = null)
    {
        return new ApiResponse
        {
            Success = false,
            Error = error,
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// </summary>
public class PaginatedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public int TotalCount { get; set; }
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public static PaginatedResponse<T> Create(
        List<T> items,
        int pageNumber,
        int pageSize,
        int totalCount)
    {
        return new PaginatedResponse<T>
        {
            Success = true,
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            Timestamp = DateTime.UtcNow
        };
    }
}
