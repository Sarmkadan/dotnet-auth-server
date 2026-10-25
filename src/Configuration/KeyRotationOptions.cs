namespace DotnetAuthServer;

/// <summary>
/// Key rotation interval settings. Describes how often signing keys should be rotated, expressed as four additive
/// components (days, hours, minutes, seconds) so the interval can be written without computing a total in seconds.
/// Plain settings object with no behavior of its own; it is not yet bound from configuration or consumed by any service.
/// </summary>
public class KeyRotationOptions
{
    /// <summary>
    /// Days component of the signing key rotation interval. Added to the other components to form the total interval.
    /// </summary>
    public int KeyRotationInDays { get; set; }
    /// <summary>
    /// Hours component of the signing key rotation interval. Added to the other components to form the total interval.
    /// </summary>
    public int KeyRotationInHours { get; set; }
    /// <summary>
    /// Minutes component of the signing key rotation interval. Added to the other components to form the total interval.
    /// </summary>
    public int KeyRotationInMinutes { get; set; }
    /// <summary>
    /// Seconds component of the signing key rotation interval. Added to the other components to form the total interval.
    /// </summary>
    public int KeyRotationInSeconds { get; set; }

    /// <inheritdoc />
    public override string ToString() =>
        $"KeyRotationOptions {{ KeyRotationInDays = {KeyRotationInDays}, KeyRotationInHours = {KeyRotationInHours}, KeyRotationInMinutes = {KeyRotationInMinutes}, KeyRotationInSeconds = {KeyRotationInSeconds} }}";
}
