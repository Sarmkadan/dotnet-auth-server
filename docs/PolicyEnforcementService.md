# PolicyEnforcementService

Central service for enforcing authorization policies by evaluating registered rules against contextual attributes and values. The service aggregates multiple policy rules according to a specified combine mode and supports both synchronous and asynchronous evaluation.

## API

### `PolicyEnforcementService()`
Default constructor. Initializes a new instance with an empty rule set, `PolicyCombineMode.All` as the default combine mode, and no attribute or type restrictions.

### `PolicyEnforcementService(IEnumerable<PolicyRule> rules)`
Constructor accepting an initial collection of policy rules. The rules are copied into an internal list. The combine mode defaults to `PolicyCombineMode.All`.

* **rules**: Enumerable of `PolicyRule` instances to register at initialization.

### `void RegisterPolicy(PolicyRule rule)`
Adds a policy rule to the enforcement service. The rule is appended to the internal list of rules. No validation is performed on the rule at registration time.

* **rule**: The `PolicyRule` to register. Must not be `null`.

### `bool EvaluatePolicy(string attribute, IEnumerable<string> values)`
Evaluates all registered policy rules against the provided attribute and values using the configured combine mode. Rules are matched by attribute name and evaluated in registration order. Returns `true` if all rules pass, otherwise `false`.

* **attribute**: The attribute name to match against registered rules.
* **values**: Collection of string values associated with the attribute.
* **returns**: `true` if all matching rules pass; otherwise `false`.
* **throws**: `ArgumentNullException` if `attribute` or `values` is `null`.

### `async Task<bool> EvaluatePolicyAsync(string attribute, IEnumerable<string> values)`
Asynchronously evaluates all registered policy rules against the provided attribute and values using the configured combine mode. Rules are matched by attribute name and evaluated in registration order. Returns `true` if all rules pass, otherwise `false`.

* **attribute**: The attribute name to match against registered rules.
* **values**: Collection of string values associated with the attribute.
* **returns**: `Task<bool>` resolving to `true` if all matching rules pass; otherwise `false`.
* **throws**: `ArgumentNullException` if `attribute` or `values` is `null`.

### `bool EvaluatePolicy(string attribute, IEnumerable<string> values, out IEnumerable<PolicyRule> failedRules)`
Evaluates all registered policy rules against the provided attribute and values using the configured combine mode. Rules are matched by attribute name and evaluated in registration order. Returns `true` if all rules pass; otherwise `false`. Populates `failedRules` with the subset of rules that did not pass evaluation.

* **attribute**: The attribute name to match against registered rules.
* **values**: Collection of string values associated with the attribute.
* **failedRules**: Output parameter containing rules that failed evaluation.
* **returns**: `true` if all matching rules pass; otherwise `false`.
* **throws**: `ArgumentNullException` if `attribute` or `values` is `null`.

### `List<PolicyRule> Rules`
Gets the collection of registered policy rules. The returned list is a copy; modifications do not affect the internal rule set.

### `PolicyCombineMode CombineWith`
Gets or sets the mode used to combine results from multiple policy rules. Defaults to `PolicyCombineMode.All`.

### `PolicyRuleType Type`
Gets or sets the type of policy rules enforced by this service. When set, only rules of the specified type are evaluated. Defaults to `PolicyRuleType.None`, meaning all rule types are evaluated.

### `string? Attribute`
Gets or sets the attribute name that rules must match for evaluation. When set, only rules with a matching attribute name are evaluated. Defaults to `null`, meaning all attributes are evaluated.

### `List<string> Values`
Gets or sets the collection of values that rules must match for evaluation. When non-empty, only rules whose `Values` collection intersects with this set are evaluated. Defaults to an empty list, meaning all values are accepted.

### `PolicyMatchMode Match`
Gets or sets the matching mode used to compare rule values against the provided values during evaluation. Defaults to `PolicyMatchMode.Any`.
