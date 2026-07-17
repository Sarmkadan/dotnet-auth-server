# PolicyEnforcementServiceValidation

Provides static methods to validate the configuration and runtime state of the policy enforcement service. Each overload targets a distinct aspect of the enforcement pipeline, returning structured error lists, boolean validity indicators, and precondition enforcement that throws on failure.

## API

### `Validate` (overload 1)
```csharp
public static IReadOnlyList<string> Validate(PolicyEnforcementOptions options)
```
Validates the supplied `PolicyEnforcementOptions` for completeness and internal consistency.  
**Parameters:**  
- `options` — The configuration object to inspect.  

**Returns:** A read-only list of error messages. An empty list indicates valid configuration.  
**Throws:** `ArgumentNullException` if `options` is `null`.

---

### `IsValid` (overload 1)
```csharp
public static bool IsValid(PolicyEnforcementOptions options)
```
Convenience wrapper that returns `true` when `Validate(options)` produces zero errors.  
**Parameters:**  
- `options` — The configuration object to inspect.  

**Returns:** `true` if the configuration is valid; otherwise `false`.  
**Throws:** `ArgumentNullException` if `options` is `null`.

---

### `EnsureValid` (overload 1)
```csharp
public static void EnsureValid(PolicyEnforcementOptions options)
```
Invokes `Validate(options)` and throws an `InvalidOperationException` containing all error messages if any are present.  
**Parameters:**  
- `options` — The configuration object to inspect.  

**Throws:**  
- `ArgumentNullException` if `options` is `null`.  
- `InvalidOperationException` if validation errors exist.

---

### `Validate` (overload 2)
```csharp
public static IReadOnlyList<string> Validate(PolicyScope scope)
```
Validates the structure and constraints of a `PolicyScope` definition.  
**Parameters:**  
- `scope` — The scope to inspect.  

**Returns:** A read-only list of error messages. An empty list indicates a valid scope.  
**Throws:** `ArgumentNullException` if `scope` is `null`.

---

### `IsValid` (overload 2)
```csharp
public static bool IsValid(PolicyScope scope)
```
Returns `true` when `Validate(scope)` produces zero errors.  
**Parameters:**  
- `scope` — The scope to inspect.  

**Returns:** `true` if the scope is valid; otherwise `false`.  
**Throws:** `ArgumentNullException` if `scope` is `null`.

---

### `EnsureValid` (overload 2)
```csharp
public static void EnsureValid(PolicyScope scope)
```
Invokes `Validate(scope)` and throws an `InvalidOperationException` containing all error messages if any are present.  
**Parameters:**  
- `scope` — The scope to inspect.  

**Throws:**  
- `ArgumentNullException` if `scope` is `null`.  
- `InvalidOperationException` if validation errors exist.

---

### `Validate` (overload 3)
```csharp
public static IReadOnlyList<string> Validate(PolicyEnforcementContext context)
```
Validates a runtime `PolicyEnforcementContext` before enforcement execution, checking required claims, scopes, and resource identifiers.  
**Parameters:**  
- `context` — The enforcement context to inspect.  

**Returns:** A read-only list of error messages. An empty list indicates a valid context.  
**Throws:** `ArgumentNullException` if `context` is `null`.

---

### `IsValid` (overload 3)
```csharp
public static bool IsValid(PolicyEnforcementContext context)
```
Returns `true` when `Validate(context)` produces zero errors.  
**Parameters:**  
- `context` — The enforcement context to inspect.  

**Returns:** `true` if the context is valid; otherwise `false`.  
**Throws:** `ArgumentNullException` if `context` is `null`.

---

### `EnsureValid` (overload 3)
```csharp
public static void EnsureValid(PolicyEnforcementContext context)
```
Invokes `Validate(context)` and throws an `InvalidOperationException` containing all error messages if any are present.  
**Parameters:**  
- `context` — The enforcement context to inspect.  

**Throws:**  
- `ArgumentNullException` if `context` is `null`.  
- `InvalidOperationException` if validation errors exist.

---

### `Validate` (overload 4)
```csharp
public static IReadOnlyList<string> Validate(PolicyEnforcementResult result)
```
Validates the integrity of a produced `PolicyEnforcementResult`, ensuring required fields are populated and status codes are consistent with the decision.  
**Parameters:**  
- `result` — The enforcement result to inspect.  

**Returns:** A read-only list of error messages. An empty list indicates a valid result.  
**Throws:** `ArgumentNullException` if `result` is `null`.

---

### `IsValid` (overload 4)
```csharp
public static bool IsValid(PolicyEnforcementResult result)
```
Returns `true` when `Validate(result)` produces zero errors.  
**Parameters:**  
- `result` — The enforcement result to inspect.  

**Returns:** `true` if the result is valid; otherwise `false`.  
**Throws:** `ArgumentNullException` if `result` is `null`.

---

### `EnsureValid` (overload 4)
```csharp
public static void EnsureValid(PolicyEnforcementResult result)
```
Invokes `Validate(result)` and throws an `InvalidOperationException` containing all error messages if any are present.  
**Parameters:**  
- `result` — The enforcement result to inspect.  

**Throws:**  
- `ArgumentNullException` if `result` is `null`.  
- `InvalidOperationException` if validation errors exist.

---

## Usage

### Example 1: Validating configuration at startup
```csharp
var options = new PolicyEnforcementOptions
{
    DefaultPolicy = "RequireMfa",
    CacheDuration = TimeSpan.FromMinutes(5)
};

if (!PolicyEnforcementServiceValidation.IsValid(options))
{
    var errors = PolicyEnforcementServiceValidation.Validate(options);
    foreach (var error in errors)
    {
        logger.LogError("Policy enforcement configuration error: {Error}", error);
    }
    throw new ApplicationException("Invalid policy enforcement configuration.");
}

// Proceed with service registration
services.AddPolicyEnforcement(options);
```

### Example 2: Precondition check before enforcement
```csharp
public async Task<PolicyEnforcementResult> EnforceAsync(PolicyEnforcementContext context)
{
    PolicyEnforcementServiceValidation.EnsureValid(context);

    var result = await enforcementEngine.EvaluateAsync(context);

    PolicyEnforcementServiceValidation.EnsureValid(result);

    return result;
}
```

## Notes

- All methods are static and stateless; they are safe to call concurrently from multiple threads without external synchronization.
- The `Validate` overloads never throw for validation failures—they accumulate and return error messages. Use `EnsureValid` when an immediate hard stop is required.
- `EnsureValid` wraps `Validate` and throws `InvalidOperationException` with a joined message. Callers should catch this exception at boundaries where graceful degradation is possible.
- Each overload pair (`Validate`/`IsValid`/`EnsureValid`) is specialized for a single type. Passing an object of an unexpected type will not compile; there is no base-type or interface overload.
- Input parameters are guarded against `null` with `ArgumentNullException` across all overloads. Validation logic itself does not mutate the supplied objects.
- The returned `IReadOnlyList<string>` from `Validate` is always non-null; an empty list signals validity. Callers should avoid treating a `null` return as a valid state.
