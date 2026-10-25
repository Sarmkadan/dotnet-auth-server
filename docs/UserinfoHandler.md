# UserinfoHandler

**Namespace:** `DotnetAuthServer.Handlers`

Implements the user-claims logic of the OpenID Connect UserInfo endpoint. Given a `ClaimsPrincipal` taken from a validated access token, it loads the user with `IUserRepository`, filters the claims by the token's `scope` values, and returns a `UserinfoResponse`. Returns `null` when the subject is missing or unknown.

The handler does not authenticate the caller or validate the token. It trusts the principal it is given, so the calling controller must have verified the token first. See [UserinfoController](UserinfoController.md).

`sealed`. Holds only its repository and logger, so it is safe to share. It is registered as scoped in DI (`Program.cs`), so it is normally resolved per request.

---

## Constructor

### `UserinfoHandler(IUserRepository userRepository, ILogger<UserinfoHandler> logger)`

Creates the handler. The constructor only stores its arguments; it does not validate them.

| Parameter | Type | Description |
|---|---|---|
| `userRepository` | `IUserRepository` | Repository used to load the user by subject. |
| `logger` | `ILogger<UserinfoHandler>` | Logger for userinfo events. |

---

## Methods

### `Task<UserinfoResponse?> GetUserinfoAsync(ClaimsPrincipal principal, CancellationToken cancellationToken = default)`

Builds the userinfo response for the subject of `principal`.

| Parameter | Type | Description |
|---|---|---|
| `principal` | `ClaimsPrincipal` | Principal from the access token. Must carry `sub`; `scope` is optional. |
| `cancellationToken` | `CancellationToken` | Passed to `IUserRepository.GetByIdAsync`. |

**Returns:** A `UserinfoResponse`, or `null` if the request cannot be served. Exceptions from the repository propagate to the caller.

**Behavior, in order:**

1. Reads the first claim of type `sub`. If it is missing, empty or whitespace, logs a warning and returns `null`.
2. Loads the user with `IUserRepository.GetByIdAsync(userId)`. If no user is found, logs a warning containing the `sub` and returns `null`.
3. Reads the scopes from the first `scope` claim (see Scope handling below).
4. Creates a response with `Sub` set to the `sub` claim value. `sub` is always included, whatever the scopes are. The `openid` scope is not checked.
5. Adds claims according to the scopes (see the table below).
6. Logs an information message with the `sub` and the space-joined scopes, then returns the response.

**Claims by scope:**

| Scope | Properties populated |
|---|---|
| `profile` | `Name`, `GivenName`, `FamilyName`, `UpdatedAt` |
| `email` | `Email`, `EmailVerified` |
| `address` | None. The branch is a placeholder, so `Address` is never set. |
| `phone` | None. The branch is a placeholder, so `PhoneNumber` and `PhoneNumberVerified` are never set. |

Details for the `profile` scope:

- `Name` is `User.FullName`, or `User.Username` when `FullName` is `null`.
- `GivenName` is the first space-separated token of `FullName`. `FamilyName` is the last token. For a single-word name, both hold that word. When `FullName` is `null`, both are `null`.
- `UpdatedAt` is `User.UpdatedAt` in whole seconds since the Unix epoch. The value is converted to UTC first, so an `Unspecified`-kind `DateTime` is treated as local time. Fractional seconds are truncated.

**Scope handling:** Scopes come from the first `scope` claim, split on spaces with empty entries removed. Matching is case-insensitive, so `Profile` matches `profile`. A missing or blank `scope` claim gives no scopes, and only `sub` is returned.

**Example:**

```csharp
var handler = scope.ServiceProvider.GetRequiredService<UserinfoHandler>();
UserinfoResponse? userinfo = await handler.GetUserinfoAsync(
    HttpContext.User,
    HttpContext.RequestAborted);

if (userinfo is null)
{
    return Results.NotFound();
}

return Results.Ok(userinfo);
```

---

## Related Types

### `UserinfoResponse`

**Namespace:** `DotnetAuthServer.Handlers` (declared in the same file as the handler)

Response model for the userinfo endpoint. Sealed POCO with `get; set;` properties. Only the properties allowed by the token's scopes are populated; the others stay `null`.

| Property | Type | Scope | Description |
|---|---|---|---|
| `Sub` | `string` | always | Subject identifier. Defaults to `string.Empty`. |
| `Name` | `string?` | `profile` | Full name, or username when no full name is set. |
| `GivenName` | `string?` | `profile` | First token of the full name. |
| `FamilyName` | `string?` | `profile` | Last token of the full name. |
| `UpdatedAt` | `long?` | `profile` | Last update time, seconds since the Unix epoch. |
| `Email` | `string?` | `email` | Email address. |
| `EmailVerified` | `bool?` | `email` | Whether the email address is verified. |
| `Address` | `AddressInfo?` | `address` | Never populated in the current code. |
| `PhoneNumber` | `string?` | `phone` | Never populated in the current code. |
| `PhoneNumberVerified` | `bool?` | `phone` | Never populated in the current code. |

#### `override string ToString()`

Returns a single-line string such as `UserinfoHandler { Sub = ..., Name = ..., GivenName = ..., FamilyName = ..., UpdatedAt = ..., Email = ... }`. It does not include `EmailVerified`, `Address` or the phone fields, and it includes the email address, so avoid logging it in contexts where email is sensitive. The format is not a stable contract.

### `AddressInfo`

**Namespace:** `DotnetAuthServer.Handlers` (declared in the same file as the handler)

OpenID Connect address claim structure. Sealed POCO. All properties are `string?` and default to `null`. It has no `ToString` override.

| Property | Description |
|---|---|
| `StreetAddress` | Street address, including house number. |
| `Locality` | City or locality. |
| `Region` | State, province or region. |
| `PostalCode` | Postal or ZIP code. |
| `Country` | Country. |

Nothing in the handler creates an `AddressInfo` yet, so `UserinfoResponse.Address` is always `null`.

---

## Usage Notes

- Call `GetUserinfoAsync` only after the access token is validated. The handler does not check the signature, expiry, issuer or audience.
- A `null` result means either the token has no `sub` or the user does not exist. Map it to a `404` or an error response; the handler does not distinguish the two cases to the caller. The log does distinguish them.
- Only `profile` and `email` return data today. `address` and `phone` are accepted but produce nothing.
- `Sub` is the raw `sub` claim value. It is not normalized against `User.UserId`, so the two match only because the lookup used that value.
- Scope filtering only controls which claims are returned. The information log records every scope from the token, not just the ones that produced data.

---

## See Also

- [UserinfoController](UserinfoController.md): exposes the userinfo endpoint and calls this handler.
- [IUserRepository](IUserRepository.md): source of the `User` record.
- [User](User.md): entity whose `FullName`, `Username`, `Email`, `EmailVerified` and `UpdatedAt` are mapped here.
- [TokenIntrospectionHandler](TokenIntrospectionHandler.md): another handler that reads token claims.
- [docs/ARCHITECTURE.md](ARCHITECTURE.md): design overview and known limitations.
