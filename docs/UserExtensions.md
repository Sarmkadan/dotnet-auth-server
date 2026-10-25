# UserExtensions

**Namespace:** `DotnetAuthServer.Domain.Entities`

Extension methods on the `User` entity for role checks, attribute access, and common display and authentication questions. The class is `public static`. The methods hold no state. The only write is `SetAttribute`, which also updates `User.UpdatedAt`.

Every method throws `ArgumentNullException` when the `user` argument is `null`. Methods that take a string also throw `ArgumentException` for a `null` or whitespace string, as noted per method.

## API

| Method | Returns | Reads or writes on `User` |
|---|---|---|
| [`HasRole`](#hasrole) | `bool` | `Roles` |
| [`HasAnyRole`](#hasanyrole) | `bool` | `Roles` (through `HasRole`) |
| [`GetAttribute<T>`](#getattributet) | `T?` | `Attributes` |
| [`SetAttribute`](#setattribute) | `void` | `Attributes`, `UpdatedAt` |
| [`IsAdmin`](#isadmin) | `bool` | `Roles` (through `HasRole`) |
| [`GetDisplayName`](#getdisplayname) | `string` | `FullName`, `Username` |
| [`CanAuthenticate`](#canauthenticate) | `bool` | `EmailVerified`, `IsActive`, `IsLocked()` |
| [`SecondsSinceLastLogin`](#secondssincelastlogin) | `long?` | `LastLoginAt` |

### HasRole

```csharp
public static bool HasRole(this User user, string role)
```

Returns `true` when `role` is in `user.Roles`. The comparison uses `StringComparer.OrdinalIgnoreCase`, so `"admin"` matches `"Admin"`.

**Parameters**

- `user`: The user to check.
- `role`: The role name to look for.

**Returns**

`true` if the user has the role; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.
- `ArgumentException`: `role` is `null`, empty, or whitespace.

### HasAnyRole

```csharp
public static bool HasAnyRole(this User user, IEnumerable<string> roles)
```

Returns `true` when at least one entry in `roles` satisfies `HasRole`. The check is lazy. It stops at the first match, so an invalid entry after a match is never validated.

An empty `roles` sequence returns `false`. A `null` or whitespace entry that is reached before a match throws `ArgumentException` from `HasRole`.

**Parameters**

- `user`: The user to check.
- `roles`: The role names to look for. Matching is case-insensitive.

**Returns**

`true` if the user has any of the roles; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `user` or `roles` is `null`.
- `ArgumentException`: an entry that is reached is `null` or whitespace.

### GetAttribute&lt;T&gt;

```csharp
public static T? GetAttribute<T>(this User user, string key, T? defaultValue = default)
```

Looks up `key` in `user.Attributes`. If the key exists and the stored value is of type `T` (checked with `is T`), returns that value. In every other case it returns `defaultValue`. This covers a missing key, a stored value of a different type, and a stored `null`.

A type mismatch is silent. The method does not throw and does not log. For example, storing an `int` and reading it as `string` returns the default.

The key lookup is case-sensitive, because `Attributes` uses the dictionary's default comparer.

**Type parameters**

- `T`: The expected type of the stored value.

**Parameters**

- `user`: The user to read from.
- `key`: The attribute key.
- `defaultValue`: The value returned when the lookup fails. Defaults to `default(T)`.

**Returns**

The stored value if it exists and has type `T`; otherwise `defaultValue`.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.
- `ArgumentException`: `key` is `null`, empty, or whitespace.

### SetAttribute

```csharp
public static void SetAttribute(this User user, string key, object value)
```

Stores `value` under `key` in `user.Attributes`, replacing any existing value. Then sets `user.UpdatedAt` to `DateTime.UtcNow`.

The method does not check the type of `value`. A later `GetAttribute<T>` with a different type returns the default.

If an exception is thrown, no changes are made. All checks run before the dictionary is written.

**Parameters**

- `user`: The user to update.
- `key`: The attribute key.
- `value`: The value to store. Must not be `null`.

**Returns**

Nothing.

**Exceptions**

- `ArgumentNullException`: `user` or `value` is `null`.
- `ArgumentException`: `key` is `null`, empty, or whitespace.

### IsAdmin

```csharp
public static bool IsAdmin(this User user)
```

Returns `true` when the user has the role `"Admin"` or the role `"Administrator"`. Both checks go through `HasRole`, so they are case-insensitive. The role names are fixed in the method and are not configurable.

**Parameters**

- `user`: The user to check.

**Returns**

`true` if the user has either role; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.

### GetDisplayName

```csharp
public static string GetDisplayName(this User user)
```

Returns `user.FullName` when it is not `null`. Otherwise returns `user.Username`. The fallback uses `??`, so only `null` triggers it. An empty or whitespace `FullName` is returned as is.

**Parameters**

- `user`: The user to read from.

**Returns**

The full name, or the username when no full name is set.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.

### CanAuthenticate

```csharp
public static bool CanAuthenticate(this User user)
```

Returns `true` only when all three conditions hold:

- `user.EmailVerified` is `true`.
- `user.IsActive` is `true`.
- `user.IsLocked()` is `false`.

The method evaluates these in that order and short-circuits. It does not check the password, MFA, or consent state.

**Parameters**

- `user`: The user to check.

**Returns**

`true` if the user can sign in under these rules; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.

### SecondsSinceLastLogin

```csharp
public static long? SecondsSinceLastLogin(this User user)
```

Returns the whole seconds between `user.LastLoginAt` and `DateTime.UtcNow`. The value is truncated toward zero, not rounded. This is a cast to `long`, so fractional seconds are dropped.

Returns `null` when `LastLoginAt` has no value, for example when the user has never logged in.

The method assumes `LastLoginAt` is stored in UTC. If `LastLoginAt` is later than the current time, the result is negative.

**Parameters**

- `user`: The user to read from.

**Returns**

The number of seconds since the last login, or `null` if the user has never logged in.

**Exceptions**

- `ArgumentNullException`: `user` is `null`.

## Related

- [User](User.md): the entity these methods extend.
- [ClientModelExtensions](ClientModelExtensions.md) and [ClientExtensions](ClientExtensions.md): the equivalent extension classes for the `Client` entity.
