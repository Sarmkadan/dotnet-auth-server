#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.BackgroundWorkers;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Events;
using DotnetAuthServer.Formatters;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Integration;
using DotnetAuthServer.Middleware;
using DotnetAuthServer.Security;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Configure options
builder.Services.AddOptions<DotnetAuthServerOptions>()
    .Bind(builder.Configuration.GetSection("DotnetAuthServer"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<WebhookOptions>()
    .Bind(builder.Configuration.GetSection("Webhooks"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Backward compatibility registration for services
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DotnetAuthServerOptions>>().Value.AuthServer);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DotnetAuthServerOptions>>().Value.Cache);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DotnetAuthServerOptions>>().Value.Logging);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<DotnetAuthServerOptions>>().Value.Opa);
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<WebhookOptions>>().Value);

// Repositories
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<IClientRepository, ClientRepository>();
builder.Services.AddSingleton<IAuthorizationGrantRepository, AuthorizationGrantRepository>();
builder.Services.AddSingleton<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddSingleton<DotnetAuthServer.Services.IConsentRepository, DotnetAuthServer.Services.ConsentRepository>();
builder.Services.AddSingleton<IUserSessionRepository, UserSessionRepository>();
builder.Services.AddSingleton<ITotpCredentialRepository, TotpCredentialRepository>();

// Phase 1 Services
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<AuthorizationService>();
builder.Services.AddScoped<ConsentService>();

// Security
builder.Services.AddSingleton<RevokedTokenStore>();
builder.Services.AddSingleton<LoginRateLimiter>();

// Phase 2 Services
builder.Services.AddSingleton<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<ClientValidationService>();
builder.Services.AddScoped<ScopeValidationService>();
builder.Services.AddScoped<AuditLoggingService>();
builder.Services.AddScoped<PolicyEnforcementService>();
builder.Services.AddScoped<PkceValidationService>();
builder.Services.AddScoped<SessionStateService>();
builder.Services.AddScoped<DynamicClientRegistrationService>();

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
builder.Services.AddScoped<UserService>();

// Phase 3 Services — User Management, Session Dashboard, MFA/TOTP
builder.Services.AddScoped<UserManagementService>();
builder.Services.AddScoped<UserSessionService>();
builder.Services.AddScoped<TotpService>();

// Background workers
builder.Services.AddHostedService<TokenCleanupWorker>();

// HTTP Clients
builder.Services.AddHttpClient<WebhookClient>()
    .ConfigureHttpClient((sp, client) => {
        var options = sp.GetRequiredService<WebhookOptions>();
        client.Timeout = options.Timeout;
    });

// Optional OPA client — registered only when integration is enabled so the
// HttpClient factory doesn't create unnecessary connections.
var opaOptions = builder.Configuration.GetSection("DotnetAuthServer:Opa").Get<OpaOptions>() ?? new OpaOptions();
if (opaOptions.Enabled)
{
    builder.Services.AddHttpClient<OpaClient>()
        .ConfigureHttpClient(client =>
        {
            client.BaseAddress = new Uri(opaOptions.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(opaOptions.TimeoutSeconds);
        });
}

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
var authServerOptions = app.Services.GetRequiredService<AuthServerOptions>();
var resolvedOpaOptions = app.Services.GetRequiredService<OpaOptions>();
var webhookOptions = app.Services.GetRequiredService<WebhookOptions>();

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
        registration_endpoint = $"{authServerOptions.IssuerUrl}/register",
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
        registration_endpoint = $"{authServerOptions.IssuerUrl}/register",
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

// JWKS endpoint - advertised in the metadata documents above, so it has to exist.
// Key material for symmetric keys is never included (see JwksHandler).
app.MapGet("/.well-known/jwks.json", async (JwksHandler jwksHandler, CancellationToken cancellationToken) =>
{
    var jwks = await jwksHandler.GetJwksAsync(cancellationToken);
    return Results.Json(jwks);
})
.WithOpenApi()
.WithName("GetJwks")
.WithDescription("Returns the JSON Web Key Set used to validate tokens issued by this server");

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
