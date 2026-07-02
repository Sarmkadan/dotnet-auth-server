#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when input validation fails
/// </summary>
public sealed class ValidationException : AuthServerException
{
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
    }

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
    }

    public Dictionary<string, object> Errors { get; } = new();

    public void AddError(string fieldName, string errorMessage)
    {
        Errors[fieldName] = errorMessage;
    }
}