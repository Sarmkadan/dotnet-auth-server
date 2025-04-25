// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

/// <summary>
/// Extension methods for HttpRequest to extract and validate OAuth2/OIDC parameters.
/// Handles both query string and form body parameters which are both valid in OAuth2.
/// </summary>
public static class HttpRequestExtensions
{
    /// <summary>
    /// Safely retrieves a parameter from either query string or form body.
    /// OAuth2 allows parameters in either location for specific endpoints.
    /// </summary>
    public static string? GetOAuthParameter(this HttpRequest request, string parameterName)
    {
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
    public static (string? ClientId, string? ClientSecret) ExtractClientCredentials(this HttpRequest request)
    {
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
    private static (string ClientId, string ClientSecret) ExtractBasicAuthCredentials(string authHeader)
    {
        var encodedCredentials = authHeader.Substring("Basic ".Length);
        var decodedBytes = Convert.FromBase64String(encodedCredentials);
        var decodedString = System.Text.Encoding.UTF8.GetString(decodedBytes);
        var parts = decodedString.Split(':', 2);

        return (parts[0], parts.Length > 1 ? parts[1] : string.Empty);
    }

    /// <summary>
    /// Retrieves the requesting client's IP address, accounting for proxies.
    /// Checks X-Forwarded-For header which is commonly set by load balancers/proxies.
    /// </summary>
    public static string? GetClientIpAddress(this HttpRequest request)
    {
        if (request.Headers.TryGetValue("X-Forwarded-For", out var xForwardedFor))
        {
            var ips = xForwardedFor.ToString().Split(',', StringSplitOptions.TrimEntries);
            return ips.FirstOrDefault();
        }

        return request.HttpContext.Connection.RemoteIpAddress?.ToString();
    }

    /// <summary>
    /// Checks if request is using HTTPS/TLS.
    /// Important for OAuth2 security - most endpoints require secure transport.
    /// </summary>
    public static bool IsSecureTransport(this HttpRequest request)
    {
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
    public static string? GetBearerToken(this HttpRequest request)
    {
        if (!request.Headers.ContainsKey("Authorization"))
            return null;

        var authHeader = request.Headers["Authorization"].ToString();
        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader.Substring("Bearer ".Length);
    }
}
