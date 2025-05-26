# DateTimeExtensions

A set of extension methods for `System.DateTime` that provide common utilities for working with Unix timestamps, expiration checks, lifetime adjustments, and RFC 3339 formatting in the dotnet‑auth‑server project.

## API

### `public static long ToUnixTimestamp(this DateTime dateTime)`
**Purpose**  
Converts a `DateTime` value to the number of whole seconds that have elapsed since the Unix epoch (1970‑01‑01T00:00:00Z).

**Parameters**  
- `dateTime`: The `DateTime` instance to convert. The method treats the value as UTC; if the `Kind` is `Local` it is first converted to UTC.

**Return Value**  
A `long` representing the Unix timestamp in seconds.

**Exceptions**  
- `ArgumentOutOfRangeException` if the resulting timestamp is outside the range of a signed 64‑bit integer (practically impossible for .NET `DateTime` values).

---

### `public static DateTime FromUnixTimestamp(this long unixTimestamp)`
**Purpose**  
Creates a `DateTime` from a Unix timestamp expressed in seconds.

**Parameters**  
- `unixTimestamp`: The number of seconds elapsed since 1970‑01‑01T00:00:00Z.

**Return Value**  
A `DateTime` with `Kind` set to `Utc` representing the corresponding point in time.

**Exceptions**  
- `ArgumentOutOfRangeException` if `unixTimestamp` would result in a `DateTime` earlier than `DateTime.MinValue` or later than `DateTime.MaxValue`.

---

### `public static bool IsExpired(this DateTime dateTime)`
**Purpose**  
Determines whether the supplied `DateTime` represents a moment in the past relative to the current UTC time.

**Parameters**  
- `dateTime`: The `DateTime` instance to test. The method compares the UTC representation of `dateTime` with `DateTime.UtcNow`.

**Return Value**  
`true` if `dateTime` is earlier than the current UTC time; otherwise `false`.

**Exceptions**  
None.

---

### `public static bool IsValid(this DateTime dateTime)`
**Purpose**  
Checks whether the `DateTime` value is within the supported range and is not an unset sentinel (`DateTime.MinValue`).

**Parameters**  
- `dateTime`: The `DateTime` instance to validate.

**Return Value**  
`true` if `dateTime` is greater than `DateTime.MinValue` and less than `DateTime.MaxValue`; otherwise `false`.

**Exceptions**  
None.

---

### `public static long RemainingSeconds(this DateTime dateTime)`
**Purpose**  
Calculates the number of whole seconds remaining from the current UTC time until the supplied `DateTime`. If the moment is in the past, returns `0`.

**Parameters**  
- `dateTime`: The target `DateTime` (treated as UTC).

**Return Value**  
A non‑negative `long` indicating the seconds left until `dateTime`. Returns `0` when `dateTime` is already past.

**Exceptions**  
None.

---

### `public static DateTime AddLifetime(this DateTime dateTime)`
**Purpose**  
Adds a predefined lifetime interval to the supplied `DateTime`. The lifetime constant is defined elsewhere in the auth server (e.g., token validity period).

**Parameters**  
- `dateTime`: The base `DateTime` to which the lifetime is added. The method does not modify the original instance.

**Return Value**  
A new `DateTime` representing `dateTime` plus the configured lifetime, with `Kind` preserved.

**Exceptions**  
None.

---

### `public static string ToRfc3339String(this DateTime dateTime)`
**Purpose**  
Formats the `DateTime` as an RFC 3339‑compliant string (ISO 8601 with UTC offset “Z”).

**Parameters**  
- `dateTime`: The `DateTime` instance to format. The value is converted to UTC before formatting.

**Return Value**  
A string such as `"2025-09-24T12:34:56Z"` representing the UTC instant.

**Exceptions**  
None.

## Usage

```csharp
using System;
using DotNetAuthServer.Extensions; // namespace containing DateTimeExtensions

// Convert to and from Unix timestamp
DateTime now = DateTime.UtcNow;
long stamp = now.ToUnixTimestamp();          // e.g., 1737779695
DateTime fromStamp = stamp.FromUnixTimestamp(); // round‑trip yields same instant

// Check expiration and remaining time
DateTime expires = now.AddMinutes(5);
bool expired = expires.IsExpired();          // false
long secsLeft = expires.RemainingSeconds();  // approximately 300

// Apply a configured lifetime and produce an RFC 3339 string
DateTime issued = DateTime.UtcNow;
DateTime validUntil = issued.AddLifetime(); // adds server‑defined token lifetime
string header = validUntil.ToRfc3339String(); // suitable for HTTP headers or JWT claims
```

```csharp
// Defensive validation before using a DateTime from an untrusted source
DateTime userProvided = ParseUserInput(); // hypothetical method
if (!userProvided.IsValid())
{
    throw new ArgumentException("The supplied date is not valid.");
}

// If the date is in the past, treat it as expired immediately
if (userProvided.IsExpired())
{
    HandleExpiredToken();
}
else
{
    long remaining = userProvided.RemainingSeconds();
    // Use remaining seconds for sliding‑expiration logic
}
```

## Notes

- All extension methods operate on the UTC representation of the input `DateTime`. If the original `DateTime.Kind` is `Local`, the methods first convert it to UTC; therefore the caller should be aware that the returned values reflect UTC time.
- The methods are pure and stateless; they do not modify the instance on which they are invoked and have no side effects, making them thread‑safe for concurrent use.
- `FromUnixTimestamp` will throw if the timestamp would produce a `DateTime` outside the .NET supported range (`DateTime.MinValue`/`DateTime.MaxValue`). Applications receiving timestamps from external sources should validate the range before calling this method.
- `IsExpired` and `RemainingSeconds` rely on `DateTime.UtcNow` at the moment of invocation; frequent calls in a tight loop may observe slight variations due to clock progression.
- The lifetime added by `AddLifetime` is immutable for the duration of the process; if the server’s configuration changes, the AppDomain must be reloaded for the new value to take effect. The method itself remains thread‑safe regardless of configuration changes.
