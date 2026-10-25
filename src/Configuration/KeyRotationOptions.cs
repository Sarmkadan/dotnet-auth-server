namespace DotnetAuthServer;

/// <summary>
/// Key rotation interval settings.
/// </summary>
public class KeyRotationOptions
{
    /// <summary>
    /// Key rotation interval component, in days.
    /// </summary>
    public int KeyRotationInDays { get; set; }
    /// <summary>
    /// Key rotation interval component, in hours.
    /// </summary>
    public int KeyRotationInHours { get; set; }
    /// <summary>
    /// Key rotation interval component, in minutes.
    /// </summary>
    public int KeyRotationInMinutes { get; set; }
    /// <summary>
    /// Key rotation interval component, in seconds.
    /// </summary>
    public int KeyRotationInSeconds { get; set; }
}
