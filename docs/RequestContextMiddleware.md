# RequestContextMiddleware

**Namespace:** `DotnetAuthServer.Middleware`

Assigns every request a correlation ID and makes that ID available to the rest of the pipeline. If the client sends an `X-Request-Id` header, its value is reused. Otherwise a new GUID is generated. The ID is stored in `HttpContext.Items`, echoed in the `X-Request-Id` response header, and published through `LogicalContext.RequestId` for the duration of the request so that logging and audit code can include it.

Registered in `Program.cs` with `app.UseMiddleware<RequestContextMiddleware>();`. It should run before `LoggingMiddleware`, which reads the ID from `LogicalContext`.

`sealed`. Stateless apart from the `RequestDelegate` it receives in its constructor.

---

## Constructor

### `RequestContextMiddleware(RequestDelegate next)`

Stores the next delegate in the pipeline. The constructor performs no null check.

| Parameter | Type | Description |
|---|---|---|
| `next` | `RequestDelegate` | The next middleware or terminal handler. ASP.NET Core supplies it when the middleware is registered. |

---

## Methods

### `Task InvokeAsync(HttpContext context)`

Runs once per request. It determines the request ID, publishes it, and then awaits the rest of the pipeline inside a logging scope.

| Parameter | Type | Description |
|---|---|---|
| `context` | `HttpContext` | The current request. Its `Items` and response headers are updated. |

**Returns:** A `Task` that completes when the downstream pipeline completes.

**Behavior:**

1. **Request ID selection.** If the request has an `X-Request-Id` header (the lookup is case-insensitive), its value is used via `ToString()`. If the header is repeated, the values are joined with commas. Otherwise `Guid.NewGuid().ToString("N")` is used, which gives 32 lowercase hex characters with no hyphens.
2. **Storage.** The ID is stored as a `string` at `context.Items["RequestId"]`.
3. **Response header.** `X-Request-Id` is set on the response before the downstream pipeline runs, so the header is present even when a later middleware short-circuits the request.
4. **Logging scope.** The downstream pipeline is awaited inside a private scope object. The scope sets `LogicalContext.RequestId` to the ID when it is created, and sets it back to `null` when it is disposed.

The method does not catch exceptions. An exception from the downstream pipeline propagates unchanged, and the scope is still disposed (so `LogicalContext.RequestId` is cleared) because the `using` block runs on both normal completion and failure.

**Example:**

```csharp
// Request: GET /api/token  with header  X-Request-Id: abc-123
// Response: X-Request-Id: abc-123
// HttpContext.Items["RequestId"] == "abc-123"

// Request without the header
// Response: X-Request-Id: 3f2c9d1e8a7b4c60915e2f0d6a1b8c47   (32 hex characters)
```

---

## Public Types in the Same File

### `LogicalContext`

**Namespace:** `DotnetAuthServer.Middleware`

A `static` class that holds request-scoped data for the current async flow. It is declared in `RequestContextMiddleware.cs`, but it is a separate public type and can be used from anywhere in the application.

### `LogicalContext.RequestId`

```csharp
public static string? RequestId { get; set; }
```

The request ID of the current async flow, backed by an `AsyncLocal<string?>`.

| Accessor | Behavior |
|---|---|
| `get` | Returns the ID set by `RequestContextMiddleware`. Returns `null` outside a request, or after the middleware's scope has been disposed. |
| `set` | Sets the value for the current async flow. Code outside the middleware can set it, but the middleware overwrites it on the next request in the same flow and clears it when that request ends. |

**Remarks**

- Each async flow has its own copy. Concurrent requests do not see each other's values, and the value flows into `await` continuations within the request.
- The scope does not restore the previous value. It sets `null` on exit, so a value set before the middleware ran is lost once the request finishes.
- Consumers that need a fallback should use `LogicalContext.RequestId ?? "unknown"`. `LoggingMiddleware` does this.

**Consumers in this codebase:** `LoggingMiddleware` (log scope) and `AuditLoggingService` (audit records).

---

## Notes

- **Trust of inbound IDs.** The incoming `X-Request-Id` value is used without validation or a length limit. Whatever a client sends appears in the response header, in `Items`, and in logs and audit records. Treat the ID as a correlation hint, not as trusted data.
- **Empty header.** If `X-Request-Id` is present with an empty value, the empty string is used as the ID. The middleware does not fall back to a generated GUID in that case.
- **Lifetime.** The middleware holds no per-request state, so a single instance serves all requests concurrently.
- **Not a logger.** The middleware does not write log entries. It only publishes the ID for other components to read.
