# ClientExtensions

**Namespace:** `DotnetAuthServer.Domain.Entities`

Extension methods on the `Client` entity that answer common questions about a client's configuration: whether it is public, whether it must use PKCE, how long its tokens live, and whether it has CORS origins. The class is `public static`. The methods are read-only and hold no state.

The file also declares the public enum `TokenType` in the same namespace. It is used by `GetTokenLifetimeMinutes`. See [TokenType](#tokentype).

## API

| Method | Returns | Reads from `Client` |
|---|---|---|
| [`IsPublicClient`](#ispublicclient) | `bool` | `IsConfidential` |
| [`RequiresPkce`](#requirespkce) | `bool` | `IsConfidential`, `RequirePkce` |
| [`GetTokenLifetimeMinutes`](#gettokenlifetimeminutes) | `int` | `AccessTokenLifetime`, `RefreshTokenLifetime` |
| [`HasCorsOrigins`](#hascorsorigins) | `bool` | `AllowedCorsOrigins` |

All four methods extend `Client` (`this Client client`). Each one first calls `ArgumentNullException.ThrowIfNull(client)`, so passing `null` throws `ArgumentNullException`.

### IsPublicClient

```csharp
public static bool IsPublicClient(this Client client)
```

Returns `true` when the client is not confidential. The result is `!client.IsConfidential`. This method does not check whether a client secret is set.

**Parameters**

- `client`: The client to check.

**Returns**

`true` if `client.IsConfidential` is `false`; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.

### RequiresPkce

```csharp
public static bool RequiresPkce(this Client client)
```

Returns `true` only when the client is confidential and has `RequirePkce` set. The result is `client.IsConfidential && client.RequirePkce`.

A public client returns `false` here even when `RequirePkce` is `true`, because the `IsConfidential` check comes first. Callers that must enforce PKCE for public clients need their own check.

**Parameters**

- `client`: The client to check.

**Returns**

`true` if the client is confidential and `RequirePkce` is `true`; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.

### GetTokenLifetimeMinutes

```csharp
public static int GetTokenLifetimeMinutes(this Client client, TokenType tokenType)
```

Converts the lifetime of the requested token type from seconds to minutes. The lifetimes are stored on the client as seconds:

| `tokenType` | Source property | Default on `Client` |
|---|---|---|
| `TokenType.Access` | `AccessTokenLifetime` | 3600 (60 minutes) |
| `TokenType.Refresh` | `RefreshTokenLifetime` | 2592000 (30 days) |

The result is an integer division by 60, so any remainder of seconds is dropped. For example, 90 seconds returns `1`, and 59 seconds returns `0`.

**Parameters**

- `client`: The client whose lifetime to read.
- `tokenType`: The token type to read, `TokenType.Access` or `TokenType.Refresh`.

**Returns**

The lifetime in whole minutes.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.
- `ArgumentOutOfRangeException`: `tokenType` is not a defined `TokenType` value. The exception's parameter name is `tokenType`.

### HasCorsOrigins

```csharp
public static bool HasCorsOrigins(this Client client)
```

Returns `true` when `client.AllowedCorsOrigins` contains at least one entry. The method only counts entries. It does not check that they are valid origins, and it does not check for duplicates.

`AllowedCorsOrigins` is initialized to an empty collection, so a new client returns `false` until origins are added.

**Parameters**

- `client`: The client to check.

**Returns**

`true` if `AllowedCorsOrigins` has one or more entries; otherwise `false`.

**Exceptions**

- `ArgumentNullException`: `client` is `null`.

## TokenType

```csharp
public enum TokenType
{
    Access,
    Refresh
}
```

Identifies which lifetime `GetTokenLifetimeMinutes` reads.

| Member | Meaning |
|---|---|
| `Access` | Access token. Reads `AccessTokenLifetime`. |
| `Refresh` | Refresh token. Reads `RefreshTokenLifetime`. |

This enum is separate from `DotnetAuthServer.Domain.Enums.TokenType`. Both types have the same name. Code in `DotnetAuthServer.Domain.Entities` that refers to `TokenType` gets this enum. Code that uses the enum in `Domain.Enums` must reference it by its full name, or add a `using` alias.

## Usage

### Choosing a PKCE requirement

```csharp
if (client.RequiresPkce())
{
    // Reject the authorization request if no code_challenge was sent
}
```

### Reading a token lifetime

```csharp
int accessMinutes = client.GetTokenLifetimeMinutes(TokenType.Access);
DateTime expiresAt = DateTime.UtcNow.AddMinutes(accessMinutes);
```

### Checking for CORS configuration

```csharp
if (client.HasCorsOrigins())
{
    // Apply the client's CORS policy
}
```

## Notes

- **Null handling:** Every method throws `ArgumentNullException` for a `null` client. There is no default return value.
- **Thread safety:** The methods do not modify the client and keep no state. They are safe to call from several threads at once, as long as nothing else changes the `Client` instance during the call.
- **Cost:** Each method reads one or two properties and does a comparison or division. The `HasCorsOrigins` check uses `Count`, which is constant time for the collection types used on `Client`.
