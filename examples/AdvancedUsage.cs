// =============================================================================
// Advanced Usage Example
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Services;

// 1. Configure Options
var authOptions = new AuthServerOptions
{
    IssuerUrl = "https://auth.example.com",
    JwtSigningKey = "very-long-and-secure-signing-key-for-production",
    JwtAlgorithm = "RS256", // Using RS256 for production
    AccessTokenLifetimeSeconds = 3600,
    RequirePkceForAllClients = true
};

var cacheOptions = new CacheOptions
{
    Enabled = true,
    DefaultTtlSeconds = 300
};

// 2. Set up Service Collection
var services = new ServiceCollection();

// 3. Register Core Services and Options
services.AddSingleton(authOptions);
services.AddSingleton(cacheOptions);

// 4. Register Services with custom implementations
services.AddSingleton<ICacheService, MemoryCacheService>();
services.AddScoped<TokenService>();
services.AddScoped<ClientValidationService>();

// 5. Example of custom error handling or logging configuration could go here
// services.AddLogging(...);

var serviceProvider = services.BuildServiceProvider();

Console.WriteLine($"Auth Server configured for: {authOptions.IssuerUrl}");
Console.WriteLine($"PKCE Required: {authOptions.RequirePkceForAllClients}");
