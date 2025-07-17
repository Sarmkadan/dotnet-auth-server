#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DotnetAuthServer.Extensions;

/// <summary>
/// Extension methods for <see cref="HttpRequest"/> to extract and validate OAuth2/OIDC parameters.
/// Handles both query string and form body parameters which are both valid in OAuth2.
/// </summary>
/// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/></exception>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Safely retrieves a parameter from either query string or form body.
    /// OAuth2 allows parameters in either location for specific endpoints.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <param name="parameterName">The parameter name to retrieve.</param>
    /// <returns>The parameter value if found; otherwise <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="parameterName"/> is <see langword="null"/>.</exception>
    public static string? GetOAuthParameter(this HttpRequest request, string parameterName)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(parameterName);

        // Check query string first
        if (request.Query.TryGetValue(parameterName, out var queryValue))
            return queryValue.ToString();

        // Check form body (requires synchronous read, typically not a problem in middleware)
        if (request.HasFormContentType && request.Form.TryGetValue(parameterName, out var formValue))
            return formValue.ToString();

        return null;
    }

    /// <summary>
    /// Extracts client credentials from either HTTP Basic Authorization header or form parameters.
    /// Per OAuth2 spec, confidential clients can authenticate via either method.
    /// Returns tuple of (client_id, client_secret).
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>Tuple containing client_id and client_secret; both may be null if not found.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public static (string? ClientId, string? ClientSecret) ExtractClientCredentials(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var clientId = request.GetOAuthParameter("client_id");
        var clientSecret = request.GetOAuthParameter("client_secret");

        // Check HTTP Basic Auth header
        if (request.Headers.ContainsKey("Authorization"))
        {
            var authHeader = request.Headers["Authorization"].ToString();
            if (authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var credentials = ExtractBasicAuthCredentials(authHeader);
                    clientId = credentials.ClientId;
                    clientSecret = credentials.ClientSecret;
                }
                catch
                {
                    // Malformed Basic Auth header, fall through to form parameters
                }
            }
        }

        return (clientId, clientSecret);
    }

    /// <summary>
    /// Decodes HTTP Basic Authentication header.
    /// Format: Authorization: Basic base64(clientid:clientsecret)
    /// </summary>
    /// <param name="authHeader">The Authorization header value starting with "Basic ".</param>
    /// <returns>Tuple containing client_id and client_secret.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="authHeader"/> is <see langword="null"/>.</exception>
    /// <exception cref="FormatException">The header is malformed or not properly Base64 encoded.</exception>
    /// <exception cref="ArgumentException">The decoded credentials do not contain a colon separator.</exception>
    private static (string ClientId, string ClientSecret) ExtractBasicAuthCredentials(string authHeader)
    {
        ArgumentNullException.ThrowIfNull(authHeader);

        if (!authHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            throw new FormatException("Authorization header must start with 'Basic '");

        var encodedCredentials = authHeader.Substring("Basic ".Length);
        var decodedBytes = Convert.FromBase64String(encodedCredentials);
        var decodedString = Encoding.UTF8.GetString(decodedBytes);
        var parts = decodedString.Split(':', 2);

        if (parts.Length == 0)
            throw new FormatException("No credentials found in Basic Authentication header");

        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

    /// <summary>
    /// Retrieves the requesting client's IP address, accounting for proxies.
    /// Checks X-Forwarded-For header which is commonly set by load balancers/proxies.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The client IP address as a string, or null if not available.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public static string? GetClientIpAddress(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
        {
            var ips = xForwardedFor.ToString().Split(',', StringSplitOptions.TrimEntries);
            return ips.FirstOrDefault(static ip => !string.IsNullOrEmpty(ip));
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Checks if request is using HTTPS/TLS.
    /// Important for OAuth2 security - most endpoints require secure transport.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>True if the request uses HTTPS/TLS; otherwise false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public static bool IsSecureTransport(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.IsHttps)
            return true;

        // Check for X-Forwarded-Proto header (set by proxies)
        if (request.Headers.TryGetValue("X-Forwarded-Proto", out var protocol))
            return protocol.ToString().Equals("https", StringComparison.OrdinalIgnoreCase);

        return false;
    }

    /// <summary>
    /// Retrieves a bearer token from Authorization header.
    /// Per RFC 6750.
    /// </summary>
    /// <param name="request">The HTTP request.</param>
    /// <returns>The bearer token if found; otherwise null.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="request"/> is <see langword="null"/>.</exception>
    public static string? GetBearerToken(this HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!request.Headers.ContainsKey("Authorization"))
            return null;

        var authHeader = request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader.Substring("Bearer ".Length);
    }
}