# TotpRateLimiter

**Namespace:** `DotnetAuthServer.Security`

Tracks failed TOTP verification attempts per user in a sliding window and blocks further attempts once the user reaches the configured limit. TOTP codes have only six digits and stay valid for 30 seconds, so they are easier to brute-force than passwords. This limiter therefore applies its own, stricter limit to the TOTP verification path. It is used by `MfaController` around TOTP code checks and is registered as a singleton in `Program.cs`.

The class is `sealed` and implements `IDisposable`. Its state is in memory only and is lost on restart.

## Constructor

### `TotpRateLimiter(AuthServerOptions options, ILogger<TotpRateLimiter> logger)`

Creates the limiter and starts a background cleanup timer.

**Parameters:**
- `options`: Server options. The limiter reads `TotpAttemptsPerWindow` and `TotpRateLimitWindowSeconds`.
- `logger`: Logger for block decisions.

**Exceptions:**
- `ArgumentNullException`: `options` or `logger` is `null`.

The cleanup timer runs every `max(10, TotpRateLimitWindowSeconds / 2)` seconds. The first run happens after one interval.

## Methods

### `void ThrowIfBlocked(string userId)`

Checks whether the user may attempt TOTP verification. Call it before checking the submitted code.

The limiter counts the user's failures whose timestamps fall inside the last `TotpRateLimitWindowSeconds` seconds. If the count is greater than or equal to `TotpAttemptsPerWindow`, the call logs a warning and throws. With the defaults (`5` attempts per `30` seconds), the sixth attempt inside a window is blocked.

If `userId` is `null` or whitespace, the call returns without checking anything.

**Parameters:**
- `userId`: The user's ID. Matching is case-insensitive.

**Exceptions:**
- `AuthServerException`: The user is blocked. The error code is `too_many_requests`, the status code is `429`, and the message is `Too many TOTP attempts. Please try again in {N} seconds.` `N` is the time until the oldest failure in the window expires, with a minimum of `1`.

A block lasts only while the recorded failures remain inside the window. It is not a fixed lockout period.

### `void RecordFailure(string userId)`

Records one failed TOTP verification at the current UTC time. Call it after the submitted code is rejected.

Each call also removes timestamps that are older than the window from that user's bucket. A `null` or whitespace `userId` does nothing.

**Parameters:**
- `userId`: The user's ID, or `null`.

### `void RecordSuccess(string userId)`

Clears the user's failure history after a successful TOTP verification, so the user starts with a clean window.

A `null` or whitespace `userId` does nothing.

**Parameters:**
- `userId`: The user's ID, or `null`.

### `void Dispose()`

Stops and disposes the cleanup timer. The singleton registration disposes the limiter at host shutdown.

## Behavior notes

- **Matching:** Buckets are keyed by user ID with `StringComparer.OrdinalIgnoreCase`, so IDs that differ only in case share one bucket.
- **Cleanup:** Every cleanup pass removes timestamps older than twice the window and deletes buckets that become empty. Errors during cleanup are logged at `Error` and do not stop the timer.
- **Logging:** Only blocks are logged, at `Warning`. `RecordFailure` and `RecordSuccess` do not log.
- **Thread safety:** Each user's timestamp list is guarded by a lock on that list. The bucket map itself is a `ConcurrentDictionary`.
- **Success resets the window:** `RecordSuccess` removes the whole bucket, so earlier failures no longer count toward the limit.
- **Keys are user IDs only:** Unlike `LoginRateLimiter`, this class does not track IP addresses or a global counter.

## Usage

The following sketch mirrors how `MfaController` uses the limiter. The collaborator names are placeholders.

```csharp
public async Task<IActionResult> VerifyTotpAsync(string userId, string code)
{
    _totpRateLimiter.ThrowIfBlocked(userId);

    if (!_totpService.VerifyCode(userId, code))
    {
        _totpRateLimiter.RecordFailure(userId);
        throw new InvalidGrantException("Invalid verification code.");
    }

    _totpRateLimiter.RecordSuccess(userId);
    return Ok();
}
```

`ThrowIfBlocked` throws `AuthServerException`, which `ErrorHandlingMiddleware` converts to a `429` response with the `too_many_requests` error code. Do not catch it in the caller unless you need a different response.
