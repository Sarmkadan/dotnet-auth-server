# ClientModelExtensions

**Namespace:** `DotnetAuthServer.Domain.Entities`

Extension methods on the `Client` entity that answer three common questions: whether the client is confidential, whether it allows a grant type, and whether it has a given redirect URI. The class is `public static`. The methods are read-only and hold no state.

Unlike [ClientExtensions](ClientExtensions.md), these methods are null-safe. A `null` client, or a `null` collection on the client, returns `false` instead of throwing.

## API

| Method | Returns | Reads from `Client` |
|---|---|---|
| [`IsConfidential`](#isconfidential) | `bool` | `IsConfidential` |
| [`AllowsGrantType`](#allowsgranttype) | `bool` | `AllowedGrantTypes` |
| [`HasRedirectUri`](#hasredirecturi) | `bool` | `RedirectUris` |

All three methods extend `Client` (`this Client client`). None of them throws for a `null` argument.

### IsConfidential

```csharp
public static bool IsConfidential(this Client client)
```

Returns the value of `client.IsConfidential`. If `client` is `null`, returns `false`.

This is a plain pass-through to the property. The extension does not check whether a client secret is set.

**Parameters**

- `client`: The client to check. May be `null`.

**Returns**

`true` if the client is confidential; `false` otherwise, including when `client` is `null`.

**Exceptions**

None.

### AllowsGrantType

```csharp
public static bool AllowsGrantType(this Client client, string grantType)
```

Returns `true` when `grantType` is present in `client.AllowedGrantTypes`. The check uses `ICollection<string>.Contains` with the default string comparer, so it is **case-sensitive**. For example, `"Client_Credentials"` does not match `"client_credentials"`.

Returns `false` when:

- `client` is `null`.
- `client.AllowedGrantTypes` is `null`.
- `grantType` is not in the collection. This includes `null` and an empty string, unless the collection itself contains that value.

The instance method `Client.IsGrantTypeAllowed` uses `StringComparer.OrdinalIgnoreCase` and throws `ArgumentNullException` for a `null` grant type. Use that method when case-insensitive matching is needed. This extension does not match it.

**Parameters**

- `client`: The client to check. May be `null`.
- `grantType`: The grant type to look for, for example `"authorization_code"`.

**Returns**

`true` if `grantType` is in `client.AllowedGrantTypes`; otherwise `false`.

**Exceptions**

None.

### HasRedirectUri

```csharp
public static bool HasRedirectUri(this Client client, Uri uri)
```

Returns `true` when `client.RedirectUris` contains a string equal to `uri.ToString()`. The comparison uses `StringComparison.OrdinalIgnoreCase`, so it is case-insensitive.

The method compares against `uri.ToString()`, not `uri.OriginalString`. Any normalization that `Uri.ToString()` applies is included in the comparison. Registered redirect URIs must match that string form.

Returns `false` when:

- `client` is `null`.
- `uri` is `null`.
- `client.RedirectUris` is `null`.
- No registered redirect URI matches.

**Parameters**

- `client`: The client to check. May be `null`.
- `uri`: The redirect URI to look for. May be `null`.

**Returns**

`true` if the client has a redirect URI equal to `uri`, ignoring case; otherwise `false`.

**Exceptions**

None.

## Related

- [Client](Client.md): the entity these methods extend.
- [ClientExtensions](ClientExtensions.md): other `Client` extension methods. Those methods throw `ArgumentNullException` for a `null` client.
