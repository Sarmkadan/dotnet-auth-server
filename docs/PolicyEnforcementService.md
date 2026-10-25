# PolicyEnforcementService

`PolicyEnforcementService` evaluates RBAC (role-based) and ABAC (attribute-based) policies against a `ClaimsPrincipal`. It answers one question: does this principal satisfy this policy? When OPA integration is enabled, the decision is delegated to Open Policy Agent. Otherwise the built-in evaluator decides.

The class is `public sealed` and lives in `src/Services/PolicyEnforcementService.cs`, namespace `DotnetAuthServer.Services`. It has no interface and is registered as a concrete type. The same file defines the public policy model: `Policy`, `PolicyRule`, `PolicyCombineMode`, `PolicyRuleType`, and `PolicyMatchMode`.

Validation helpers for this class live in `PolicyEnforcementServiceValidation.cs` and are documented separately.

## Role in the system

- Holds named policies in memory, keyed by name.
- Evaluates a named policy or a policy object against a principal.
- Delegates to OPA (`OpaClient`) when `OpaOptions.Enabled` is `true`, with a fallback to the built-in evaluator.
- Registers two default policies, `AdminOnly` and `ConsentRequired`, on construction.

Logging goes through `ILogger<PolicyEnforcementService>`. The service logs an information event when a policy is registered and a warning when a named policy is not found. It does not log principals or claim values.

## Constructors

### `public PolicyEnforcementService(ILogger<PolicyEnforcementService> logger)`

Creates a service that uses only the built-in evaluator. Registers the default policies.

**Parameters:**
- `logger` – logger for registration and lookup events.

### `public PolicyEnforcementService(ILogger<PolicyEnforcementService> logger, OpaClient opaClient, OpaOptions opaOptions)`

Creates a service that can delegate to OPA. Registers the default policies.

**Parameters:**
- `logger` – logger for registration and lookup events.
- `opaClient` – client for the OPA REST API. Used only when `opaOptions.Enabled` is `true`.
- `opaOptions` – OPA settings. `Enabled` turns delegation on. `FailClosedOnError` controls what happens when OPA is unreachable (see [OPA delegation](#opa-delegation)).

## Methods

### `public void RegisterPolicy(string? policyName, Policy policy)`

Adds a policy under `policyName`. If a policy with that name already exists, it is replaced. Logs an information event.

**Parameters:**
- `policyName` – the lookup key. Must not be `null`.
- `policy` – the policy to store.

**Exceptions:** `ArgumentNullException` if `policyName` is `null`.

### `public bool EvaluatePolicy(string? policyName, ClaimsPrincipal? principal)`

Synchronous evaluation of a named policy.

Order of evaluation:
1. If OPA is enabled, call `OpaClient.EvaluatePolicyAsync` and block on it with `GetAwaiter().GetResult()`. If OPA returns a value, that value is the result.
2. If OPA returned no value (unreachable and `FailClosedOnError` is `false`), fall through to the built-in evaluator.
3. Look up `policyName`. If it is not registered, log a warning and return `false`.
4. Evaluate the stored policy with the overload below.

Because step 1 blocks the calling thread, use `EvaluatePolicyAsync` in async code.

**Parameters:**
- `policyName` – name of a registered policy. Must not be `null`.
- `principal` – the caller. Must not be `null`.

**Return value:** `true` if the principal satisfies the policy, otherwise `false`.

**Exceptions:** `ArgumentNullException` if `policyName` or `principal` is `null`.

### `public async Task<bool> EvaluatePolicyAsync(string policyName, ClaimsPrincipal principal, CancellationToken cancellationToken = default)`

Asynchronous version of the named-policy evaluation. It follows the same order as `EvaluatePolicy(string?, ClaimsPrincipal?)`, but awaits the OPA call instead of blocking.

**Parameters:**
- `policyName` – name of a registered policy.
- `principal` – the caller.
- `cancellationToken` – passed to the OPA call. Default `default`. The built-in evaluator does not observe it.

**Return value:** `true` if the principal satisfies the policy, otherwise `false`.

**Exceptions:**
- `OperationCanceledException` if `cancellationToken` is cancelled during the OPA call.
- No `ArgumentNullException` check is made in this method. Passing `null` for `principal` reaches the evaluator and can throw a `NullReferenceException`. Callers should pass non-null arguments.

### `public bool EvaluatePolicy(Policy policy, ClaimsPrincipal? principal)`

Evaluates a policy object directly. OPA is not consulted, and the policy is not looked up by name.

Each rule is evaluated to a boolean. The results are then combined using `Policy.CombineWith`:
- `PolicyCombineMode.All` – `true` only if every rule is `true`.
- `PolicyCombineMode.Any` – `true` if at least one rule is `true`.

**Parameters:**
- `policy` – the policy to evaluate.
- `principal` – the caller.

**Return value:** `true` if the principal satisfies the policy. Returns `false` in these cases:
- `policy` or `principal` is `null`.
- `policy.Rules` is empty. A policy with no rules never grants access.
- `CombineWith` has an unknown value.

### `public void InitializeDefaultPolicies()`

Registers the built-in policies. Each call overwrites any existing `AdminOnly` or `ConsentRequired` policy with the default definition. The constructors call it once.

| Name | Rules | `CombineWith` |
|---|---|---|
| `AdminOnly` | One `Role` rule, `Values = ["admin"]`, `Match = Any` | `All` |
| `ConsentRequired` | One `Scope` rule, `Values = ["consent"]`, `Match = Any` | `All` |

### `public override string ToString()`

Returns a fixed string, not a dump of the instance state. The output is:

```
PolicyEnforcementService { Rules = , CombineWith = All, Type = Role, Attribute = , Values = , Match = Any }
```

Do not rely on it for diagnostics. It does not reflect the registered policies or the OPA configuration.

## Rule evaluation

Each `PolicyRule` is evaluated according to its `Type`. Unknown rule types evaluate to `false`.

### `Role`

Reads all `ClaimTypes.Role` claims from the principal and compares them with `Values`. `Match` is applied to the comparison:

| `Match` | Result is `true` when |
|---|---|
| `Any` | the principal has at least one of `Values` |
| `All` | the principal has every value in `Values` |
| `None` | the principal has none of `Values` |

`Attribute` is ignored for this rule type.

### `Scope`

Reads the `scope` claim, splits it on spaces (empty entries removed), and compares the result with `Values`. `Match` works as in the `Role` table. A missing `scope` claim is treated as an empty list.

### `Attribute` and `Claim`

Both types run the same logic. They read the first claim whose type equals `Attribute`. `Attribute` must not be `null`, otherwise `ArgumentNullException` is thrown.

- If the claim is missing or its value is empty, the result is `true` only for `Match = None`.
- `Match = Any` – `true` if the claim value is in `Values`.
- `Match = None` – `true` if the claim value is not in `Values`.
- `Match = All` – always `true` when the claim is present. A single claim value cannot match every entry in `Values`, so this mode is effectively permissive. Avoid it. Use `Any` or `None` instead.

## OPA delegation

OPA is used only when both conditions hold:
- The service was constructed with the OPA constructor, so `OpaClient` is not `null`.
- `OpaOptions.Enabled` is `true`.

`OpaClient.EvaluatePolicyAsync` returns a `bool?`:
- A value is an OPA decision. It is returned as-is.
- `null` means OPA was unreachable or failed, and `FailClosedOnError` is `false`. The built-in evaluator runs instead.
- With `FailClosedOnError` set to `true`, the client returns `false` instead of `null`. Access is denied without falling back.

`OpaOptions.FailClosedOnError` defaults to `false`. `OpaOptions.Enabled` defaults to `false`.

The OPA path only applies to the named-policy methods. `EvaluatePolicy(Policy, ClaimsPrincipal?)` never calls OPA.

## Policy model

### `public sealed class Policy`

A set of rules and how their results combine.

| Property | Type | Default | Description |
|---|---|---|---|
| `Rules` | `List<PolicyRule>` | empty list | Rules that make up the policy. |
| `CombineWith` | `PolicyCombineMode` | `All` | How rule results are combined. |

### `public sealed class PolicyRule`

One condition to check.

| Property | Type | Default | Description |
|---|---|---|---|
| `Type` | `PolicyRuleType` | `Role` (first enum value) | Kind of rule. |
| `Attribute` | `string?` | `null` | Claim or attribute name. Required for `Attribute` and `Claim` rules. |
| `Values` | `List<string>` | empty list | Values to compare against. |
| `Match` | `PolicyMatchMode` | `Any` | How `Values` are matched. |

### `public enum PolicyCombineMode`

| Member | Meaning |
|---|---|
| `All` | Every rule must be satisfied. Default. |
| `Any` | At least one rule must be satisfied. |

### `public enum PolicyRuleType`

| Member | Meaning |
|---|---|
| `Role` | Checks `ClaimTypes.Role` claims. |
| `Attribute` | Checks the claim named by `Attribute`. |
| `Scope` | Checks the space-separated `scope` claim. |
| `Claim` | Checks the claim named by `Attribute`. Same behavior as `Attribute`. |

### `public enum PolicyMatchMode`

| Member | Meaning |
|---|---|
| `Any` | At least one of `Values` is present. Default. |
| `All` | Every entry in `Values` is present. |
| `None` | None of `Values` is present. |

## Examples

Register a policy that requires the `billing` role and the `invoices.write` scope:

```csharp
var policy = new Policy
{
    CombineWith = PolicyCombineMode.All,
    Rules =
    [
        new PolicyRule { Type = PolicyRuleType.Role, Values = ["billing"], Match = PolicyMatchMode.Any },
        new PolicyRule { Type = PolicyRuleType.Scope, Values = ["invoices.write"], Match = PolicyMatchMode.Any }
    ]
};

policyService.RegisterPolicy("InvoiceWriter", policy);

bool allowed = await policyService.EvaluatePolicyAsync("InvoiceWriter", httpContext.User, cancellationToken);
```

Deny access to a principal whose `department` claim is `contractor`:

```csharp
var policy = new Policy
{
    Rules =
    [
        new PolicyRule
        {
            Type = PolicyRuleType.Attribute,
            Attribute = "department",
            Values = ["contractor"],
            Match = PolicyMatchMode.None
        }
    ]
};
```

## Thread safety

Policies are stored in a plain `Dictionary<string, Policy>`, not a concurrent collection. `RegisterPolicy` and `InitializeDefaultPolicies` write to it. Register policies at startup, before concurrent evaluation begins. Concurrent writes during evaluation are not safe.

## Known limitations

- Policies live in memory only. They are lost on restart and are not shared between instances.
- The synchronous `EvaluatePolicy(string?, ClaimsPrincipal?)` blocks a thread on OPA calls.
- `Match = All` on `Attribute` and `Claim` rules always passes when the claim is present. See the rule evaluation section.
- `ToString` returns a fixed string, not instance data.
