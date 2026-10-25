# LoginRateLimiter

**Namespace:** `DotnetAuthServer.Security`

Tracks failed login attempts per username and per IP address in a sliding window and blocks callers that exceed the configured threshold. It also keeps a global failure counter as a circuit breaker against distributed attacks. It is used on the password grant path to slow brute-force and credential-stuffing attacks. It is registered as a singleton in `Program.cs` and injected into `TokenIssuer`.

The class is `sealed` and implements `IDisposable`. Its state is in memory only.

## Constructor

### `LoginRateLimiter(AuthServerOptions options, ILogger<LoginRateLimiter> logger)`

Creates the limiter and starts a background cleanup timer.

**Parameters:**
- `options`: Server options. The limiter reads `FailedLoginAttemptThreshold` and `AccountLockoutDurationMinutes`.
- `logger`: Logger for rate limit decisions.

**Exceptions:**
- `ArgumentNullException`: `options` or `logger` is `null`.

The cleanup timer runs every `max(1, AccountLockoutDurationMinutes / 2)` minutes.

## Methods

### `void ThrowIfBlocked(string? username, string? ipAddress)`

Checks whether the login may proceed. Call it before verifying credentials.

The checks run in this order:

1. **Global circuit breaker.** If the global failure count in the current window has reached `FailedLoginAttemptThreshold × 10`, the call throws. The global counter resets when `AccountLockoutDurationMinutes` have passed since the last reset. The call that performs the reset does not throw.
2. **Username.** If `username` is not null or whitespace and it has at least `FailedLoginAttemptThreshold` failures inside the window, the call throws.
3. **IP address.** The same check runs for `ipAddress`.

A null or whitespace value skips its check.

**Parameters:**
- `username`: The submitted username, or `null`.
- `ipAddress`: The client IP address, or `null`.

**Exceptions:**
- `AuthServerException`: The caller is blocked. The error code is `too_many_requests`, the message is `Too many failed login attempts. Please try again later.`, and the status code is `429`. The message is the same for every block reason, so it does not reveal whether the username exists.

A block lasts only while the recorded failures remain inside the window. It is not a fixed lockout period.

### `void RecordFailure(string? username, string? ipAddress)`

Records one failed login attempt. Call it after credential verification fails.

Each call increments the global failure count. It also adds the current UTC timestamp to the username bucket and to the IP address bucket, when those values are not null or whitespace. Each bucket drops timestamps older than the window when a new one is added.

**Parameters:**
- `username`: The submitted username, or `null`.
- `ipAddress`: The client IP address, or `null`.

### `void RecordSuccess(string? username)`

Clears the failure history for the username after a successful login.

It removes only the username bucket. It does not change the IP address buckets or the global counter. A null or whitespace username does nothing.

**Parameters:**
- `username`: The username that authenticated, or `null`.

### `void Dispose()`

Stops and disposes the cleanup timer. Call it when the limiter is no longer needed. The singleton registration in `Program.cs` disposes it at host shutdown.

## Behavior notes

- **Matching:** Username buckets use `StringComparer.OrdinalIgnoreCase`, so `Alice` and `alice` share one bucket. IP addresses use the same bucket map.
- **Cleanup:** Every cleanup pass removes timestamps older than twice the window and deletes empty buckets. It also prunes the global tracking entries.
- **Global tracking:** Per-user and per-IP entries in the global tracker are written but do not decide blocks. Only the global failure count is checked.
- **Logging:** Each public call logs at `Information`. Blocks log at `Warning`. Errors during cleanup are logged at `Error`.
- **Thread safety:** Buckets are guarded by a lock on each list. Global state is guarded by a separate lock.
- **Identifier consistency:** Pass the same normalized username and IP address to all three methods within one login flow. Otherwise failures and checks use different buckets.

## Usage

The following sketch is illustrative. The collaborator names are placeholders.

```csharp
public async Task<TokenResponse> PasswordGrantAsync(string username, string password, string? ipAddress)
{
    _loginRateLimiter.ThrowIfBlocked(username, ipAddress);

    var user = await _userRepository.FindByUsernameAsync(username);
    if (user is null || !_passwordHasher.Verify(user.PasswordHash, password))
    {
        _loginRateLimiter.RecordFailure(username, ipAddress);
        throw new InvalidGrantException("Invalid username or password.");
    }

    _loginRateLimiter.RecordSuccess(username);
    return IssueTokens(user);
}
```

`ThrowIfBlocked` throws `AuthServerException`, which `ErrorHandlingMiddleware` converts to a `429` response with the `too_many_requests` error code. Do not catch it in the caller unless you need a different response.
