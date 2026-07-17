# RateLimitingOptions

Configuration options for the rate limiting middleware that protects OAuth2 endpoints from abuse using a token bucket algorithm.

## API

### RequestsPerMinute

Gets or sets the number of requests allowed per minute per client. This value determines the refill rate of the token bucket.

- **Type:** `int`
- **Default:** `60`
- **Constraints:** Must be a positive integer
- **Usage:** Controls the steady-state request rate that clients are permitted to sustain

### BurstSize

Gets or sets the maximum number of tokens that can be accumulated in the bucket. This represents the maximum burst capacity before rate limiting begins.

- **Type:** `int`
- **Default:** `10`
- **Constraints:** Must be a positive integer
- **Usage:** Allows short bursts of traffic while preventing sustained abuse

### SensitiveEndpoints

Gets or sets the collection of endpoint paths that are subject to rate limiting. Endpoints matching any entry in this set will have their requests tracked and limited.

- **Type:** `HashSet<string>`
- **Default:** A case-insensitive set containing:
  - `/oauth/token`
  - `/oauth/authorize`
  - `/oauth/introspect`
  - `/oauth/revoke`
- **Constraints:** Must not be null; initialized to a new instance if null
- **Usage:** Specifies which OAuth2 endpoints require protection from brute force attacks


## Usage

### Basic Configuration in Program.cs

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRateLimiting(options =>
{
    options.RequestsPerMinute = 120;    // Allow 2 requests per second
    options.BurstSize = 30;              // Allow bursts up to 30 requests
    options.SensitiveEndpoints = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/oauth/token",
        "/oauth/authorize",
        "/oauth/introspect"
    };
});

var app = builder.Build();
app.UseMiddleware<RateLimitingMiddleware>();
```

### Loading from Configuration


```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddRateLimiting();

var app = builder.Build();
app.UseMiddleware<RateLimitingMiddleware>();
```

With corresponding `appsettings.json`:

```json
{
  "RateLimiting": {
    "RequestsPerMinute": 120,
    "BurstSize": 30,
    "SensitiveEndpoints": [
      "/oauth/token",
      "/oauth/authorize",
      "/oauth/introspect"
    ]
  }
}
```

## Notes

- **Thread Safety:** The properties themselves are not thread-safe for concurrent modification. In production scenarios, configure rate limiting options during application startup before any requests are processed, and avoid modifying them at runtime.
- **Validation:** No runtime validation is performed when setting these properties. Invalid values (zero or negative numbers) will result in unexpected behavior from the rate limiting middleware.
- **Case Sensitivity:** The `SensitiveEndpoints` collection uses case-insensitive comparison by default, allowing flexible endpoint path matching regardless of casing.
- **Default Values:** When using dependency injection, if no explicit configuration is provided, the middleware will use the default values defined in the `RateLimitingOptions` class.
- **Serialization:** The class supports JSON serialization/deserialization via `RateLimitingMiddlewareJsonExtensions` for persistence and inter-process communication scenarios.