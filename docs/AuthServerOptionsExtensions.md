# AuthServerOptionsExtensions

The `AuthServerOptionsExtensions` class provides a suite of static extension methods designed to simplify the retrieval and validation of configuration settings from an `AuthServerOptions` instance within the `dotnet-auth-server` framework. These methods centralize the logic for accessing operational parameters such as token lifetimes, security thresholds, and feature toggles, ensuring consistent configuration handling across the application.

## API

All methods are static extension methods for the `AuthServerOptions` type. They throw an `ArgumentNullException` if the `options` parameter is `null`.

*   **`bool Validate(this AuthServerOptions options)`**
    Validates the current configuration settings within the `AuthServerOptions` instance to ensure they meet required server constraints. Returns `true` if the configuration is valid, otherwise `false`.
*   **`bool SupportsScope(this AuthServerOptions options)`**
    Indicates whether the authorization server is configured to support scope validation. Returns `true` if scopes are enabled, otherwise `false`.
*   **`bool SupportsGrantType(this AuthServerOptions options)`**
    Indicates whether the authorization server is configured to support grant types. Returns `true` if grant types are enabled, otherwise `false`.
*   **`TimeSpan GetAccessTokenLifetime(this AuthServerOptions options)`**
    Retrieves the configured lifetime for access tokens.
*   **`TimeSpan GetRefreshTokenLifetime(this AuthServerOptions options)`**
    Retrieves the configured lifetime for refresh tokens.
*   **`TimeSpan GetAuthorizationCodeLifetime(this AuthServerOptions options)`**
    Retrieves the configured lifetime for authorization codes.
*   **`TimeSpan GetClockSkewTolerance(this AuthServerOptions options)`**
    Retrieves the configured clock skew tolerance for token validation.
*   **`TimeSpan GetAccountLockoutDuration(this AuthServerOptions options)`**
    Retrieves the configured duration for which an account remains locked after reaching the failed login threshold.
*   **`bool IsPkceRequired(this AuthServerOptions options)`**
    Indicates whether Proof Key for Code Exchange (PKCE) is required for authorization requests. Returns `true` if required, otherwise `false`.
*   **`bool IsTokenRotationEnabled(this AuthServerOptions options)`**
    Indicates whether refresh token rotation is enabled. Returns `true` if enabled, otherwise `false`.
*   **`int GetMaxRefreshTokenGenerations(this AuthServerOptions options)`**
    Retrieves the maximum number of times a refresh token can be rotated (generated).
*   **`int GetFailedLoginAttemptThreshold(this AuthServerOptions options)`**
    Retrieves the maximum number of failed login attempts allowed before an account is locked.
*   **`bool IsUserConsentRequired(this AuthServerOptions options)`**
    Indicates whether user consent is required for authorization requests. Returns `true` if consent is required, otherwise `false`.
*   **`bool UsesInMemoryDatabase(this AuthServerOptions options)`**
    Indicates whether the authorization server is configured to use an in-memory database provider. Returns `true` if configured to use in-memory, otherwise `false`.

## Usage

```csharp
// Example 1: Validating options during startup
public void ConfigureServices(IServiceCollection services, AuthServerOptions options)
{
    if (!options.Validate())
    {
        throw new InvalidOperationException("Invalid authorization server configuration.");
    }
    
    // Proceed with registration...
}
```

```csharp
// Example 2: Using configuration to adjust service logic
public void CreateToken(AuthServerOptions options)
{
    TimeSpan lifetime = options.GetAccessTokenLifetime();
    bool pkceRequired = options.IsPkceRequired();
    
    Console.WriteLine($"Token lifetime set to: {lifetime.TotalMinutes} minutes.");
    Console.WriteLine($"PKCE required: {pkceRequired}");
}
```

## Notes

*   **Thread Safety:** These extension methods are thread-safe, provided that the underlying `AuthServerOptions` instance is not being modified by another thread during invocation. It is recommended to treat the `AuthServerOptions` instance as immutable once the server has started.
*   **Null Checks:** Every method enforces a null check on the `options` parameter. Ensure that the `AuthServerOptions` object is properly initialized before calling any of these extension methods to avoid runtime `ArgumentNullException` exceptions.
*   **Performance:** These methods are designed to perform simple property access or minor arithmetic on the `AuthServerOptions` object; they are highly performant and suitable for use within hot paths, such as token generation or validation requests.
