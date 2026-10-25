# DateTimeExtensions

**Namespace:** `DotnetAuthServer.Extensions`

Static extension methods on `System.DateTime` for Unix timestamps, token expiration checks, lifetime calculation, and RFC 3339 formatting. JWT and OAuth2 handling uses them so that timestamp conversion and expiry checks behave the same way everywhere in the server.

The class is `public static`. It has no state and no constructor.

## Methods

### `long ToUnixTimestamp(this DateTime dateTime)`

Converts a `DateTime` to Unix epoch time: whole seconds since 1970-01-01T00:00:00Z.

The conversion depends on `Kind`:

- `Utc`: the value is used as-is.
- `Local`: the value is converted to UTC first.
- `Unspecified`: the value is treated as local time and converted to UTC. This is the behaviour of `DateTime.ToUniversalTime()`, so the result depends on the machine's time zone. Use `Utc` values to get machine-independent results.

Fractional seconds are truncated, not rounded.

**Parameters:**
- `dateTime`: The value to convert.

**Returns:** The Unix timestamp in seconds. Values before the epoch return negative numbers.

**Exceptions:** None thrown by the method itself. A `Local` or `Unspecified` value at the extreme ends of the `DateTime` range can overflow during the conversion to UTC.

**Example:**

```csharp
long exp = DateTime.UtcNow.AddMinutes(5).ToUnixTimestamp();
```

### `DateTime FromUnixTimestamp(long timestamp)`

Creates a `DateTime` from Unix epoch time in seconds. This is a regular static method, not an extension method, so call it through the class name.

**Parameters:**
- `timestamp`: Seconds since 1970-01-01T00:00:00Z. Negative values are allowed.

**Returns:** A `DateTime` with `Kind` set to `Utc`.

**Exceptions:**
- `ArgumentOutOfRangeException`: `timestamp` is outside `-62135596800` (`0001-01-01T00:00:00Z`) through `253402300799` (`9999-12-31T23:59:59Z`).

**Example:**

```csharp
DateTime expiresAt = DateTimeExtensions.FromUnixTimestamp(1737779695);
```

### `bool IsExpired(this DateTime expiresAt)`

Returns `true` if `expiresAt` is at or before the current UTC time plus 5 seconds. The 5-second margin stops a token from being accepted just before it actually expires, which avoids races with clock skew and in-flight requests.

The method compares the raw `DateTime` value with `DateTime.UtcNow`. It does not convert `Kind` first, so pass UTC values.

**Parameters:**
- `expiresAt`: The expiration time.

**Returns:** `true` if the value has expired, including the 5-second margin; otherwise `false`.

**Exceptions:** None.

### `bool IsValid(this DateTime expiresAt)`

The negation of `IsExpired`. Returns `true` while `expiresAt` is more than 5 seconds in the future.

**Parameters:**
- `expiresAt`: The expiration time.

**Returns:** `true` if the value is still valid; otherwise `false`.

**Exceptions:** None.

### `long RemainingSeconds(this DateTime expiresAt)`

Returns the whole seconds from the current UTC time until `expiresAt`. The result is truncated. It has no 5-second margin, so it can return a positive value for a time that `IsExpired` already reports as expired.

**Parameters:**
- `expiresAt`: The expiration time. The comparison uses the raw value against `DateTime.UtcNow`.

**Returns:** The remaining seconds, or `0` if `expiresAt` is in the past.

**Exceptions:** None.

### `DateTime AddLifetime(this DateTime baseTime, int lifetimeSeconds)`

Adds a lifetime in seconds to `baseTime`. Negative lifetimes are treated as `0`, so the method never returns a time earlier than `baseTime`.

**Parameters:**
- `baseTime`: The starting time. `Kind` is preserved in the result.
- `lifetimeSeconds`: The lifetime to add, in seconds. Values below `0` are clamped to `0`.

**Returns:** A new `DateTime`. The original value is not changed.

**Exceptions:** None thrown directly. The result can overflow past `DateTime.MaxValue`, which makes `AddSeconds` throw `ArgumentOutOfRangeException`.

**Example:**

```csharp
DateTime expiresAt = DateTime.UtcNow.AddLifetime(3600);
```

### `string ToRfc3339String(this DateTime dateTime)`

Formats a `DateTime` in the round-trip (`"o"`) format after converting it to UTC. The result always ends in `Z`.

The output includes seven fractional-second digits, for example `2025-09-24T12:34:56.0000000Z`. Some consumers expect whole seconds only. Truncate the fraction yourself if they do.

**Parameters:**
- `dateTime`: The value to format. `Local` and `Unspecified` values are converted to UTC first.

**Returns:** The RFC 3339 string in UTC.

**Exceptions:** None thrown directly.

## Behavior notes

- **Epoch precision:** `ToUnixTimestamp` and `RemainingSeconds` both truncate sub-second parts. Round trips through `ToUnixTimestamp` and `FromUnixTimestamp` lose milliseconds.
- **Null checks:** `DateTime` is a value type, so the XML comments that mention `ArgumentNullException` on the expiry methods do not apply.
- **Kind handling:** Only `ToUnixTimestamp` and `ToRfc3339String` convert `Kind`. `IsExpired`, `IsValid`, `RemainingSeconds`, and `AddLifetime` use the raw value. Use `Utc` throughout to avoid surprises.

## Usage

```csharp
using DotnetAuthServer.Extensions;

DateTime issuedAt = DateTime.UtcNow;
DateTime expiresAt = issuedAt.AddLifetime(3600);

long expClaim = expiresAt.ToUnixTimestamp();            // JWT "exp" claim
string header = expiresAt.ToRfc3339String();            // e.g. "2025-09-24T13:34:56.0000000Z"

if (expiresAt.IsExpired())
{
    // reject the token
}

long secondsLeft = expiresAt.RemainingSeconds();

DateTime fromClaim = DateTimeExtensions.FromUnixTimestamp(expClaim);
```
