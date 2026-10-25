# RateLimitingOptions

Configuration class for `RateLimitingMiddleware`. It sets the sustained request rate, the burst capacity of the token bucket, and the set of URL paths that are subject to rate limiting.

The class is declared in `src/Middleware/RateLimitingOptions.cs`. It has no namespace declaration, so it lives in the global namespace. The middleware itself (`DotnetAuthServer.Middleware.RateLimitingMiddleware`) declares and uses its own `DotnetAuthServer.Middleware.RateLimitingOptions` type in `src/Middleware/RateLimitingMiddleware.cs`. The two types are separate, and the middleware does not read the global one. See [Remarks](#remarks).

## API

### `RequestsPerMinute`

Gets or sets the sustained number of requests allowed per minute for each client identifier. The default is `60`.

The middleware converts this value to a refill rate of `RequestsPerMinute / 60` tokens per second.

### `BurstSize`

Gets or sets the maximum number of requests a client can make in a burst. This is the capacity of the token bucket, and a new bucket starts full. The default is `10`.

### `SensitiveEndpoints`

Gets or sets the set of path prefixes that are rate limited. Requests whose path starts with one of these values (case-insensitive) are checked against the token bucket. Requests to any other path pass through without a check.

The default set is compared case-insensitively and contains:

- `/oauth/token`
- `/oauth/authorize`
- `/oauth/introspect`
- `/oauth/revoke`

---

## Defaults

| Property | Default |
|---|---|
| `RequestsPerMinute` | `60` |
| `BurstSize` | `10` |
| `SensitiveEndpoints` | `/oauth/token`, `/oauth/authorize`, `/oauth/introspect`, `/oauth/revoke` (case-insensitive) |

The middleware also hard-codes the same fallback values. If it is constructed with `options: null`, it uses `60`, `10`, and the same four paths, and it logs a warning at startup.

---

## Usage in `RateLimitingMiddleware`

The middleware reads the options in its constructor and copies the values into private fields:

- `RequestsPerMinute` and `BurstSize` are used to create one `TokenBucket` per client key. Buckets are stored in a `ConcurrentDictionary<string, TokenBucket>`.
- `SensitiveEndpoints` is used in `InvokeAsync`. A request is limited only when its path starts with one of these entries.

For each sensitive request, `InvokeAsync` does the following:

1. Derives a client key with `ExtractClientIdentifier`. The key is the first 20 characters of the `Authorization` header, if present. Otherwise it is the `client_id` query parameter. Otherwise it is the remote IP address, or `"unknown"` if no address is available.
2. Calls `TryConsumeToken()` on the bucket for that key. If a token is available, the request continues down the pipeline.
3. If no token is available, responds with HTTP `429 Too Many Requests`, sets `Retry-After: 60`, writes the body `{"error":"rate_limit_exceeded"}`, and does not call the next middleware.

Requests whose paths are not in `SensitiveEndpoints` are never checked and are never rejected by this middleware.

### Registration

`Program.cs` registers the middleware with:

```csharp
app.UseMiddleware<RateLimitingMiddleware>();
```

No `RateLimitingOptions` instance is registered in dependency injection in `Program.cs`. As a result, the middleware receives `null` and uses its built-in defaults. To change the values, the middleware must be constructed with a populated options object, or `Program.cs` must register one and pass it through.

---

## Remarks

- **Duplicate type name.** The global `RateLimitingOptions` in `src/Middleware/RateLimitingOptions.cs` is not referenced by `RateLimitingMiddleware`. The middleware's `DotnetAuthServer.Middleware.RateLimitingOptions` is the one it uses. The two types have the same defaults, but changing one does not change the other.
- **No validation.** The class does not validate its values. A `BurstSize` of `0` gives a bucket capacity of zero, so every sensitive request is rejected. A `RequestsPerMinute` of `0` gives a refill rate of zero, so each client gets only its initial `BurstSize` requests and the bucket is never refilled.
- **Fixed `Retry-After`.** The `Retry-After` header is always `60` seconds, regardless of `RequestsPerMinute`.
- **In-memory state.** Buckets are kept in process memory and are never evicted. They are not shared across server instances.
- **Path matching is by prefix.** An entry such as `/oauth/token` also matches `/oauth/token/anything`.
