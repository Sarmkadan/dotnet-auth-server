# JwksHandler

**Namespace:** `DotnetAuthServer.Handlers`

Builds the JSON Web Key Set (JWKS, RFC 7517) served at `GET /.well-known/jwks.json`. The set describes the key this server signs with, so resource servers can match a token's `kid` header to a key. The handler caches the set for 24 hours.

The published key is a symmetric (`oct`) key that carries only metadata: `kty`, `kid`, `use` and `alg`. The key material is never included, because the JWKS is a public document. Clients that validate HS256 tokens therefore still need the shared secret through another channel.

`sealed`. Stateless apart from its constructor-time dependencies. It is registered as scoped in DI (`Program.cs`), and the endpoint resolves it per request.

---

## Constructor

### `JwksHandler(AuthServerOptions options, ICacheService cacheService, ILogger<JwksHandler> logger)`

Stores the dependencies. The constructor performs no null checks.

| Parameter | Type | Description |
|---|---|---|
| `options` | `AuthServerOptions` | Supplies `JwtSigningKey` (the Base64-encoded key the JWK is derived from) and `JwtAlgorithm` (the `alg` value). |
| `cacheService` | `ICacheService` | Stores the generated set under the cache key `jwks`. |
| `logger` | `ILogger<JwksHandler>` | Logger for JWKS activity. |

---

## Methods

### `Task<JwksResponse> GetJwksAsync(CancellationToken cancellationToken = default)`

Returns the current key set. It reads the `jwks` entry from the cache first. On a miss, it generates the set from the signing key and stores it with a 24-hour expiry.

| Parameter | Type | Description |
|---|---|---|
| `cancellationToken` | `CancellationToken` | Passed to the cache calls. |

**Returns:** A `JwksResponse`. Its `Keys` list holds zero or one `JwkKey`.

**Behavior:**

1. Logs at information level and reads the cached `JwksResponse`.
2. If a cached value exists, returns it.
3. Otherwise generates a new set (see [Key generation](#key-generation)), stores it with `SetAsync` for `TimeSpan.FromHours(24)`, and returns it.

The method does not throw for key problems. An invalid signing key produces an empty `Keys` list (see [Key generation](#key-generation)). That empty set is cached like any other result, so the problem persists for up to 24 hours.

**Example:**

```csharp
var handler = scope.ServiceProvider.GetRequiredService<JwksHandler>();
JwksResponse jwks = await handler.GetJwksAsync(cancellationToken);

// Serialized by the endpoint as: { "keys": [ { "kty": "oct", "kid": "...", "use": "sig", "alg": "HS256" } ] }
```

---

### `Task<bool> IsValidKeyIdAsync(string keyId, CancellationToken cancellationToken = default)`

Reports whether `keyId` matches the `kid` of a key in the current set. It calls `GetJwksAsync`, so it uses the cache in the same way.

| Parameter | Type | Description |
|---|---|---|
| `keyId` | `string` | The key ID to look for. Compared with `==`, which is case-sensitive. |
| `cancellationToken` | `CancellationToken` | Passed to `GetJwksAsync`. |

**Returns:** `true` if a key in the set has a matching `Kid`; otherwise `false`. An empty set returns `false`.

---

### `override string ToString()`

Returns a single-line summary of the keys in the cache, for example `JwksHandler { Keys = Kty = oct, Kid = ..., Use = sig, Alg = HS256, K =  }`.

The method reads the cache synchronously (`.Result`), so it blocks the calling thread. It does not generate the set. If the cache is empty, the output contains no keys. Use it for logging and debugging only, and do not rely on its exact format.

---

## Key generation

These steps run on a cache miss. They are private, but they determine what `GetJwksAsync` returns.

1. `options.JwtSigningKey` is decoded with `Convert.FromBase64String`. A value that is not valid Base64 throws, the error is logged at error level, and the set is empty.
2. The SHA-256 hash of the decoded bytes is computed and encoded as unpadded Base64url. The first 16 characters of that string become the `kid`.
3. One `JwkKey` is created with these values:

| Field | Value |
|---|---|
| `kty` | `"oct"` |
| `kid` | First 16 characters of the Base64url SHA-256 thumbprint of the decoded key |
| `use` | `"sig"` |
| `alg` | `options.JwtAlgorithm`, or `"HS256"` if it is `null` |
| `k`, `n`, `e` | Not set (`null`) |

The `kid` identifies the key but does not reveal it, since a SHA-256 hash of a high-entropy secret cannot be reversed in practice.

The set holds at most one key. Key rotation (several keys in one set) is not implemented. The code comments mention it as future work.

---

## Related Types

Both types are public and declared in the same file as the handler.

### `JwksResponse`

**Namespace:** `DotnetAuthServer.Handlers`

The JWKS document. Mutable POCO.

| Property | Type | JSON name | Description |
|---|---|---|---|
| `Keys` | `List<JwkKey>` | `keys` | The public keys in the set. Initialized to an empty list. |

### `JwkKey`

**Namespace:** `DotnetAuthServer.Handlers`

A single JSON Web Key (RFC 7517). Mutable POCO with `get; set;` properties. All properties are nullable.

| Property | Type | JSON name | Description |
|---|---|---|---|
| `Kty` | `string?` | `kty` | Key type. Set to `"oct"` by this handler. |
| `Kid` | `string?` | `kid` | Key identifier. |
| `Use` | `string?` | `use` | Intended use, `"sig"` or `"enc"`. Set to `"sig"` by this handler. |
| `Alg` | `string?` | `alg` | Algorithm the key is intended for. |
| `K` | `string?` | `k` | Symmetric key material. Never set by this handler. |
| `N` | `string?` | `n` | RSA modulus. Never set by this handler. |
| `E` | `string?` | `e` | RSA public exponent. Never set by this handler. |

#### `override string ToString()`

Returns `JwkKey { Kty = ..., Kid = ..., Use = ..., Alg = ..., K = ... }`. It includes `K`, the symmetric key material, so do not log it if `K` is ever set. This handler never sets it.

---

## Endpoint

`Program.cs` maps the handler to `GET /.well-known/jwks.json` (endpoint name `GetJwks`). It returns `GetJwksAsync` as JSON with `Results.Json`. The metadata documents advertise the same URL as `jwks_uri`.

---

## Usage Notes

- The handler does not check the `kid` in incoming tokens. Clients must compare it against this set themselves.
- Changing `JwtSigningKey` or `JwtAlgorithm` does not refresh the set until the cached entry expires after 24 hours.
- The `alg` value comes from configuration without a check against the algorithm the server actually uses. Keep `JwtAlgorithm` consistent with the token issuer.
- The key ID is derived from the signing key. Because the key is never published, the `kid` is the only value clients can use to match it.

---

## See Also

- [TokenRevocationHandler](TokenRevocationHandler.md): validates access tokens with the same signing key.
- [TokenIntrospectionHandler](TokenIntrospectionHandler.md): checks token status on the server side.
- [ICacheService](ICacheService.md): cache used to store the generated set.
- [AuthServerOptions](AuthServerOptions.md): source of `JwtSigningKey` and `JwtAlgorithm`.
- [docs/ARCHITECTURE.md](ARCHITECTURE.md): design overview and known limitations.
