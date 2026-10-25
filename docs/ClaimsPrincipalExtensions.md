# ClaimsPrincipalExtensions

The `ClaimsPrincipalExtensions` class is a static class of extension methods on `System.Security.Claims.ClaimsPrincipal` and, for one method, `ClaimsIdentity`. It reads standard OIDC/OAuth2 claims (`sub`, `email`, `email_verified`, `roles`, `aud`, `scope`, `iat`, `exp`) and returns typed values or `null` when a claim is missing or malformed. The methods are read-only: they never modify the principal or identity.

The class lives in the `DotnetAuthServer.Extensions` namespace and is declared `public static`. The claim names come from `DotnetAuthServer.Configuration.Constants.Claims`. The companion class `ClaimsPrincipalExtensionsValidation` is a separate public type and is not documented here.

## API

| Method | Receiver | Returns | Claim type read |
|---|---|---|---|
| [`GetSubject`](#getsubject) | `ClaimsPrincipal` | `string?` | `sub` |
| [`GetEmail`](#getemail) | `ClaimsPrincipal` | `string?` | `email` |
| [`IsEmailVerified`](#isemailverified) | `ClaimsPrincipal` | `bool` | `email_verified` |
| [`GetRoles`](#getroles) | `ClaimsPrincipal` | `IEnumerable<string>` | `roles` |
| [`HasRole`](#hasrole) | `ClaimsPrincipal` | `bool` | `roles` |
| [`GetTokenSubject`](#gettokensubject) | `ClaimsIdentity` | `string?` | `sub` |
| [`GetAudience`](#getaudience) | `ClaimsPrincipal` | `string?` | `aud` |
| [`GetScopes`](#getscopes) | `ClaimsPrincipal` | `IEnumerable<string>` | `scope` |
| [`HasScope`](#hasscope) | `ClaimsPrincipal` | `bool` | `scope` |
| [`GetIssuedAt`](#getissuedat) | `ClaimsPrincipal` | `long?` | `iat` |
| [`GetExpiration`](#getexpiration) | `ClaimsPrincipal` | `long?` | `exp` |

All lookups use `FindFirst` or `FindAll` on the receiver, which search every identity in a principal. Only the claim type listed above is read. For example, `GetRoles` does not look at `role` or `ClaimTypes.Role`, and `GetScopes` does not look at `scp`.

### GetSubject

```csharp
public static string? GetSubject(this ClaimsPrincipal principal)
```

Gets the value of the first `sub` claim.

**Parameters**

- `principal`: The principal to read.

**Returns**

The claim value, or `null` if there is no `sub` claim. An empty `sub` value is returned as an empty string.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### GetEmail

```csharp
public static string? GetEmail(this ClaimsPrincipal principal)
```

Gets the value of the first `email` claim.

**Parameters**

- `principal`: The principal to read.

**Returns**

The claim value, or `null` if there is no `email` claim.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### IsEmailVerified

```csharp
public static bool IsEmailVerified(this ClaimsPrincipal principal)
```

Checks whether the `email_verified` claim is present and parses as `true`. The value is parsed with `bool.TryParse`, so `"true"` matches in any letter case. Any other value, including `"1"`, `"yes"`, or a malformed string, is treated as not verified. The method does not throw for malformed values.

**Parameters**

- `principal`: The principal to check.

**Returns**

`true` if the claim exists and its value parses as `true`; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### GetRoles

```csharp
public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
```

Gets the values of all `roles` claims. The result is lazy: the claims are read when the sequence is enumerated, not when the method is called. Claims are returned in the order the principal holds them.

**Parameters**

- `principal`: The principal to read.

**Returns**

An enumerable of role values. The sequence is empty if there are no `roles` claims. A `roles` claim with an empty value yields an empty string.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### HasRole

```csharp
public static bool HasRole(this ClaimsPrincipal principal, string role)
```

Checks whether any `roles` claim equals `role`. The comparison uses `StringComparison.OrdinalIgnoreCase`, so `"Admin"` and `"admin"` match.

**Parameters**

- `principal`: The principal to check.
- `role`: The role name to look for. An empty string is accepted and matches only a `roles` claim whose value is also empty.

**Returns**

`true` if the principal has a matching role; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `principal` or `role` is `null`.

### GetTokenSubject

```csharp
public static string? GetTokenSubject(this ClaimsIdentity identity)
```

Gets the value of the first `sub` claim in a single identity. This overload is defined on `ClaimsIdentity`, not `ClaimsPrincipal`. To call it from a principal, pass the identity you are interested in, for example `(ClaimsIdentity)principal.Identity!`. Only that identity is searched; other identities on the same principal are ignored.

**Parameters**

- `identity`: The identity to read.

**Returns**

The claim value, or `null` if there is no `sub` claim in the identity.

**Exceptions**

- `ArgumentNullException`: `identity` is `null`.

### GetAudience

```csharp
public static string? GetAudience(this ClaimsPrincipal principal)
```

Gets the value of the first `aud` claim. If the token has several audiences, only the first is returned. Use `FindAll` on the principal to read the rest.

**Parameters**

- `principal`: The principal to read.

**Returns**

The first `aud` value, or `null` if there is no `aud` claim. In OAuth2 terms this is usually the client ID.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### GetScopes

```csharp
public static IEnumerable<string> GetScopes(this ClaimsPrincipal principal)
```

Reads the first `scope` claim and splits its value on space characters. Empty entries are removed, so repeated spaces are harmless. Only the space character is a separator; tabs and other whitespace are not. Only the first `scope` claim is read.

**Parameters**

- `principal`: The principal to read.

**Returns**

An enumerable of scope names. The sequence is empty if there is no `scope` claim, or if the claim's value is null, empty, or whitespace only.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### HasScope

```csharp
public static bool HasScope(this ClaimsPrincipal principal, string scope)
```

Checks whether the scopes returned by `GetScopes` contain `scope`. The comparison is ordinal and case-sensitive, as OAuth2 requires.

**Parameters**

- `principal`: The principal to check.
- `scope`: The scope name to look for. An empty string never matches, because `GetScopes` never returns empty entries.

**Returns**

`true` if the principal has the scope; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `principal` or `scope` is `null`.

### GetIssuedAt

```csharp
public static long? GetIssuedAt(this ClaimsPrincipal principal)
```

Gets the first `iat` claim and parses it as a 64-bit integer. The value is a Unix timestamp in seconds since the epoch. The value is not checked for range or plausibility.

**Parameters**

- `principal`: The principal to read.

**Returns**

The timestamp, or `null` if the claim is missing or its value is not a valid `long`. A decimal value such as `"1.5"` yields `null`.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

### GetExpiration

```csharp
public static long? GetExpiration(this ClaimsPrincipal principal)
```

Gets the first `exp` claim and parses it as a 64-bit integer. The value is a Unix timestamp in seconds since the epoch. The method does not check whether the token has expired. It only reports the value.

**Parameters**

- `principal`: The principal to read.

**Returns**

The timestamp, or `null` if the claim is missing or its value is not a valid `long`.

**Exceptions**

- `ArgumentNullException`: `principal` is `null`.

## Usage

### Role-based authorization

```csharp
if (HttpContext.User.HasRole("admin"))
{
    // Allow access to administrative functionality
}
else
{
    // Challenge or forbid
}
```

### Token expiration check

```csharp
var exp = HttpContext.User.GetExpiration();
if (exp.HasValue && DateTimeOffset.FromUnixTimeSeconds(exp.Value) < DateTimeOffset.UtcNow)
{
    // Token has expired; request a new one or reject the request
}
```

## Remarks

- The methods are stateless. They read the supplied principal or identity and do not change it. They are safe to call concurrently as long as nothing else mutates the same principal.
- A `null` argument throws `ArgumentNullException` in every method. Methods that return a value throw before reading any claim.
- When a claim is absent or malformed, methods that return `string?` or `long?` return `null`. `GetRoles` and `GetScopes` return an empty sequence instead.
- `HasRole` compares roles case-insensitively. `HasScope` compares scopes case-sensitively.
- `IsEmailVerified` never throws for malformed values. It returns `false`.
- These helpers only extract claims. They do not validate the token's signature, issuer, audience, or lifetime. Do that separately before trusting the principal.
