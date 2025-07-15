#nullable enable

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Extension methods for <see cref="ValidationException"/> to provide additional functionality
/// </summary>
public static class ValidationExceptionExtensions
{
    /// <summary>
    /// Adds multiple errors to the ValidationException at once
    /// </summary>
    /// <param name="exception">The ValidationException instance</param>
    /// <param name="errors">Dictionary of field names and error messages</param>
    /// <returns>The ValidationException instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> or <paramref name="errors"/> is <see langword="null"/></exception>
    public static ValidationException AddErrors(this ValidationException exception, Dictionary<string, string> errors)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            ArgumentNullException.ThrowIfNull(error.Value);
            exception.AddError(error.Key, error.Value);
        }

        return exception;
    }

    /// <summary>
    /// Adds an error with additional context data
    /// </summary>
    /// <param name="exception">The ValidationException instance</param>
    /// <param name="fieldName">Name of the field that failed validation</param>
    /// <param name="errorMessage">Error message describing the validation failure</param>
    /// <param name="contextData">Additional context data to include with the error</param>
    /// <returns>The ValidationException instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when any parameter is <see langword="null"/></exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is <see cref="string.IsNullOrEmpty"/> or whitespace</exception>
    public static ValidationException AddErrorWithContext(this ValidationException exception, string fieldName, string errorMessage, Dictionary<string, object> contextData)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentNullException.ThrowIfNull(errorMessage);
        ArgumentNullException.ThrowIfNull(contextData);

        exception.AddError(fieldName, errorMessage);
        exception.Errors[fieldName] = contextData;

        return exception;
    }

    /// <summary>
    /// Merges another ValidationException's errors into this one
    /// </summary>
    /// <param name="target">The target ValidationException to merge into</param>
    /// <param name="source">The source ValidationException to merge from</param>
    /// <returns>The target ValidationException instance for method chaining</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="target"/> or <paramref name="source"/> is <see langword="null"/></exception>
    public static ValidationException MergeErrors(this ValidationException target, ValidationException source)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var error in source.Errors)
        {
            target.Errors[error.Key] = error.Value;
        }

        return target;
    }

    /// <summary>
    /// Checks if the ValidationException contains an error for the specified field
    /// </summary>
    /// <param name="exception">The ValidationException instance</param>
    /// <param name="fieldName">Name of the field to check</param>
    /// <returns>True if the field has an error, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> or <paramref name="fieldName"/> is <see langword="null"/></exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="fieldName"/> is <see cref="string.IsNullOrEmpty"/> or whitespace</exception>
    public static bool HasError(this ValidationException exception, string fieldName)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrEmpty(fieldName);

        return exception.Errors.ContainsKey(fieldName);
    }
}