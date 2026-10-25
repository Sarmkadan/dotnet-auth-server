# SecretsService

`SecretsService` generates random secrets and tokens, hashes secrets for storage with PBKDF2-SHA256, verifies a presented secret against a stored hash, and masks secrets for logs. It is built only on the BCL (`System.Security.Cryptography`), so it adds no NuGet dependencies.

The class is `public sealed` and lives in `src/Services/SecretsService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; it is registered as a scoped concrete type (`Program.cs`). The file also defines the public `SecretHash` class, which holds a stored hash and its metadata.

## Role in the system

- Generates client secrets and tokens.
- Produces the `SecretHash` value that is persisted in place of the plain-text secret.
- Verifies a secret presented at request time against that stored `SecretHash`.
- Masks values before they reach logs.

Logging goes through `ILogger<SecretsService>`. The service logs an invalid requested length and verification errors. It never logs the secret, the hash, or the salt.

## Constructor

### `public SecretsService(ILogger<SecretsService> logger)`

Creates the service.

**Parameters:**
- `logger` – logger for invalid-length and verification-error events.

**Exceptions:** `ArgumentNullException` if `logger` is `null`.

## Constants

These values are private and fixed. They are listed here because they determine the output of the public methods.

| Constant | Value | Meaning |
|---|---|---|
| Default secret length | `32` bytes | 256-bit random secret; default for `GenerateSecureSecret` and `GenerateToken` |
| Salt length | `16` bytes | 128-bit salt, unique per `HashSecret` call |
| PBKDF2 output length | `32` bytes | 256-bit derived key, matching SHA-256 |
| Default iterations | `10000` | Default PBKDF2 work factor for `HashSecret` |
| Hash algorithm | SHA-256 | PBKDF2 PRF |

## Generation

### `public string GenerateSecureSecret(int length = 32)`

Generates a cryptographically secure random secret from `RandomNumberGenerator`. Suitable for client secrets, API keys, and tokens.

The bytes are encoded as unpadded Base64url (`+` → `-`, `/` → `_`, `=` removed). The output is therefore about `length × 4 / 3` characters long, and it contains no characters that need escaping in URLs.

**Parameters:**
- `length` – number of random bytes, before encoding. Must be between `16` and `256` inclusive. Default `32`.

**Return value:** the Base64url-encoded secret.

**Exceptions:**
- `ArgumentException` with the message `Secret length must be between 16 and 256` if `length` is outside the range. A warning with the requested length is logged first.

### `public string GenerateToken(int length = 32)`

Same as `GenerateSecureSecret`. It forwards `length` unchanged, so the same range and exception apply. Use it where the call site means "token" rather than "secret".

**Parameters:**
- `length` – number of random bytes. Must be between `16` and `256` inclusive. Default `32`.

**Return value:** the Base64url-encoded token.

**Exceptions:** same as `GenerateSecureSecret`.

## Hashing

### `public SecretHash HashSecret(string secret, int iterations = 10000)`

Hashes a secret for storage. A new 16-byte random salt is generated on every call, so hashing the same secret twice gives different results. The derived key is computed with `Rfc2898DeriveBytes.Pbkdf2` using SHA-256 and a 32-byte output.

**Parameters:**
- `secret` – the plain-text secret. Must not be `null` or empty.
- `iterations` – PBKDF2 iteration count. Default `10000`. The value is stored in the result, so `VerifySecret` uses the same count. No range check is done here; the BCL rejects values that are not positive.

**Return value:** a `SecretHash` with:
- `Hash` – Base64 of the derived key.
- `Salt` – Base64 of the salt.
- `Iterations` – the iteration count used.
- `Algorithm` – always `"PBKDF2-SHA256"`.

**Exceptions:**
- `ArgumentException` if `secret` is `null` or empty.
- `ArgumentOutOfRangeException` from the BCL if `iterations` is not positive. This exception is not caught.

### `public bool VerifySecret(string secret, SecretHash hash)`

Checks a presented secret against a stored `SecretHash`. The method derives a key from `secret` using the salt and iteration count in `hash`, then compares it with `hash.Hash` in constant time (all bytes are always compared; the comparison does not stop at the first mismatch).

**Parameters:**
- `secret` – the presented plain-text secret. Must not be `null` or empty.
- `hash` – the stored `SecretHash` to check against.

**Return value:**
- `true` if the derived key matches `hash.Hash`.
- `false` if the secret does not match, or if the stored value cannot be used. The second case covers a `Salt` or `Hash` that is not valid Base64, a `null` `Salt` or `Hash`, and a non-positive `Iterations`. These errors are logged with `LogError` and reported as `false`.

**Exceptions:**
- `ArgumentException` if `secret` is `null` or empty.
- `ArgumentNullException` if `hash` is `null`.

Because malformed stored data returns `false` rather than throwing, callers cannot tell "wrong secret" from "corrupt record" by the return value. Check the logs when this distinction matters.

## Masking

### `public static string MaskSecret(string secret)`

Returns a masked form of a secret for logs. It keeps the first 3 and last 3 characters and replaces the middle with `***`. Short or empty input is fully masked.

**Parameters:**
- `secret` – the value to mask. May be `null`.

**Return value:**
- `"***"` if `secret` is `null`, empty, whitespace-only, or 6 characters or fewer.
- Otherwise `{first 3 chars}***{last 3 chars}`. For example, `abcdefghij` becomes `abc***hij`.

**Exceptions:** none.

## SecretHash

`SecretHash` is a public `sealed` class in the same file. It is the storage record produced by `HashSecret` and consumed by `VerifySecret`. All properties have public setters, so it can be mapped by an ORM or serializer.

| Property | Type | Default | Description |
|---|---|---|---|
| `Hash` | `string` | `""` | Base64 of the PBKDF2-derived key |
| `Salt` | `string` | `""` | Base64 of the random salt |
| `Iterations` | `int` | `10000` | PBKDF2 iteration count used to derive `Hash` |
| `Algorithm` | `string` | `"PBKDF2-SHA256"` | Name of the algorithm |

### `public override string ToString()`

Returns `SecretHash(<Algorithm>, iterations=<Iterations>)`. `Hash` and `Salt` are never included, so the value is safe to log.

## Typical usage

```csharp
// Issue a new client secret: show the plain text once, store only the hash.
var clientSecret = secretsService.GenerateSecureSecret();
var storedHash = secretsService.HashSecret(clientSecret);

// Later, verify a presented secret.
bool valid = secretsService.VerifySecret(presentedSecret, storedHash);

// Log-safe representation of a secret.
logger.LogDebug("Received secret {Masked}", SecretsService.MaskSecret(presentedSecret));
```

## Notes

- `ConstantTimeCompare` is a private helper and is not part of the public API.
- The 10,000-iteration default is the value the code uses today. It is lower than current OWASP guidance for PBKDF2-SHA256. Raising it changes new hashes only; existing `SecretHash` records keep their stored `Iterations`, so they still verify.
- Secrets and hashes are not logged by any method in this class.
