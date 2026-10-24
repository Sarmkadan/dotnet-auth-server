#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Standard API response wrapper for consistent response format across all endpoints.
/// Supports both success and error responses with metadata.
/// </summary>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; set; } = true;
    /// <summary>
    /// Gets or sets the response payload, if any.
    /// </summary>
    public T? Data { get; set; }
    /// <summary>
    /// Gets or sets the error, if the request failed.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets a human-readable message.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// Gets or sets an optional numeric code associated with the response.
    /// </summary>
    public int? Code { get; set; }
    /// <summary>
    /// Gets or sets the trace identifier for correlating the response with logs.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// Gets or sets the UTC time at which the response was created.
    /// </summary>
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
        ArgumentException.ThrowIfNullOrEmpty(error);
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
public sealed class ApiResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; set; } = true;
    /// <summary>
    /// Gets or sets a human-readable message.
    /// </summary>
    public string? Message { get; set; }
    /// <summary>
    /// Gets or sets the error, if the request failed.
    /// </summary>
    public string? Error { get; set; }
    /// <summary>
    /// Gets or sets an optional numeric code associated with the response.
    /// </summary>
    public int? Code { get; set; }
    /// <summary>
    /// Gets or sets the trace identifier for correlating the response with logs.
    /// </summary>
    public string? TraceId { get; set; }
    /// <summary>
    /// Gets or sets the UTC time at which the response was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response without data, with an optional message.
    /// </summary>
    public static ApiResponse SuccessResponse(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates an error response with the given error, optional message, and optional code.
    /// </summary>
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

    /// <summary>
    /// Returns a string representation of the response for diagnostics.
    /// </summary>
    public override string ToString() => $"ApiResponse {{ Success = {Success}, Error = {Error}, Message = {Message}, Code = {Code}, TraceId = {TraceId} }}";
}

/// <summary>
/// Paginated response wrapper for list endpoints.
/// </summary>
public sealed class PaginatedResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; set; } = true;
    /// <summary>
    /// Gets or sets the items on the current page.
    /// </summary>
    public List<T> Items { get; set; } = new();
    /// <summary>
    /// Gets or sets the one-based number of the current page.
    /// </summary>
    public int PageNumber { get; set; } = 1;
    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 20;
    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }
    /// <summary>
    /// Gets the total number of pages, computed from the total count and page size.
    /// </summary>
    public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
    /// <summary>
    /// Gets a value indicating whether a page follows the current one.
    /// </summary>
    public bool HasNextPage => PageNumber < TotalPages;
    /// <summary>
    /// Gets a value indicating whether a page precedes the current one.
    /// </summary>
    public bool HasPreviousPage => PageNumber > 1;
    /// <summary>
    /// Gets or sets the UTC time at which the response was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a paginated response for the given items and paging values.
    /// </summary>
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

    /// <summary>
    /// Returns a string representation of the paginated response for diagnostics.
    /// </summary>
    public override string ToString() => $"PaginatedResponse<T> {{ Success = {Success}, Items = {Items?.Count ?? 0}, PageNumber = {PageNumber}, PageSize = {PageSize}, TotalCount = {TotalCount} }}";
}
