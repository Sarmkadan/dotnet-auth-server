# KeyRotationOptions

The `KeyRotationOptions` class holds the configuration for how often signing keys should be rotated. It expresses the rotation interval as four additive components (days, hours, minutes, and seconds), so an operator can write a single interval such as "every 1 day, 12 hours" by setting two properties instead of computing a total number of seconds.

The class lives in `src/Configuration/KeyRotationOptions.cs` under the `DotnetAuthServer` namespace. It is a plain settings POCO with no behavior of its own.

> **Status:** `KeyRotationOptions` is not currently bound from configuration, registered in DI, or read by any service in this repository. The server signs tokens with a single static key, `AuthServerOptions.JwtSigningKey`, and has no automatic rotation. This class defines the shape of a future rotation setting. Nothing in the current code consumes it.

## API

### KeyRotationInDays
Gets or sets the day component of the key rotation interval. Defaults to `0`.

### KeyRotationInHours
Gets or sets the hour component of the key rotation interval. Defaults to `0`.

### KeyRotationInMinutes
Gets or sets the minute component of the key rotation interval. Defaults to `0`.

### KeyRotationInSeconds
Gets or sets the second component of the key rotation interval. Defaults to `0`.

## Usage

The interval is the sum of the four components. A consumer would combine them into a `TimeSpan`:

```csharp
var rotation = new KeyRotationOptions
{
    KeyRotationInDays = 1,
    KeyRotationInHours = 12,
    KeyRotationInMinutes = 0,
    KeyRotationInSeconds = 0
};

// 1 day + 12 hours = 36 hours
var interval = new TimeSpan(
    rotation.KeyRotationInDays,
    rotation.KeyRotationInHours,
    rotation.KeyRotationInMinutes,
    rotation.KeyRotationInSeconds);
```

## Notes

*   **No validation**: The properties accept any `int`, including negative values. The class performs no range checks and no data annotations. A consumer that builds a `TimeSpan` from these values should reject a zero or negative total interval.
*   **Component overflow**: Each component is independent. Setting `KeyRotationInHours = 36` is equivalent to `KeyRotationInDays = 1, KeyRotationInHours = 12`. Both produce the same `TimeSpan`.
*   **Zero interval**: When all four components are `0`, the interval is zero. Whether that means "rotation disabled" or "invalid" is not defined by this class, because no consumer exists yet.
*   **Relation to signing keys**: Rotation applies to signing keys, not to the `JwtSigningKey` string used for HS256 tokens by default. Adding rotation would require a key store that keeps previous keys available for validating tokens issued before the rotation.
