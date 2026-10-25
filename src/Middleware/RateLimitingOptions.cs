/// <summary>
/// Configuration options for <see cref="DotnetAuthServer.Middleware.RateLimitingMiddleware"/>.
/// </summary>
public class RateLimitingOptions
{
    private const int DefaultRequestsPerMinute = 60;
    private const int DefaultBurstSize = 10;

    /// <summary>
    /// Sustained number of requests allowed per minute for each client on a sensitive endpoint. Defaults to 60.
    /// </summary>
    public int RequestsPerMinute { get; set; } = DefaultRequestsPerMinute;

    /// <summary>
    /// Maximum number of requests a client may make in a short burst before being throttled to <see cref="RequestsPerMinute"/>. Defaults to 10.
    /// </summary>
    public int BurstSize { get; set; } = DefaultBurstSize;

    /// <summary>
    /// Request path prefixes that are rate limited (case-insensitive match). Requests to other paths are not limited.
    /// Defaults to the token, authorize, introspect and revoke endpoints.
    /// </summary>
    public HashSet<string> SensitiveEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "/oauth/token", "/oauth/authorize", "/oauth/introspect", "/oauth/revoke" };
}
