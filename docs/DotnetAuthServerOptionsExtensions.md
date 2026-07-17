# DotnetAuthServerOptionsExtensions
The `DotnetAuthServerOptionsExtensions` class provides a set of static methods that extend the functionality of the `DotnetAuthServerOptions` class, allowing for the validation and retrieval of various configuration settings and properties. These extensions enable developers to easily access and manipulate the configuration options of the authentication server, streamlining the development process.

## API
The `DotnetAuthServerOptionsExtensions` class includes the following public members:
* `IsValid`: Returns a boolean indicating whether the configuration is valid.
* `GetEffectiveCacheBackend`: Returns a string representing the effective cache backend.
* `UsesRedisCache`: Returns a boolean indicating whether Redis cache is used.
* `GetEffectiveJwtAlgorithm`: Returns a string representing the effective JWT algorithm.
* `GetSupportedScopes`: Returns a read-only list of strings representing the supported scopes.
* `GetSupportedGrantTypes`: Returns a read-only list of strings representing the supported grant types.
* `SupportsScope`: Returns a boolean indicating whether a specific scope is supported.
* `SupportsGrantType`: Returns a boolean indicating whether a specific grant type is supported.
* `GetEffectiveMinimumLogLevel`: Returns a string representing the effective minimum log level.
* `IsSensitiveDataLoggingEnabled`: Returns a boolean indicating whether sensitive data logging is enabled.
* `GetEffectiveOpaPolicyPath`: Returns a string representing the effective OPA policy path.
* `GetOpaPolicyUrl`: Returns a string representing the OPA policy URL.
* `GetAccessTokenLifetimeDisplay`: Returns a string representing the access token lifetime display.
* `GetRefreshTokenLifetimeDisplay`: Returns a string representing the refresh token lifetime display.
* `GetCacheDefaultExpirationDisplay`: Returns a string representing the cache default expiration display.

## Usage
The following examples demonstrate how to use the `DotnetAuthServerOptionsExtensions` class:
```csharp
// Example 1: Validate configuration and retrieve effective cache backend
var options = new DotnetAuthServerOptions();
if (DotnetAuthServerOptionsExtensions.IsValid(options))
{
    var cacheBackend = DotnetAuthServerOptionsExtensions.GetEffectiveCacheBackend(options);
    Console.WriteLine($"Effective cache backend: {cacheBackend}");
}

// Example 2: Check supported scopes and grant types
var supportedScopes = DotnetAuthServerOptionsExtensions.GetSupportedScopes(options);
var supportedGrantTypes = DotnetAuthServerOptionsExtensions.GetSupportedGrantTypes(options);
Console.WriteLine("Supported scopes:");
foreach (var scope in supportedScopes)
{
    Console.WriteLine(scope);
}
Console.WriteLine("Supported grant types:");
foreach (var grantType in supportedGrantTypes)
{
    Console.WriteLine(grantType);
}
```

## Notes
When using the `DotnetAuthServerOptionsExtensions` class, note the following:
* The `IsValid` method may throw an exception if the configuration is invalid.
* The `GetEffectiveCacheBackend`, `GetEffectiveJwtAlgorithm`, `GetEffectiveMinimumLogLevel`, `GetEffectiveOpaPolicyPath`, `GetOpaPolicyUrl`, `GetAccessTokenLifetimeDisplay`, `GetRefreshTokenLifetimeDisplay`, and `GetCacheDefaultExpirationDisplay` methods may return null or empty strings if the corresponding configuration settings are not set.
* The `UsesRedisCache`, `SupportsScope`, `SupportsGrantType`, and `IsSensitiveDataLoggingEnabled` methods may return false if the corresponding configuration settings are not enabled or supported.
* The `GetSupportedScopes` and `GetSupportedGrantTypes` methods return read-only lists, which should not be modified.
* The `DotnetAuthServerOptionsExtensions` class is thread-safe, as it only provides static methods that do not modify any shared state. However, the underlying configuration settings may not be thread-safe, and should be accessed and modified accordingly.
