# DotnetAuthServerOptions

`DotnetAuthServerOptions` serves as the top-level configuration container for the `dotnet-auth-server` application. It aggregates distinct configuration sections—authentication server behaviour, caching parameters, logging control, and Open Policy Agent integration—into a single object that can be bound from application settings and passed through the dependency injection pipeline.

## API

### `public AuthServerOptions AuthServer`

Gets or sets the configuration for the core authentication server.

- **Purpose**: Controls issuer URI, signing credentials, token lifetimes, audience validation, and other intrinsic behaviours of the authorization server endpoints.
- **Type**: `AuthServerOptions`
- **Access**: Read/write property.
- **Exceptions**: No exceptions are thrown by the property accessor itself. Validation occurs when the options are consumed during server startup; invalid values (e.g., a missing signing key) will cause a configuration exception at that point.

### `public CacheOptions Cache`

Gets or sets the caching configuration used by the server for token introspection results, discovery documents, or other repeatable lookups.

- **Purpose**: Defines cache duration, maximum entry count, eviction policy, and whether caching is enabled at all.
- **Type**: `CacheOptions`
- **Access**: Read/write property.
- **Exceptions**: No exceptions on get/set. Invalid combinations (e.g., negative timeouts) are surfaced as validation errors during cache initialisation.

### `public LoggingOptions Logging`

Gets or sets the logging configuration.

- **Purpose**: Specifies minimum log levels, output sinks, structured logging format, and any category-specific overrides for the server and its dependencies.
- **Type**: `LoggingOptions`
- **Access**: Read/write property.
- **Exceptions**: The property accessor does not throw. Malformed logging configuration may cause the logging provider to fall back to defaults or emit a warning during host build.

### `public OpaOptions Opa`

Gets or sets the Open Policy Agent integration options.

- **Purpose**: Configures the OPA endpoint URL, query path, timeout, retry policy, and optional authentication credentials used when the server evaluates authorisation policies via an external OPA instance.
- **Type**: `OpaOptions`
- **Access**: Read/write property.
- **Exceptions**: No exceptions on property access. A misconfigured URL or unreachable endpoint will result in runtime policy evaluation failures, not construction-time errors.

## Usage

### Example 1: Binding from `appsettings.json` and registering in DI

```csharp
// appsettings.json excerpt:
// {
//   "DotnetAuthServer": {
//     "AuthServer": { "Issuer": "https://auth.example.com", ... },
//     "Cache": { "Enabled": true, "DefaultTtlSeconds": 300 },
//     "Logging": { "MinimumLevel": "Information" },
//     "Opa": { "BaseUrl": "https://opa.example.com", "PolicyPath": "/v1/data/authz/allow" }
//   }
// }

var builder = WebApplication.CreateBuilder(args);

// Bind the entire section to DotnetAuthServerOptions
builder.Services
    .Configure<DotnetAuthServerOptions>(
        builder.Configuration.GetSection("DotnetAuthServer"));

// Or register as a singleton for direct injection
builder.Services.AddSingleton(sp =>
{
    var options = new DotnetAuthServerOptions();
    builder.Configuration.GetSection("DotnetAuthServer").Bind(options);
    return options;
});
```

### Example 2: Programmatic construction with conditional OPA

```csharp
var options = new DotnetAuthServerOptions
{
    AuthServer = new AuthServerOptions
    {
        Issuer = "https://idp.internal",
        AccessTokenLifetime = TimeSpan.FromHours(1)
    },
    Cache = new CacheOptions
    {
        Enabled = true,
        DefaultTtlSeconds = 600
    },
    Logging = new LoggingOptions
    {
        MinimumLevel = LogLevel.Debug
    }
};

// Conditionally enable OPA only when an environment variable is set
var opaUrl = Environment.GetEnvironmentVariable("OPA_BASE_URL");
if (!string.IsNullOrWhiteSpace(opaUrl))
{
    options.Opa = new OpaOptions
    {
        BaseUrl = opaUrl,
        PolicyPath = "/v1/data/authz/allow",
        TimeoutSeconds = 5
    };
}

// Pass options to the server initialisation
var server = new AuthorizationServer(options);
await server.StartAsync();
```

## Notes

- **Null members**: Each property can be `null` if not explicitly initialised. Consumers of `DotnetAuthServerOptions` must guard against null references when accessing nested options. The typical pattern is to apply sensible defaults via `Configure` post-binding or to initialise all sub-options in the constructor.
- **Validation timing**: The `DotnetAuthServerOptions` class itself performs no validation. All validation is deferred to the components that consume each section. This means an instance with completely invalid values can be constructed and passed around without immediate errors.
- **Thread safety**: The property getters and setters are not synchronised. In scenarios where options are mutated after the server has started (e.g., live reload), the consuming components must handle atomic reads or snapshot the values. Concurrent reads from multiple threads are safe provided no thread is simultaneously writing.
- **Binding behaviour**: When bound from configuration, missing sections result in properties remaining at their default values (typically `null`). Partial binding is supported—keys present in configuration overwrite only the corresponding properties, leaving others untouched.
- **Immutability expectation**: Once passed to the server initialisation, the options instance is generally treated as immutable for the lifetime of the process. Changing properties on a running server has undefined behaviour unless the specific subsystem explicitly supports dynamic reconfiguration.
