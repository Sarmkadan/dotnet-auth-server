# LoginRateLimiter

The `LoginRateLimiter` class is responsible for managing login attempt tracking to prevent brute-force attacks on authentication endpoints. It tracks failed attempts associated with specific identifiers, such as usernames or IP addresses, and enforces temporary blocking mechanisms when defined thresholds are exceeded.

## API

### `LoginRateLimiter()`
Initializes a new instance of the `LoginRateLimiter` class.

### `ThrowIfBlocked(string identifier)`
Checks if the specified identifier is currently subject to a temporary block due to repeated failed login attempts.

*   **Parameters:**
    *   `identifier`: The unique identifier (e.g., username or IP address) to check.
*   **Exceptions:**
    *   Throws an `AuthenticationBlockedException` if the identifier is currently blocked.

### `RecordFailure(string identifier)`
Records a failed login attempt for the given identifier. This may increment a failure count or trigger a block status based on internal configuration policies.

*   **Parameters:**
    *   `identifier`: The unique identifier associated with the failed attempt.

### `RecordSuccess(string identifier)`
Resets the failure tracking status for the specified identifier, indicating a successful authentication.

*   **Parameters:**
    *   `identifier`: The unique identifier to be cleared of failure history.

## Usage

### Basic Usage in an Authentication Controller
```csharp
public IActionResult Login(LoginRequest request)
{
    _rateLimiter.ThrowIfBlocked(request.Username);

    var result = _authService.Authenticate(request);
    if (result.Success)
    {
        _rateLimiter.RecordSuccess(request.Username);
        return Ok();
    }

    _rateLimiter.RecordFailure(request.Username);
    return Unauthorized();
}
```

### Integration with a Service Layer
```csharp
public async Task<AuthResult> LoginAsync(string username, string password)
{
    try
    {
        _rateLimiter.ThrowIfBlocked(username);
        var user = await _userService.GetUserAsync(username);
        
        if (user != null && _passwordHasher.Verify(user.Hash, password))
        {
            _rateLimiter.RecordSuccess(username);
            return AuthResult.Success();
        }
        
        _rateLimiter.RecordFailure(username);
        return AuthResult.Failure("Invalid credentials.");
    }
    catch (AuthenticationBlockedException)
    {
        return AuthResult.Failure("Too many failed attempts. Try again later.");
    }
}
```

## Notes

*   **Thread-Safety:** The `LoginRateLimiter` is designed to be thread-safe, allowing safe access from multiple concurrent requests in a multi-threaded server environment.
*   **Identifier Scoping:** Ensure the `identifier` provided to the methods is consistent throughout the authentication flow (e.g., always use normalized usernames or client IP addresses).
*   **Configuration:** The behavior, such as maximum allowed failures and block duration, is determined by the system's current configuration policies.
*   **Exceptions:** The `ThrowIfBlocked` method is intended to be called immediately before authentication processing to short-circuit the request if the identity is already blocked.
