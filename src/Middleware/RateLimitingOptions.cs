/// <summary>
/// Configuration options for <see cref="DotnetAuthServer.Middleware.RateLimitingMiddleware"/>.
/// </summary>
public class RateLimitingOptions
{
    /// <summary>
    /// Sustained number of requests allowed per minute. Defaults to 60.
    /// </summary>
    public int RequestsPerMinute { get; set; } = 60;

    /// <summary>
    /// Maximum number of requests allowed in a burst. Defaults to 10.
    /// </summary>
    public int BurstSize { get; set; } = 10;

    /// <summary>
    /// Paths that are rate limited. Defaults to the token, authorize, introspect and revoke endpoints.
    /// </summary>
    public HashSet<string> SensitiveEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "/oauth/token", "/oauth/authorize", "/oauth/introspect", "/oauth/revoke" };
}
