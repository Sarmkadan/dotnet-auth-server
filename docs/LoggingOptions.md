# LoggingOptions

`LoggingOptions` is a configuration class that controls the behavior of the logging subsystem within the `dotnet-auth-server` project. It provides granular settings for minimum log severity, handling of sensitive data, request/response body capture, performance timing, correlation ID inclusion, and output format selection. Instances of this type are typically bound from application configuration and consumed by middleware or logging infrastructure to enforce consistent, secure, and performant logging policies across the authentication server.

## API

### MinimumLevel
`public LogLevel MinimumLevel`

Gets or sets the minimum log level threshold. Messages with a severity below this value are suppressed. The default is typically `LogLevel.Information` unless overridden by configuration. Setting this to `LogLevel.Trace` enables the most verbose output, while `LogLevel.None` disables all logging.

### LogSensitiveData
`public bool LogSensitiveData`

Controls whether potentially sensitive information—such as tokens, passwords, or personally identifiable data—appears in log output. When `false`, the logging pipeline must redact or omit such data. When `true`, sensitive values may be written in plain text. This should remain `false` in production environments unless explicit diagnostics are required.

### LogRequestBodies
`public bool LogRequestBodies`

Determines whether the body content of HTTP requests is captured and written to the log. Enabling this can significantly increase log volume and may expose sensitive payloads. Works in conjunction with `LogSensitiveData` and `MaxBodyLogLength` to control exactly what is recorded.

### LogRequestTiming
`public bool LogRequestTiming`

When `true`, the logging system records elapsed time for request processing and includes timing metrics in log entries. This is useful for performance monitoring and diagnosing latency issues. When `false`, timing data is omitted, reducing log verbosity and slight processing overhead.

### MaxBodyLogLength
`public int MaxBodyLogLength`

Specifies the maximum number of characters of a request body that will be captured when `LogRequestBodies` is enabled. Bodies exceeding this length are truncated before being written to the log. A value of `0` or a negative number effectively disables body capture regardless of `LogRequestBodies`. This property does not throw on assignment of any valid integer.

### ExcludedPaths
`public List<string> ExcludedPaths`

A list of URL path patterns that should be excluded from logging. Requests whose paths match any entry in this list bypass the logging pipeline entirely for request/response entries. Matching is typically performed as a prefix or exact match depending on the consuming middleware. An empty list means no paths are excluded. The list instance itself is never null after initialization.

### IncludeCorrelationId
`public bool IncludeCorrelationId`

When `true`, a correlation identifier is automatically attached to each log entry produced during a request’s lifetime. This enables tracing a single request across multiple log lines and services. When `false`, correlation IDs are omitted, which may hinder distributed debugging.

### StructuredLogging
`public bool StructuredLogging`

Controls the output format of log entries. When `true`, logs are emitted in a structured format (typically JSON) suitable for ingestion by log aggregators. When `false`, logs are written as plain text, which is more human-readable but harder to query programmatically.

## Usage

### Example 1: Binding from Configuration and Applying in Middleware

```csharp
// In Program.cs or Startup.cs
var builder = WebApplication.CreateBuilder(args);

// Bind LoggingOptions from appsettings.json section "Logging"
builder.Services.Configure<LoggingOptions>(
    builder.Configuration.GetSection("Logging"));

var app = builder.Build();

// Middleware that reads the options at runtime
app.Use(async (context, next) =>
{
    var options = context.RequestServices
        .GetRequiredService<IOptionsSnapshot<LoggingOptions>>().Value;

    if (options.ExcludedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
    {
        // Skip logging for excluded paths
        await next();
        return;
    }

    var logger = context.RequestServices
        .GetRequiredService<ILogger<Program>>();

    if (options.LogRequestTiming)
    {
        var sw = Stopwatch.StartNew();
        await next();
        sw.Stop();
        logger.LogInformation("Request {Path} completed in {ElapsedMs}ms",
            context.Request.Path, sw.ElapsedMilliseconds);
    }
    else
    {
        await next();
    }
});

app.Run();
```

### Example 2: Conditional Body Logging with Truncation

```csharp
public async Task LogRequestBodyIfEnabled(
    HttpContext context,
    LoggingOptions options,
    ILogger logger)
{
    if (!options.LogRequestBodies || options.MaxBodyLogLength <= 0)
        return;

    context.Request.EnableBuffering();
    using var reader = new StreamReader(
        context.Request.Body,
        Encoding.UTF8,
        leaveOpen: true);

    var body = await reader.ReadToEndAsync();
    context.Request.Body.Position = 0;

    if (!options.LogSensitiveData)
    {
        // Redact sensitive fields before logging
        body = RedactSensitiveFields(body);
    }

    if (body.Length > options.MaxBodyLogLength)
    {
        body = body[..options.MaxBodyLogLength] + "...[truncated]";
    }

    logger.LogInformation("Request body: {Body}", body);
}

private string RedactSensitiveFields(string body) => "[REDACTED]";
```

## Notes

- **Thread Safety**: `LoggingOptions` is a plain configuration object. Its properties are not synchronized. In a typical ASP.NET Core application, the options are bound once at startup and read concurrently by multiple threads. Modifying property values after binding while the application is serving requests can lead to inconsistent behavior unless external synchronization is applied. The `ExcludedPaths` list is not thread-safe for concurrent writes; populate it during initialization only.
- **Edge Cases**: When `LogRequestBodies` is `true` but `MaxBodyLogLength` is zero or negative, no body content is logged. When `LogSensitiveData` is `false`, the logging pipeline is expected to perform redaction; failure to implement redaction logic in consumers of this option may still result in sensitive data leakage. Path matching for `ExcludedPaths` depends on the consuming code—prefix matching versus exact matching must be documented and implemented consistently. Setting `MinimumLevel` to `LogLevel.None` disables all log output regardless of other settings.
- **Initialization**: The `ExcludedPaths` property is initialized to an empty `List<string>` by default constructors or binding infrastructure. It is never `null` when properly bound, but consumers should guard against `null` if the options instance is manually created without initializing the list.
