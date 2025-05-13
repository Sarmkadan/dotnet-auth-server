// =============================================================================
// Basic Usage Example
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Services;

// 1. Configure the Auth Server Options
var authOptions = new AuthServerOptions
{
    IssuerUrl = "https://localhost:7001",
    JwtSigningKey = "super-secret-key-must-be-at-least-32-bytes-long-for-hs256",
    JwtAlgorithm = "HS256"
};

// 2. Set up the Service Collection
var services = new ServiceCollection();

// 3. Register necessary services
services.AddSingleton(authOptions);
services.AddScoped<TokenService>();
services.AddScoped<AuthorizationService>();

// 4. Build the provider
var serviceProvider = services.BuildServiceProvider();

// 5. Use the TokenService (for example, to issue a token)
var tokenService = serviceProvider.GetRequiredService<TokenService>();
Console.WriteLine("TokenService initialized successfully.");
