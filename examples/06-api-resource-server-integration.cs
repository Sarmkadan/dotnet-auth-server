// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Example: Resource Server / API that validates tokens from dotnet-auth-server
/// This shows how to protect your APIs using tokens issued by the auth server
/// </summary>
public class ResourceServerStartupExample
{
    /// <summary>
    /// Configure authentication and authorization in your API
    /// </summary>
    public void ConfigureServices(IServiceCollection services)
    {
        const string authServerUrl = "https://localhost:7001";
        const string apiAudience = "my-api";

        // Add JWT Bearer authentication
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                // Authority is the auth server issuer URL
                options.Authority = authServerUrl;

                // Expected audience for tokens
                options.Audience = apiAudience;

                // Require HTTPS in production (remove for development)
                options.RequireHttpsMetadata = true;

                // Validate token signature using public keys from JWKS endpoint
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = authServerUrl,

                    ValidateAudience = true,
                    ValidAudience = apiAudience,

                    ValidateIssuerSigningKey = true,
                    // Public keys loaded from /.well-known/jwks.json

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),

                    // Require certain algorithms
                    ValidAlgorithms = new[] { "HS256", "RS256" }
                };

                // Handle failed token validation
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"Authentication failed: {context.Exception.Message}");
                        return Task.CompletedTask;
                    },

                    OnTokenValidated = context =>
                    {
                        Console.WriteLine(
                            $"Token validated for user: {context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value}");
                        return Task.CompletedTask;
                    }
                };
            });

        // Add authorization policies
        services.AddAuthorization(options =>
        {
            // Default policy - requires any authenticated user
            options.DefaultPolicy = new AuthorizationPolicy(
                new[] { new ClaimsAuthorizationRequirement(ClaimTypes.NameIdentifier, null) },
                new[] { JwtBearerDefaults.AuthenticationScheme });

            // Admin policy - requires admin role
            options.AddPolicy("AdminPolicy", builder =>
                builder.RequireRole("admin"));

            // Editor policy - requires editor or admin role
            options.AddPolicy("EditorPolicy", builder =>
                builder.RequireRole("editor", "admin"));

            // Scope-based policy - requires specific scope
            options.AddPolicy("ApiWritePolicy", builder =>
                builder.RequireClaim("scope", sc =>
                    sc.Contains("api:write")));

            // Complex policy - multiple requirements
            options.AddPolicy("SensitiveDataPolicy", builder =>
                builder
                    .RequireAuthenticatedUser()
                    .RequireRole("admin")
                    .RequireClaim("clearance", "secret")
                    .Build());
        });

        services.AddControllers();
    }

    /// <summary>
    /// Configure middleware pipeline
    /// </summary>
    public void Configure(IApplicationBuilder app)
    {
        app.UseRouting();

        // Authentication must come before Authorization
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}

/// <summary>
/// Example: Protected API controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IAuthorizationService _authorizationService;

    public UserController(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    /// <summary>
    /// Public endpoint - no authorization required
    /// </summary>
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublicData()
    {
        return Ok(new { message = "This is public data" });
    }

    /// <summary>
    /// Protected endpoint - requires valid token
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    public IActionResult GetProfile()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        var roles = User.FindAll(ClaimTypes.Role);

        return Ok(new
        {
            userId,
            email,
            roles = roles.Select(c => c.Value)
        });
    }

    /// <summary>
    /// Admin only endpoint
    /// </summary>
    [HttpGet("admin/users")]
    [Authorize(Roles = "admin")]
    public IActionResult GetAllUsers()
    {
        return Ok(new
        {
            message = "Fetching all users",
            users = new[]
            {
                new { id = "user1", name = "Alice" },
                new { id = "user2", name = "Bob" }
            }
        });
    }

    /// <summary>
    /// Scope-based authorization - requires api:read scope
    /// </summary>
    [HttpGet("data")]
    [Authorize("ApiWritePolicy")]
    public IActionResult GetData()
    {
        var userScopes = User.FindFirst("scope")?.Value?.Split(' ') ?? Array.Empty<string>();

        return Ok(new
        {
            message = "User has api:write scope",
            scopes = userScopes
        });
    }

    /// <summary>
    /// Role-based endpoint - editor or admin
    /// </summary>
    [HttpPost("content")]
    [Authorize("EditorPolicy")]
    public IActionResult CreateContent([FromBody] CreateContentRequest request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roles = string.Join(", ", User.FindAll(ClaimTypes.Role).Select(c => c.Value));

        return Created("", new
        {
            id = Guid.NewGuid(),
            title = request.Title,
            createdBy = userId,
            userRoles = roles
        });
    }

    /// <summary>
    /// Dynamic authorization - check at runtime
    /// </summary>
    [HttpDelete("content/{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteContentAsync(string id)
    {
        // Check if user is admin
        var isAdmin = User.IsInRole("admin");

        if (!isAdmin)
        {
            // Check custom authorization policy
            var result = await _authorizationService.AuthorizeAsync(
                User, null, "AdminPolicy");

            if (!result.Succeeded)
                return Forbid("Only admins can delete content");
        }

        return Ok(new { message = $"Content {id} deleted" });
    }

    /// <summary>
    /// Validate token manually (for custom scenarios)
    /// </summary>
    [HttpPost("validate-token")]
    [Authorize]
    public IActionResult ValidateToken()
    {
        var token = HttpContext.Request.Headers["Authorization"].ToString();

        if (string.IsNullOrEmpty(token))
            return BadRequest("No token provided");

        // Token has already been validated by middleware
        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

        return Ok(new
        {
            authenticated = true,
            claims
        });
    }
}

/// <summary>
/// Request model
/// </summary>
public class CreateContentRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Example: Custom authorization handler
/// </summary>
public class TokenAgeAuthorizationHandler : AuthorizationHandler<TokenAgeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        TokenAgeRequirement requirement)
    {
        var issuedAtClaim = context.User.FindFirst("iat");

        if (issuedAtClaim != null &&
            long.TryParse(issuedAtClaim.Value, out long iat))
        {
            var tokenAge = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - iat;

            if (tokenAge < requirement.MaxAgeSeconds)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }
        }

        return Task.CompletedTask;
    }
}

/// <summary>
/// Custom authorization requirement
/// </summary>
public class TokenAgeRequirement : IAuthorizationRequirement
{
    public long MaxAgeSeconds { get; set; }

    public TokenAgeRequirement(long maxAgeSeconds)
    {
        MaxAgeSeconds = maxAgeSeconds;
    }
}

/// <summary>
/// Example: Token validation middleware for custom scenarios
/// </summary>
public class TokenValidationMiddleware
{
    private readonly RequestDelegate _next;

    public TokenValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Extract token from header
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            // Validate token claims
            var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roles = context.User.FindAll(ClaimTypes.Role);

            // Log for audit
            Console.WriteLine(
                $"Request: {context.Request.Method} {context.Request.Path} | User: {userId} | Roles: {string.Join(",", roles.Select(r => r.Value))}");

            // Example: Enforce minimum role for sensitive endpoints
            if (context.Request.Path.StartsWithSegments("/api/admin"))
            {
                if (!context.User.IsInRole("admin"))
                {
                    context.Response.StatusCode = 403;
                    await context.Response.WriteAsync("Admin access required");
                    return;
                }
            }
        }

        await _next(context);
    }

    private string ExtractToken(HttpContext context)
    {
        var auth = context.Request.Headers["Authorization"].ToString();

        if (auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return auth["Bearer ".Length..];
        }

        return string.Empty;
    }
}

/// <summary>
/// Main example - demonstrates complete setup
/// </summary>
internal class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=== Resource Server Integration Example ===\n");

        Console.WriteLine("This example shows how to:");
        Console.WriteLine("1. Configure JWT Bearer authentication");
        Console.WriteLine("2. Protect API endpoints with [Authorize]");
        Console.WriteLine("3. Validate tokens from dotnet-auth-server");
        Console.WriteLine("4. Implement role-based access control");
        Console.WriteLine("5. Implement scope-based authorization");
        Console.WriteLine("6. Use custom authorization policies");
        Console.WriteLine("");

        // In real application, use:
        // var builder = WebApplication.CreateBuilder(args);
        // var startup = new ResourceServerStartupExample();
        // startup.ConfigureServices(builder.Services);
        // var app = builder.Build();
        // startup.Configure(app);
        // app.Run();

        Console.WriteLine("To use in your ASP.NET Core application:");
        Console.WriteLine("");
        Console.WriteLine("  1. Add to appsettings.json:");
        Console.WriteLine("     {");
        Console.WriteLine("       \"Authentication\": {");
        Console.WriteLine("         \"Authority\": \"https://localhost:7001\",");
        Console.WriteLine("         \"Audience\": \"my-api\"");
        Console.WriteLine("       }");
        Console.WriteLine("     }");
        Console.WriteLine("");
        Console.WriteLine("  2. Configure in Program.cs:");
        Console.WriteLine("     services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)");
        Console.WriteLine("       .AddJwtBearer(options => {");
        Console.WriteLine("         options.Authority = \"https://localhost:7001\";");
        Console.WriteLine("         options.Audience = \"my-api\";");
        Console.WriteLine("       });");
        Console.WriteLine("");
        Console.WriteLine("  3. Protect endpoints:");
        Console.WriteLine("     [Authorize]");
        Console.WriteLine("     [HttpGet(\"profile\")]");
        Console.WriteLine("     public IActionResult GetProfile() { ... }");
        Console.WriteLine("");

        Console.WriteLine("✓ Example completed");
    }
}
