// =============================================================================
// Integration Example (ASP.NET Core)
// =============================================================================

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Register Auth Server Options (from configuration)
var authOptions = new AuthServerOptions
{
    IssuerUrl = builder.Configuration["AuthServer:IssuerUrl"] ?? "https://localhost:7001",
    JwtSigningKey = builder.Configuration["AuthServer:JwtSigningKey"] ?? "default-secret-key-must-be-changed"
};
builder.Services.AddSingleton(authOptions);

// 2. Register Auth Server Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthorizationService>();

// 3. Register standard ASP.NET Core services
builder.Services.AddControllers();

var app = builder.Build();

// 4. Configure Middleware
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Now you can inject TokenService or AuthorizationService into your controllers
// app.Run(); // Uncomment to run
