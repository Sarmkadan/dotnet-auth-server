# RateLimitingMiddleware

A middleware component that enforces rate limiting using a token bucket algorithm to control the frequency of incoming requests. It is designed to protect backend services from being overwhelmed by excessive traffic while allowing bursts of activity within configured limits.

## API

### `public RateLimitingMiddleware`

Initializes a new instance of the `RateLimitingMiddleware` class with the specified token bucket.

**Parameters**
- `bucket`: The `TokenBucket` instance used to track and enforce rate limits.

**Remarks**
- The middleware is initialized with a pre-configured `TokenBucket` that defines the rate limit rules (e.g., capacity and refill rate).
- The `TokenBucket` instance should be shared and thread-safe if used across multiple middleware instances.

---

### `public async Task InvokeAsync(HttpContext context, RequestDelegate next)`

Invokes the middleware to process the HTTP request and enforce rate limiting.

**Parameters**
- `context`: The `HttpContext` for the current HTTP request.
- `next`: The delegate representing the next middleware in the pipeline.

**Return Value**
- A `Task` representing the asynchronous operation.

**Exceptions**
- Throws `ArgumentNullException` if `context` or `next` is `null`.
- Throws `InvalidOperationException` if the `TokenBucket` is exhausted and the request should be rejected.

**Remarks**
- If the token bucket has available tokens, the request is allowed to proceed by invoking `next`.
- If the token bucket is exhausted, the middleware responds with HTTP 429 (Too Many Requests) and does not invoke the next middleware.
- The operation is asynchronous to avoid blocking the request pipeline.

---

### `public TokenBucket`

Gets the `TokenBucket` instance used by this middleware for rate limiting.

**Return Value**
- The `TokenBucket` instance configured for this middleware.

**Remarks**
- The returned `TokenBucket` is shared across all instances of this middleware if the same instance is passed during construction.
- Modifications to the bucket's state (e.g., capacity or refill rate) after construction are not supported and may lead to undefined behavior.

---
### `public bool TryConsumeToken()`

Attempts to consume a token from the rate-limiting bucket.

**Return Value**
- `true` if a token was successfully consumed; otherwise, `false`.

**Remarks**
- This method is thread-safe and can be called concurrently from multiple threads.
- The operation is non-blocking and returns immediately with the result.
- Use this method to manually check or enforce rate limits outside the middleware context (e.g., in a custom rate-limiting service).

## Usage

### Example 1: Basic Middleware Registration
