# ScopeStringExtensions

**Namespace:** `DotnetAuthServer.Security`

Extension methods that convert between OAuth 2.0 space-delimited scope strings (RFC 6749 §3.3, for example `"openid profile email"`) and collections of individual scopes. Use them wherever a `scope` parameter is parsed, checked, or built, so the splitting rules are the same everywhere in the server.

The class is `public static`. It has no state and no constructor.

The class declares four public methods. This page covers all four: `ToScopeSet`, `ContainsScope`, and `ToScopeString` (the three main helpers), plus `HasAllScopes`.

## Methods

### `HashSet<string> ToScopeSet(this string scopeString)`

Splits a space-delimited scope string into a `HashSet<string>`. The split uses a single space character as the only delimiter.

Behaviour that follows from the implementation:

- Duplicate scopes collapse into one entry.
- Order is not preserved. Use the set only for membership checks.
- Empty entries are not removed. Consecutive spaces, and leading or trailing spaces, produce `""` entries in the set. `ToScopeSet("")` returns a set that contains one empty string.
- Comparison is case-sensitive, because the set uses the default string comparer. `"Read"` and `"read"` are different scopes.
- Tabs and other whitespace are not delimiters.

**Parameters:**
- `scopeString`: The space-delimited scope string.

**Returns:** A `HashSet<string>` with one entry per token.

**Exceptions:**
- `NullReferenceException`: `scopeString` is `null`. The method does not guard against this. Check for `null` before calling if the input can be missing.

**Example:**

```csharp
using DotnetAuthServer.Security;

string requested = "openid profile email openid";
HashSet<string> scopes = requested.ToScopeSet();
// scopes contains: "openid", "profile", "email" (the duplicate "openid" collapses)
```

### `bool ContainsScope(this string scopeString, string scope)`

Returns `true` if `scope` is one of the tokens in `scopeString`. It calls `ToScopeSet` internally, so it follows the same splitting rules. Matching is exact and case-sensitive.

Because the input is split first, `scope` is always compared as a whole token. `ContainsScope("read:users write", "read")` returns `false`. Passing a value that contains a space, such as `"read write"`, always returns `false`.

**Parameters:**
- `scopeString`: The space-delimited scope string to search.
- `scope`: The single scope to look for.

**Returns:** `true` if the scope is present; otherwise `false`.

**Exceptions:**
- `NullReferenceException`: `scopeString` is `null`.

**Example:**

```csharp
using DotnetAuthServer.Security;

string grantedScope = "openid profile offline_access";

if (grantedScope.ContainsScope("offline_access"))
{
    // Issue a refresh token.
}

bool canReadEmail = grantedScope.ContainsScope("email"); // false
```

### `bool HasAllScopes(this string scopeString, params string[] scopes)`

Returns `true` only if every scope in `scopes` is present in `scopeString`. Use it when a request needs several scopes at once. It calls `ToScopeSet` internally and then checks that the set is a superset of `scopes`.

- Matching is exact and case-sensitive, as with `ContainsScope`.
- If `scopes` is empty (no arguments were passed), the result is always `true`.
- Duplicate values in `scopes` have no effect on the result.

**Parameters:**
- `scopeString`: The space-delimited scope string to search.
- `scopes`: The scopes that must all be present. Pass them as separate arguments, or as an array.

**Returns:** `true` if every scope is present; otherwise `false`.

**Exceptions:**
- `NullReferenceException`: `scopeString` is `null`.
- `ArgumentNullException`: `scopes` is `null`. This happens only when the array is passed explicitly as `null`. Omitting the arguments gives an empty array instead.

**Example:**

```csharp
using DotnetAuthServer.Security;

string granted = "openid profile email";

bool canReadProfile = granted.HasAllScopes("openid", "profile");        // true
bool canManageUsers = granted.HasAllScopes("openid", "users:write");    // false
bool noRequirements = granted.HasAllScopes();                           // true
```

### `string ToScopeString(this IEnumerable<string> scopes)`

Joins a collection of scopes into one space-delimited string. It uses `string.Join(" ", scopes)`, so the output keeps the input order and does not remove duplicates.

**Parameters:**
- `scopes`: The scopes to join. Use a `List<string>`, an array, or a `HashSet<string>` (the set has no defined order).

**Returns:** The scopes separated by single spaces. An empty collection returns `""`.

**Exceptions:**
- `ArgumentNullException`: `scopes` is `null`.

Null elements are written as empty strings. The method does not validate the scope values, so it can produce a string with extra spaces if an element is empty or contains a space.

**Example:**

```csharp
using DotnetAuthServer.Security;

var scopes = new List<string> { "openid", "profile", "email" };
string scopeClaim = scopes.ToScopeString();
// "openid profile email"

HashSet<string> fromSet = requested.ToScopeSet();
string normalized = fromSet.ToScopeString();
// Same scopes, but the order follows the set's enumeration order, which is not guaranteed.
```

## Common patterns

### Validate a requested scope against the client's allowed scopes

```csharp
using DotnetAuthServer.Security;

string requested = "openid profile admin";
string allowed = "openid profile email";

HashSet<string> requestedScopes = requested.ToScopeSet();
HashSet<string> allowedScopes = allowed.ToScopeSet();

bool allAllowed = requestedScopes.IsSubsetOf(allowedScopes); // false, "admin" is not allowed
```

### Build a scope string from a list of granted scopes

```csharp
using DotnetAuthServer.Security;

var granted = new List<string> { "openid", "offline_access" };

if (someCondition)
{
    granted.Add("email");
}

string scope = granted.ToScopeString(); // "openid offline_access email"
```

### Gate an endpoint on required scopes

```csharp
using DotnetAuthServer.Security;

string accessTokenScope = principal.FindFirst("scope")?.Value ?? string.Empty;

if (!accessTokenScope.HasAllScopes("openid", "profile"))
{
    // Reject with insufficient_scope.
}
```

## Notes

- The methods are pure: they do not read configuration, call services, or change their input.
- Trimming and validating scope values is the caller's responsibility. The validation for scope values lives in `ScopeValidationService`, not in these helpers.
- Because `ToScopeSet` does not remove empty entries, normalise the input (for example, with `string.Join(" ", value.Split(' ', StringSplitOptions.RemoveEmptyEntries))`) before storing or comparing scope strings that come from untrusted clients.
