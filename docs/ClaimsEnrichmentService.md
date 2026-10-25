# ClaimsEnrichmentService

`ClaimsEnrichmentService` builds the list of `System.Security.Claims.Claim` objects that get embedded in tokens for a user. It maps a `User` entity to identity, profile, email, phone, address, role, and application claims, gated by the scopes that were granted. It also provides `FilterClaimsByScope`, which removes claims a token's scopes do not allow.

The class is `public sealed` and lives in `src/Services/ClaimsEnrichmentService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; it is registered as a concrete scoped service (`Program.cs`: `AddScoped<ClaimsEnrichmentService>()`). It depends only on the BCL and `Microsoft.Extensions.Logging`, so it adds no NuGet dependencies.

## Role in the system

- Input is a `User` entity (`src/Domain/Entities/User.cs`). The service does not load users itself.
- Output is a `List<Claim>` that the caller adds to the token being issued.
- The service holds an `IUserRepository`, but none of its public members call it.
- Each call logs the user ID and the requested scopes at `Information` level, and the resulting claim count at `Debug` and `Information` level. Claim values are not logged.

## Constructor

### `public ClaimsEnrichmentService(IUserRepository userRepository, ILogger<ClaimsEnrichmentService> logger)`

Creates the service from its collaborators.

**Parameters:**
- `userRepository` – user store. Currently stored but not used.
- `logger` – logger for enrichment events.

**Exceptions:** none. The constructor does not validate its arguments.

## Methods

### `public async Task<List<Claim>> EnrichUserClaimsAsync(User user, IEnumerable<string> grantedScopes, CancellationToken cancellationToken = default)`

Builds the claim list for a user. Claims are added in this order:

1. **Always included:**
   - `ClaimTypes.NameIdentifier` = `user.UserId`
   - `sub` = `user.UserId`
2. **`profile` scope** (see [Profile claims](#profile-claims)).
3. **`email` scope** (see [Email claims](#email-claims)).
4. **`phone` scope** (see [Phone claims](#phone-claims)).
5. **`address` scope** (see [Address claims](#address-claims)). Adds nothing.
6. **Roles, always included:** for each entry in `user.Roles`, `ClaimTypes.Role` and `roles` with the same value.
7. **Application claims, always included** (see [Application claims](#application-claims)).

Scope matching is an exact, case-sensitive `List.Contains` check. `"Profile"` does not match `"profile"`. A `null` `grantedScopes` is treated as an empty list.

**Parameters:**
- `user` – the user to enrich.
- `grantedScopes` – scopes granted to the token. May be `null`.
- `cancellationToken` – accepted for signature compatibility. The method does not observe it, and it contains no `await`, so the returned task is already completed.

**Return value:** a new `List<Claim>`. Duplicate claim types are possible (for example, `name` and `ClaimTypes.Name` carry the same value). The list is never `null`.

**Exceptions:**
- `ArgumentNullException` (parameter `user`) if `user` is `null`.
- `NullReferenceException` if `user.Roles` is `null`. The property defaults to an empty collection, so this only happens if it is explicitly set to `null`.

**Example:**

```csharp
var claims = await enrichment.EnrichUserClaimsAsync(
    user,
    new[] { "openid", "profile", "email" },
    cancellationToken);

var identity = new ClaimsIdentity(claims, "Bearer");
```

## Claim reference

Timestamps are Unix epoch seconds as a decimal string. The fractional part is truncated. Each value is computed from `DateTime.ToUniversalTime()`.

### Profile claims

Added only when `profile` is granted. Emitted by `EnrichProfileClaims`.

| Claim type | Value | Condition |
|---|---|---|
| `ClaimTypes.Name` and `name` | `user.FullName` | `FullName` is not null or whitespace |
| `given_name` | First whitespace-separated token of `FullName` | Token exists and is not whitespace |
| `family_name` | Last whitespace-separated token of `FullName` | Token exists and is not whitespace |
| `updated_at` | `user.UpdatedAt` as Unix seconds | Always, when the scope is granted |

For a single-word name, `given_name` and `family_name` contain the same word.

### Email claims

Added only when `email` is granted. Emitted by `EnrichEmailClaims`.

| Claim type | Value | Condition |
|---|---|---|
| `ClaimTypes.Email` and `email` | `user.Email` | `Email` is not null or whitespace |
| `email_verified` | `"true"` or `"false"` from `user.EmailVerified` | Same condition as above |

### Phone claims

Added only when `phone` is granted. Emitted by `EnrichPhoneClaims`.

| Claim type | Value | Condition |
|---|---|---|
| `phone_number_verified` | `"false"` | Always |

This is a placeholder. `User` has no phone properties, so the value does not reflect the user's data.

### Address claims

Added only when `address` is granted. `EnrichAddressClaims` is a placeholder and adds no claims.

### Role claims

Always included. Each role produces two claims:

| Claim type | Value |
|---|---|
| `ClaimTypes.Role` | Role name |
| `roles` | Role name |

### Application claims

Always included. Emitted by `EnrichApplicationClaims`.

| Claim type | Value | Condition |
|---|---|---|
| `preferred_username` | `user.Username` | Not null or whitespace |
| `account_locked` | `"true"` | Only when `user.IsLocked()` is `true` |
| `account_created` | `user.CreatedAt` as Unix seconds | Always |

`account_created` is included regardless of scope. It is an application claim, so `FilterClaimsByScope` keeps it.

## FilterClaimsByScope

### `public List<Claim> FilterClaimsByScope(List<Claim> claims, IEnumerable<string> grantedScopes)`

Returns the subset of `claims` that the granted scopes allow. The input list is not modified. Claims are kept in their original order.

The filter applies the data-minimization rule: profile, email, and phone data leave the token unless the matching scope was granted.

**Rules, applied in order:**

1. **Always kept** (technical and identity claims): `ClaimTypes.NameIdentifier`, `sub`, `iss`, `aud`, `iat`, `exp`.
2. Otherwise, the claim type decides:

| Claim type | Kept when |
|---|---|
| `ClaimTypes.Name`, `name`, `given_name`, `family_name`, `updated_at` | `profile` granted |
| `ClaimTypes.Email`, `email`, `email_verified` | `email` granted |
| `ClaimTypes.HomePhone`, `phone_number`, `phone_number_verified` | `phone` granted |
| `address` | `address` granted |
| `ClaimTypes.Role`, `roles` | Always |
| Any other type (including `preferred_username`, `account_locked`, `account_created`) | Always |

**Parameters:**
- `claims` – claims to filter. Must not be `null`.
- `grantedScopes` – scopes granted to the token. `null` is treated as no scopes.

**Return value:** a new `List<Claim>`. It is empty if `claims` is empty.

**Exceptions:** `NullReferenceException` if `claims` is `null`.

**Note:** `ClaimTypes.HomePhone` and `phone_number` are matched but never produced by `EnrichUserClaimsAsync`. The `iss`, `aud`, `iat`, and `exp` claims are also matched but never produced by this service. They are expected to be added later by the token issuer.

## Known limitations

- `EnrichUserClaimsAsync` is `async` but does not await anything. `cancellationToken` is ignored.
- `_userRepository` is injected but unused.
- Phone and address claims are placeholders (see above).
- `FilterClaimsByScope` is public but is not called inside this repository. The token pipeline must call it explicitly if it should apply.
- Scope matching is case-sensitive.
- Claim types are compared as plain strings with no normalization, so a claim type must match the exact casing used here.

## Related

- `src/Domain/Entities/User.cs` – source of the claim values.
- `docs/ARCHITECTURE.md` – where the service sits in the token flow.
