public class RateLimitingOptions
{
    public int RequestsPerMinute { get; set; } = 60;
    public int BurstSize { get; set; } = 10;
    public HashSet<string> SensitiveEndpoints { get; set; } = new(StringComparer.OrdinalIgnoreCase) { "/oauth/token", "/oauth/authorize", "/oauth/introspect", "/oauth/revoke" };
}
