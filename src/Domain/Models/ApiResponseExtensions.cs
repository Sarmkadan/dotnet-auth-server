namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Provides useful extension methods for <see cref="ApiResponse"/> and <see cref="ApiResponse{T}"/> types.
/// </summary>
public static class ApiResponseExtensions
{
    /// <summary>
    /// Creates a new successful ApiResponse{T} with the specified data.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <param name="data">The data to include in the response.</param>
    /// <returns>A new ApiResponse{T} with the specified data.</returns>
    public static ApiResponse<T> WithData<T>(this ApiResponse response, T data)
    {
        return ApiResponse<T>.SuccessResponse(data);
    }

    /// <summary>
    /// Creates a new ApiResponse with the specified error message.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <param name="errorMessage">The error message to include in the response.</param>
    /// <returns>A new ApiResponse with the specified error message.</returns>
    public static ApiResponse WithError(this ApiResponse response, string errorMessage)
    {
        return ApiResponse.ErrorResponse(errorMessage);
    }

    /// <summary>
    /// Creates a new ApiResponse with the specified error message and status code.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <param name="errorMessage">The error message to include in the response.</param>
    /// <param name="statusCode">The HTTP status code to include in the response.</param>
    /// <returns>A new ApiResponse with the specified error message and status code.</returns>
    public static ApiResponse WithError(this ApiResponse response, string errorMessage, int statusCode)
    {
        var result = ApiResponse.ErrorResponse(errorMessage);
        result.Code = statusCode;
        return result;
    }

    /// <summary>
    /// Adds a message to the ApiResponse. If the response already has a message,
    /// the new message is appended with a separator.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <param name="message">The message to add to the response.</param>
    /// <returns>The same ApiResponse instance for method chaining.</returns>
    public static ApiResponse WithMessage(this ApiResponse response, string message)
    {
        if (string.IsNullOrEmpty(response.Message))
        {
            response.Message = message;
        }
        else if (!response.Message.Contains(message, StringComparison.Ordinal))
        {
            response.Message += " | " + message;
        }

        return response;
    }

    /// <summary>
    /// Determines whether the ApiResponse contains data (non-null).
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="response">The source ApiResponse{T} instance.</param>
    /// <returns>True if the response contains data; otherwise, false.</returns>
    public static bool HasData<T>(this ApiResponse<T> response)
    {
        return response.Data != null;
    }

    /// <summary>
    /// Determines whether the ApiResponse represents a successful operation.
    /// This is an alias for the Success property for more fluent API style.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <returns>True if Success is true; otherwise, false.</returns>
    public static bool IsSuccess(this ApiResponse response)
    {
        return response.Success;
    }

    /// <summary>
    /// Creates a new ApiResponse by copying all properties from the source and updating
    /// the data with the specified value.
    /// </summary>
    /// <typeparam name="T">The type of data in the response.</typeparam>
    /// <param name="response">The source ApiResponse{T} instance.</param>
    /// <param name="newData">The new data value to set.</param>
    /// <returns>A new ApiResponse{T} with updated data.</returns>
    public static ApiResponse<T> UpdateData<T>(this ApiResponse<T> response, T newData)
    {
        return new ApiResponse<T>
        {
            Success = response.Success,
            Data = newData,
            Error = response.Error,
            Message = response.Message,
            Code = response.Code,
            TraceId = response.TraceId,
            Timestamp = response.Timestamp
        };
    }

    /// <summary>
    /// Sets the Code property to the specified HTTP status code.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <param name="statusCode">The HTTP status code to set.</param>
    /// <returns>The same ApiResponse instance for method chaining.</returns>
    public static ApiResponse WithStatusCode(this ApiResponse response, int statusCode)
    {
        response.Code = statusCode;
        return response;
    }

    /// <summary>
    /// Sets the TraceId property to a new GUID value.
    /// </summary>
    /// <param name="response">The source ApiResponse instance.</param>
    /// <returns>The same ApiResponse instance for method chaining.</returns>
    public static ApiResponse WithTraceId(this ApiResponse response)
    {
        response.TraceId = Guid.NewGuid().ToString();
        return response;
    }
}