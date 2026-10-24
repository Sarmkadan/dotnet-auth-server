using System.ComponentModel.DataAnnotations;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Root configuration options for the authorization server.
/// </summary>
public sealed class DotnetAuthServerOptions
{
    /// <summary>
    /// Configuration section name this options class binds to.
    /// </summary>
    public const string SectionName = "DotnetAuthServer";

    /// <summary>
    /// Options for the authorization server.
    /// </summary>
    [Required]
    public AuthServerOptions AuthServer { get; set; } = new();

    /// <summary>
    /// Options for the caching layer.
    /// </summary>
    [Required]
    public CacheOptions Cache { get; set; } = new();

    /// <summary>
    /// Logging options.
    /// </summary>
    [Required]
    public LoggingOptions Logging { get; set; } = new();

    /// <summary>
    /// Open Policy Agent (OPA) integration options.
    /// </summary>
    [Required]
    public OpaOptions Opa { get; set; } = new();

    /// <summary>
    /// Returns a string representation of these options.
    /// </summary>
    public override string ToString() =>
        $"DotnetAuthServerOptions {{ AuthServer = {AuthServer}, Cache = {Cache}, Logging = {Logging}, Opa = {Opa} }}";
}
