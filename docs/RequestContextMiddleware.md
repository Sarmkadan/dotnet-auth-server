# RequestContextMiddleware

`RequestContextMiddleware` is an ASP.NET Core middleware component responsible for establishing and managing per-request diagnostic contexts. It creates a scoped logging context at the start of each HTTP request, enriches it with request-specific metadata, and ensures proper cleanup when the request pipeline completes. The middleware implements `IDisposable` to release any resources held by the logging scope.

## API

### RequestContextMiddleware

```csharp
public RequestContextMiddleware(RequestDelegate next)
```

Constructs the middleware with a reference to the next delegate in the request pipeline.

**Parameters:**
- `next` — The `RequestDelegate` representing the subsequent middleware or terminal handler. Must not be null.

**Exceptions:**
- `ArgumentNullException` — Thrown when `next` is null.

---

### InvokeAsync

```csharp
public async Task InvokeAsync(HttpContext context)
```

Invoked by the ASP.NET Core runtime for each HTTP request. Creates a `LoggingScope` that wraps the execution of the downstream pipeline, making request-scoped state available to loggers and other diagnostics infrastructure for the duration of the request.

**Parameters:**
- `context` — The `HttpContext` for the current request. Must not be null.

**Return value:**
- A `Task` that completes when the downstream pipeline has finished executing and the logging scope has been disposed.

**Exceptions:**
- `ArgumentNullException` — Thrown when `context` is null.
- Exceptions thrown by the downstream pipeline propagate unmodified after the logging scope is cleaned up.

---

### LoggingScope

```csharp
public LoggingScope { get; }
```

Gets the current request-scoped logging scope instance. This property provides access to the scope object that was created during `InvokeAsync` and remains valid for the lifetime of the request. Consumers can use this scope to attach additional contextual data to log entries emitted during request processing.

**Return value:**
- The active `LoggingScope` instance, or null if accessed outside of a request context.

---

### Dispose

```csharp
public void Dispose()
```

Releases all resources held by the middleware, including any lingering logging scope references. Called by the ASP.NET Core infrastructure when the application shuts down or when the middleware instance is no longer needed.

## Usage

### Example 1: Basic Registration in Pipeline

```csharp
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseMiddleware<RequestContextMiddleware>();

app.MapGet("/", async (HttpContext context, RequestContextMiddleware middleware) =>
{
    var scope = middleware.LoggingScope;
    scope?.SetProperty("UserId", context.User?.Identity?.Name);
    // Request processing logic
});

app.Run();
```

### Example 2: Accessing Scope in a Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly RequestContextMiddleware _middleware;

    public OrdersController(RequestContextMiddleware middleware)
    {
        _middleware = middleware;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetOrder(int id)
    {
        _middleware.LoggingScope?.SetProperty("OrderId", id.ToString());
        // Order retrieval logic
        return Ok(order);
    }
}
```

## Notes

- **Scope lifetime:** The `LoggingScope` is created at the start of `InvokeAsync` and disposed after the downstream pipeline completes, even if an exception occurs. Accessing `LoggingScope` outside of a request context returns null.
- **Thread safety:** `InvokeAsync` is called concurrently for different requests. The `LoggingScope` property reflects the scope for the currently executing request on the same call context. No synchronization is required for per-request access, but the property itself is not thread-safe for cross-request access.
- **Exception propagation:** Exceptions from downstream middleware are not swallowed. The logging scope is cleaned up in a finally block before rethrowing.
- **Disposal:** `Dispose` is invoked once per middleware instance during application shutdown. It does not affect in-flight requests, which manage their own scope lifecycle independently.
