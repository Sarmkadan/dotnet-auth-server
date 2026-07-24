#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when input validation fails.
/// </summary>
public sealed class ValidationException : AuthServerException
{
    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with a generic message.
    /// </summary>
    /// <param name="message">
    /// Optional custom error message. Defaults to a generic validation‑failed message.
    /// </param>
    /// <param name="errorDescription">
    /// Optional detailed error description. If <c>null</c>, <paramref name="message"/> is used.
    /// </param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> is an empty string.
    /// </exception>
    public ValidationException(
        string message = "Validation failed",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_request",
            message,
            400,
            errorDescription ?? message,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }

    /// <summary>
    /// Initializes a new instance of <see cref="ValidationException"/> with field‑specific details.
    /// </summary>
    /// <param name="fieldName">The name of the field that failed validation.</param>
    /// <param name="fieldValue">The value of the field that failed validation.</param>
    /// <param name="validationRule">A description of the validation rule that was violated.</param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any of the string arguments are <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any of the string arguments are empty.
    /// </exception>
    public ValidationException(
        string fieldName,
        string fieldValue,
        string validationRule,
        Exception? innerException = null)
        : base(
            "invalid_request",
            $"Validation failed for {fieldName}: '{fieldValue}'. {validationRule}",
            400,
            null,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentException.ThrowIfNullOrEmpty(fieldValue);
        ArgumentException.ThrowIfNullOrEmpty(validationRule);
    }

    /// <summary>
    /// A collection of field‑specific validation errors.
    /// </summary>
    public Dictionary<string, object> Errors { get; } = new();

    /// <summary>
    /// Adds a validation error for a specific field.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="errorMessage">The error message.</param>
    public void AddError(string fieldName, string errorMessage)
    {
        ArgumentException.ThrowIfNullOrEmpty(fieldName);
        ArgumentException.ThrowIfNullOrEmpty(errorMessage);
        Errors[fieldName] = errorMessage;
    }
}
