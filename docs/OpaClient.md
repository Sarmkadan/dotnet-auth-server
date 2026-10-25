# OpaClient

**Namespace:** `DotnetAuthServer.Integration`

HTTP client for the Open Policy Agent (OPA) Data API. It posts a policy query for a `ClaimsPrincipal` to OPA and maps the boolean result to an allow or deny decision. The client keeps no mutable state. Its only dependencies are the constructor arguments, which it stores in readonly fields.

`sealed`. It is registered only when `DotnetAuthServer:Opa:Enabled` is true, as a typed client (`AddHttpClient<OpaClient>()` in `Program.cs`). `PolicyEnforcementService` is its consumer. The options come from [`OpaOptions`](OpaOptions.md).

---

## Constructor

### `OpaClient(HttpClient http, OpaOptions options, ILogger<OpaClient> logger)`

Stores the dependencies. Throws `ArgumentNullException` if any argument is `null`.

| Parameter | Type | Description |
|---|---|---|
| `http` | `HttpClient` | Sends the request to OPA. In DI it comes from the HTTP client factory. |
| `options` | `OpaOptions` | Supplies `BaseUrl`, `PolicyPath`, `TimeoutSeconds` and `FailClosedOnError`. |
| `logger` | `ILogger<OpaClient>` | Records warnings and errors for failed evaluations. |

The DI registration also sets the `HttpClient`'s `BaseAddress` and `Timeout` from `OpaOptions`. `EvaluatePolicyAsync` does not use `BaseAddress`, because it builds an absolute URL from `BaseUrl`. The class applies its own timeout as well (see [Timeout and cancellation](#timeout-and-cancellation)).

---

## Methods

### `Task<bool?> EvaluatePolicyAsync(string policyName, ClaimsPrincipal principal, CancellationToken cancellationToken = default)`

Evaluates the policy named `policyName` for `principal`. The method does not throw for OPA failures. Those are converted to a result (see below).

| Parameter | Type | Description |
|---|---|---|
| `policyName` | `string` | Rule name appended to the policy path, for example `"allow_admin"`. Must not be null, empty or whitespace. |
| `principal` | `ClaimsPrincipal` | The authenticated caller. Must not be `null`. |
| `cancellationToken` | `CancellationToken` | Linked with the timeout (see below). |

**Returns:** a `bool?` with three meanings:

| Value | Meaning | Caller action |
|---|---|---|
| `true` | OPA allowed the request. | Allow. |
| `false` | OPA denied the request, or OPA failed and `FailClosedOnError` is `true`. | Deny. |
| `null` | OPA failed and `FailClosedOnError` is `false`. | Fall back to the built-in evaluator. |

**Throws:**

| Exception | When |
|---|---|
| `ArgumentException` | `policyName` is `null`, empty or whitespace. |
| `ArgumentNullException` | `principal` is `null`. |

These checks run before any request is sent. They are not converted to a fallback result.

**Behavior:**

1. Validates the arguments.
2. Builds the OPA input from the principal:

   | Input field | Source |
   |---|---|
   | `subject` | Value of the `sub` claim, or `""` if absent. |
   | `roles` | Values of every `ClaimTypes.Role` claim, in order. Empty list if none. |
   | `scopes` | The `scope` claim split on spaces, with empty entries removed. Empty list if absent. |
   | `claims` | Every claim as type → value. If a type occurs more than once, only the first value is kept. Role claims also appear here, but only the first one. |

3. Builds the URL `{BaseUrl without trailing '/'}/v1/data/{PolicyPath}/{policyName}`. `PolicyPath` may contain slashes for nested packages. Neither `PolicyPath` nor `policyName` is URL-encoded.
4. Creates a linked cancellation token that cancels after `TimeoutSeconds` or when `cancellationToken` is cancelled.
5. POSTs the body `{ "input": { ... } }` as JSON.
6. If the response status is not 2xx, logs a warning with the status code and policy name, then returns the failure result.
7. Otherwise reads the body as `{ "result": <bool> }` and returns the value. If the `result` property is missing, the method returns `false`. A body of `null` also returns `false`.

**Failure handling:**

| Condition | Log level | Returns |
|---|---|---|
| Non-2xx HTTP status | Warning | `FailClosedOnError ? false : null` |
| `OperationCanceledException` (timeout or caller cancellation) | Warning | `FailClosedOnError ? false : null` |
| `HttpRequestException` (connection failure) | Error | `FailClosedOnError ? false : null` |
| Any other exception, such as a response body whose `result` is not a boolean | Error | `FailClosedOnError ? false : null` |

Logs contain the policy name and, for HTTP errors, the status code. They do not contain the principal's claims or the input document.

**Example:**

```csharp
var client = scope.ServiceProvider.GetRequiredService<OpaClient>();

bool? decision = await client.EvaluatePolicyAsync("allow_admin", httpContext.User, cancellationToken);

if (decision is null)
{
    // OPA is unavailable and FailClosedOnError is false: use the built-in evaluator.
}
else if (decision.Value)
{
    // Allowed.
}
```

With a principal that has `sub` = `user-123`, role `admin`, and scope `read:articles write:articles`, the request body is:

```json
{
  "input": {
    "subject": "user-123",
    "roles": ["admin"],
    "scopes": ["read:articles", "write:articles"],
    "claims": {
      "sub": "user-123",
      "scope": "read:articles write:articles",
      "http://schemas.microsoft.com/ws/2008/06/identity/claims/role": "admin"
    }
  }
}
```

A matching Rego rule for this request:

```rego
package authz

import rego.v1

default allow_admin := false

allow_admin if "admin" in input.roles
```

---

## Timeout and cancellation

The request timeout is `OpaOptions.TimeoutSeconds`, applied through a linked cancellation token. The same token covers the POST and reading the response body.

The `catch (OperationCanceledException)` block does not distinguish the two causes. If the caller cancels `cancellationToken`, the method does not rethrow. It logs "OPA request timed out", then returns the failure result (`false` or `null`). Callers that need to stop on cancellation must check their own token after the call.

---

## Notes

- **Undefined policies deny.** When the rule is not defined, OPA returns a response without a `result` property. The method returns `false`, not `null`, so the built-in fallback is not used even when `FailClosedOnError` is `false`.
- **No credentials are sent.** The client adds no headers. If the OPA server requires authentication, the `HttpClient` must be configured for it. The current DI registration does not do this.
- **No caching or retries.** Each call makes one request.
- **Synchronous callers block.** `PolicyEnforcementService.EvaluatePolicy` calls this method and waits with `GetAwaiter().GetResult()`. Prefer `EvaluatePolicyAsync` on the service.
- **Thread safety.** The class has no mutable state, so concurrent calls are safe as long as the `HttpClient` and `OpaOptions` are safe to share. A typed `HttpClient` is.
