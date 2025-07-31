// existing content ...

## CacheOptionsExtensions

The `CacheOptionsExtensions` class provides a set of extension methods for configuring cache settings. It allows you to use Redis or memory caching, set default expiration, maximum entries, and expiration scan interval, as well as get expiration seconds and set expiration.

### Usage Example

```csharp
// Use Redis caching
var cacheOptions = CacheOptionsExtensions.UseRedis(options);

// Use memory caching
cacheOptions = CacheOptionsExtensions.UseMemory(options);

// Set default expiration
cacheOptions = CacheOptionsExtensions.SetDefaultExpiration(options, TimeSpan.FromHours(1));

// Set maximum entries
cacheOptions = CacheOptionsExtensions.SetMaxEntries(options, 1000);

// Set expiration scan interval
cacheOptions = CacheOptionsExtensions.SetExpirationScanInterval(options, TimeSpan.FromMinutes(5));

// Get expiration seconds
var expirationSeconds = CacheOptionsExtensions.GetExpirationSeconds(options);

// Set expiration
cacheOptions = CacheOptionsExtensions.SetExpiration(options, TimeSpan.FromHours(2));

// Disable caching
cacheOptions = CacheOptionsExtensions.Disable(options);

// Enable caching
cacheOptions = CacheOptionsExtensions.Enable(options);
```

## ClientCredentialsFlowExampleValidation

The `ClientCredentialsFlowExampleValidation` class validates client credentials in an authentication flow. It provides methods to check validity, collect validation errors, and enforce validation rules.

### Usage Example

```csharp
var clientId = "my-client-id";
var clientSecret = "my-client-secret";

// Get validation errors
var errors = ClientCredentialsFlowExampleValidation.Validate(clientId, clientSecret);

// Check if valid
if (ClientCredentialsFlowExampleValidation.IsValid(clientId, clientSecret))
{
    Console.WriteLine("Client credentials are valid.");
}
else
{
    Console.WriteLine("Client credentials are invalid.");
}

// Enforce validity (throws on invalid)
try
{
    ClientCredentialsFlowExampleValidation.EnsureValid(clientId, clientSecret);
}
catch (Exception ex)
{
    Console.WriteLine($"Validation failed: {ex.Message}");
}
```

// ... rest of content ...
