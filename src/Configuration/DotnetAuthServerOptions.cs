using System.ComponentModel.DataAnnotations;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Root configuration options for the authorization server.
/// </summary>
public sealed class DotnetAuthServerOptions
{
    public const string SectionName = "DotnetAuthServer";

    [Required]
    public AuthServerOptions AuthServer { get; set; } = new();

    [Required]
    public CacheOptions Cache { get; set; } = new();

    [Required]
    public LoggingOptions Logging { get; set; } = new();

    [Required]
    public OpaOptions Opa { get; set; } = new();
}
