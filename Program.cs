// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Caching;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Events;
using DotnetAuthServer.Formatters;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Integration;
using DotnetAuthServer.Middleware;
using DotnetAuthServer.Services;

var builder = WebApplication.CreateBuilder(args);

// Load configuration
var authServerOptions = new AuthServerOptions
{
    IssuerUrl = builder.Configuration["AuthServer:IssuerUrl"] ?? "https://localhost:7001",
    JwtSigningKey = builder.Configuration["AuthServer:JwtSigningKey"] ?? GenerateDefaultSigningKey(),
    JwtAlgorithm = builder.Configuration["AuthServer:JwtAlgorithm"] ?? "HS256",
    AccessTokenLifetimeSeconds = int.TryParse(
        builder.Configuration["AuthServer:AccessTokenLifetimeSeconds"], out var atl) ? atl : 3600,
    RefreshTokenLifetimeSeconds = int.TryParse(
        builder.Configuration["AuthServer:RefreshTokenLifetimeSeconds"], out var rtl) ? rtl : 2592000,
    AuthorizationCodeLifetimeSeconds = int.TryParse(
        builder.Configuration["AuthServer:AuthorizationCodeLifetimeSeconds"], out var acl) ? acl : 300,
    RequirePkceForAllClients = bool.TryParse(
        builder.Configuration["AuthServer:RequirePkceForAllClients"], out var pkce) && pkce,
    DatabaseConnectionString = builder.Configuration["ConnectionStrings:DefaultConnection"] ?? "",
    UseInMemoryDatabase = bool.TryParse(
        builder.Configuration["AuthServer:UseInMemoryDatabase"], out var inmem) && inmem,
};

if (!authServerOptions.IsValid())
{
    throw new InvalidOperationException("AuthServer configuration is invalid. Check appsettings.json");
}

// Load Phase 2 configuration options
var loggingOptions = new LoggingOptions();
builder.Configuration.GetSection("Logging").Bind(loggingOptions);

var cacheOptions = new CacheOptions();
builder.Configuration.GetSection("Cache").Bind(cacheOptions);

var webhookOptions = new WebhookOptions();
builder.Configuration.GetSection("Webhooks").Bind(webhookOptions);

// Add services to the container
builder.Services.AddSingleton(authServerOptions);
builder.Services.AddSingleton(loggingOptions);
builder.Services.AddSingleton(cacheOptions);
builder.Services.AddSingleton(webhookOptions);

// Repositories
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IClientRepository, ClientRepository>();
builder.Services.AddSingleton<IAuthorizationGrantRepository, AuthorizationGrantRepository>();
builder.Services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<IConsentRepository, ConsentRepository>();

// Phase 1 Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<ConsentService>();

// Phase 2 Services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<ClientValidationService>();
builder.Services.AddScoped<ScopeValidationService>();
builder.Services.AddScoped<AuditLoggingService>();
builder.Services.AddScoped<PolicyEnforcementService>();
builder.Services.AddScoped<PkceValidationService>();
builder.Services.AddScoped<SessionStateService>();

// Event system
builder.Services.AddSingleton<IEventPublisher>(sp => new EventPublisher(sp.GetRequiredService<ILogger<EventPublisher>>()));

// Handlers
builder.Services.AddScoped<TokenIntrospectionHandler>();
builder.Services.AddScoped<TokenRevocationHandler>();
builder.Services.AddScoped<UserinfoHandler>();
builder.Services.AddScoped<ScopeMetadataHandler>();
builder.Services.AddScoped<DeviceFlowHandler>();
builder.Services.AddScoped<JwksHandler>();
builder.Services.AddScoped<RequestValidationHandler>();

// Formatters
builder.Services.AddScoped<JwtTokenFormatter>();

// Additional Services
builder.Services.AddScoped<SecretsService>();
builder.Services.AddScoped<ClaimsEnrichmentService>();

// Background workers
builder.Services.AddHostedService<TokenCleanupWorker>();

// HTTP Clients
builder.Services.AddHttpClient<WebhookClient>()
    .ConfigureHttpClient(client => client.Timeout = webhookOptions.Timeout);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "OAuth2/OIDC Authorization Server",
        Version = "v1",
        Description = "Minimal OAuth2/OIDC authorization server with PKCE, refresh token rotation, consent, RBAC+ABAC",
        Contact = new()
        {
            Name = "Vladyslav Zaiets",
            Url = new Uri("https://sarmkadan.com")
        },
        License = new()
        {
            Name = "MIT",
            Url = new Uri("https://github.com/sarmkadan/dotnet-auth-server/blob/main/LICENSE")
        }
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure middleware pipeline
// Order is important: error handling should be early, CORS before routing, logging everywhere
app.UseMiddleware<RequestContextMiddleware>();
app.UseMiddleware<LoggingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<ErrorHandlingMiddleware>();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Authorization Server V1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

// OAuth2 metadata endpoint
app.MapGet("/.well-known/oauth-authorization-server", () =>
{
    return Results.Json(new
    {
        issuer = authServerOptions.IssuerUrl,
        authorization_endpoint = $"{authServerOptions.IssuerUrl}/oauth/authorize",
        token_endpoint = $"{authServerOptions.IssuerUrl}/oauth/token",
        revocation_endpoint = $"{authServerOptions.IssuerUrl}/oauth/revoke",
        introspection_endpoint = $"{authServerOptions.IssuerUrl}/oauth/introspect",
        userinfo_endpoint = $"{authServerOptions.IssuerUrl}/oauth/userinfo",
        scopes_supported = authServerOptions.SupportedScopes,
        grant_types_supported = authServerOptions.SupportedGrantTypes,
        token_endpoint_auth_methods_supported = new[] { "client_secret_basic", "client_secret_post", "none" },
        code_challenge_methods_supported = new[] { "plain", "S256" }
    });
})
.WithOpenApi()
.WithName("GetOAuth2Metadata")
.WithDescription("Returns OAuth2 authorization server metadata");

// OIDC metadata endpoint
app.MapGet("/.well-known/openid-configuration", () =>
{
    return Results.Json(new
    {
        issuer = authServerOptions.IssuerUrl,
        authorization_endpoint = $"{authServerOptions.IssuerUrl}/oauth/authorize",
        token_endpoint = $"{authServerOptions.IssuerUrl}/oauth/token",
        userinfo_endpoint = $"{authServerOptions.IssuerUrl}/oauth/userinfo",
        jwks_uri = $"{authServerOptions.IssuerUrl}/.well-known/jwks.json",
        scopes_supported = authServerOptions.SupportedScopes,
        response_types_supported = new[] { "code", "token", "id_token", "code id_token", "code token", "id_token token", "code id_token token" },
        subject_types_supported = new[] { "public" },
        id_token_signing_alg_values_supported = new[] { "HS256", "RS256" },
        code_challenge_methods_supported = new[] { "plain", "S256" }
    });
})
.WithOpenApi()
.WithName("GetOpenIdConfiguration")
.WithDescription("Returns OpenID Connect configuration");

// Health check endpoint
app.MapGet("/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
})
.WithOpenApi()
.WithName("Health")
.WithDescription("Health check endpoint");

Console.WriteLine($"Authorization Server starting at {authServerOptions.IssuerUrl}");
app.Run();

/// <summary>
/// Generates a default 256-bit signing key (should be configured in production)
/// </summary>
static string GenerateDefaultSigningKey()
{
    var bytes = new byte[32];
    using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
    {
        rng.GetBytes(bytes);
    }
    return Convert.ToBase64String(bytes);
}
