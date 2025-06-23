#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetAuthServer.Extensions;

/// <summary>
/// Validation extension methods for HttpRequest to validate OAuth2/OIDC parameters.
/// Provides comprehensive validation for extracted request parameters.
/// </summary>
public static class HttpRequestExtensionsValidation
{
    /// <summary>
    /// Validates the extracted OAuth parameters and returns a list of human-readable problems.
    /// Checks for null/empty strings, whitespace-only strings, and invalid formats.
    /// </summary>
    /// <param name="request">The HttpRequest to extract and validate parameters from.</param>
    /// <returns>An enumerable of validation problems, or empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    public static IReadOnlyList<string> Validate(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var problems = new List<string>();

        // Validate GetOAuthParameter results (client_id, client_secret, etc.)
        // These are strings that should not be empty when present

        // Validate ExtractClientCredentials results
        var (clientId, clientSecret) = request.ExtractClientCredentials();

        if (!string.IsNullOrEmpty(clientId) && string.IsNullOrWhiteSpace(clientId))
        {
            problems.Add("Client ID contains only whitespace characters");
        }

        if (!string.IsNullOrEmpty(clientSecret) && string.IsNullOrWhiteSpace(clientSecret))
        {
            problems.Add("Client secret contains only whitespace characters");
        }

        // Validate GetClientIpAddress result
        var ipAddress = request.GetClientIpAddress();
        if (!string.IsNullOrEmpty(ipAddress))
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
            {
                problems.Add("IP address contains only whitespace characters");
            }
            else if (ipAddress == "::1" || ipAddress == "127.0.0.1")
            {
                problems.Add("IP address is localhost (::1 or 127.0.0.1)");
            }
        }

        // Validate IsSecureTransport result - no validation needed as it's a boolean

        // Validate GetBearerToken result
        var bearerToken = request.GetBearerToken();
        if (!string.IsNullOrEmpty(bearerToken))
        {
            if (string.IsNullOrWhiteSpace(bearerToken))
            {
                problems.Add("Bearer token contains only whitespace characters");
            }
            else if (bearerToken.Length < 10)
            {
                problems.Add("Bearer token is too short (less than 10 characters)");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the HttpRequest parameters are valid.
    /// </summary>
    /// <param name="request">The HttpRequest to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    public static bool IsValid(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return Validate(request).Count == 0;
    }

    /// <summary>
    /// Ensures that the HttpRequest parameters are valid, throwing an exception if not.
    /// </summary>
    /// <param name="request">The HttpRequest to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if request is null.</exception>
    /// <exception cref="ArgumentException">Thrown if validation fails, containing the list of problems.</exception>
    public static void EnsureValid(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var problems = Validate(request);

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"HttpRequest parameter validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}