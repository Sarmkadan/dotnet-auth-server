// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the logging system.
/// Controls verbosity, formatting, and what information is logged.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Minimum log level to output.
    /// </summary>
    public LogLevel MinimumLevel { get; set; } = LogLevel.Information;

    /// <summary>
    /// Whether to log sensitive information (tokens, secrets).
    /// Should be false in production.
    /// </summary>
    public bool LogSensitiveData { get; set; } = false;

    /// <summary>
    /// Whether to include request/response bodies in logs.
    /// Can be expensive for large payloads.
    /// </summary>
    public bool LogRequestBodies { get; set; } = false;

    /// <summary>
    /// Whether to include timing information for request processing.
    /// Useful for performance analysis.
    /// </summary>
    public bool LogRequestTiming { get; set; } = true;

    /// <summary>
    /// Maximum length of request/response bodies to log.
    /// Longer bodies are truncated.
    /// </summary>
    public int MaxBodyLogLength { get; set; } = 1000;

    /// <summary>
    /// Paths that should not be logged (to reduce noise).
    /// </summary>
    public List<string> ExcludedPaths { get; set; } = new()
    {
        "/health",
        "/swagger",
        "/.well-known"
    };

    /// <summary>
    /// Whether to include correlation IDs in logs.
    /// </summary>
    public bool IncludeCorrelationId { get; set; } = true;

    /// <summary>
    /// Whether to use structured logging format (JSON).
    /// </summary>
    public bool StructuredLogging { get; set; } = false;
}
