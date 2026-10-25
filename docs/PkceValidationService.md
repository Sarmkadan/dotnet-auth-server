# PkceValidationService

`PkceValidationService` implements Proof Key for Code Exchange (PKCE, RFC 7636). It creates code verifiers and code challenges, checks a presented verifier against a stored challenge, and decides whether PKCE is required for a client. PKCE protects the authorization code flow against code interception, which matters most for public clients (mobile apps and single-page apps) that cannot keep a secret.

The class is `public sealed` and lives in `src/Services/PkceValidationService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; `Program.cs` registers it as a scoped concrete type. It depends on `AuthServerOptions` (for `RequirePkceForAllClients`) and `ILogger<PkceValidationService>`. It uses only the BCL (`System.Security.Cryptography`), so it adds no NuGet dependencies.

## Role in the system

- Generates verifiers for clients and tests that need them.
- Derives the `S256` or `plain` challenge that is stored with an authorization code.
- Compares a verifier from the token request with the stored challenge.
- Decides whether PKCE is mandatory for a client, based on its confidentiality and server policy.
- Checks that a challenge has a well-formed value for its method.

## Constructor

### `public PkceValidationService(AuthServerOptions options, ILogger<PkceValidationService> logger)`

Creates the service.

**Parameters:**
- `options` – server options. Only `RequirePkceForAllClients` is read.
- `logger` – logger for validation warnings and errors.

**Exceptions:** `ArgumentNullException` if `options` or `logger` is `null`.

## Constants

These values are private and fixed. They define the verifier length rules used by every method.

| Constant | Value | Meaning |
|---|---|---|
| `MinCodeVerifierLength` | `43` | Shortest allowed verifier (RFC 7636 §4.1) |
| `MaxCodeVerifierLength` | `128` | Longest allowed verifier (RFC 7636 §4.1) |

## Generation

### `public string GenerateCodeVerifier()`

Generates a code verifier from 32 bytes of `RandomNumberGenerator` output. The bytes are encoded as unpadded base64url (`+` → `-`, `/` → `_`, `=` removed). The result is always 43 characters, which is the RFC minimum.

**Return value:** the code verifier.

**Exceptions:**
- `InvalidOperationException` with the message `Failed to generate valid code verifier` if the generated length is outside 43–128. The service logs the length first. With 32 random bytes this check does not fail in practice.
- Failures from the random number generator are not caught and propagate to the caller.

### `public string GenerateCodeChallenge(string codeVerifier, string method = "S256")`

Derives a code challenge from a verifier.

- `"S256"` (default): the SHA-256 hash of the verifier's UTF-8 bytes, encoded as unpadded base64url. The result is always 43 characters.
- `"plain"`: returns the verifier unchanged.

The method name is case-sensitive.

**Parameters:**
- `codeVerifier` – the verifier to transform. It must pass the format check described under [Verifier format](#verifier-format).
- `method` – the challenge method, `"S256"` or `"plain"`. Default `"S256"`.

**Return value:** the code challenge.

**Exceptions:**
- `ArgumentException` with the message `Code verifier is required` if `codeVerifier` is `null`, empty, or whitespace.
- `ArgumentException` with the message `Code verifier must contain only unreserved characters [A-Z][a-z][0-9]_-.` if the verifier fails the format check. A warning is logged first. The message is shorter than the real rule; the check also accepts `.` and `~`.
- `NotSupportedException` with the message `Method '<method>' is not supported` if `method` is neither `"S256"` nor `"plain"`. The format check runs before the method check, so an invalid verifier with an unknown method raises `ArgumentException`.

## Validation

### `public bool ValidateCodeVerifier(string? codeVerifier, string? codeChallenge, string method = "S256")`

Recomputes the challenge from `codeVerifier` using `method` and compares it with `codeChallenge`.

**Parameters:**
- `codeVerifier` – the verifier presented by the client, usually from the token request.
- `codeChallenge` – the challenge stored with the authorization code.
- `method` – the challenge method stored with the authorization code, `"S256"` or `"plain"`. Default `"S256"`.

**Return value:** `true` if the recomputed challenge equals `codeChallenge`; otherwise `false`.

**Behavior:** this method does not throw for bad input. The following all return `false`:
- `codeVerifier` or `codeChallenge` is `null`, empty, or whitespace.
- `codeVerifier` fails the format check.
- `method` is not supported. The `NotSupportedException` from `GenerateCodeChallenge` is caught and logged as an error.

The comparison uses ordinary string equality (`==`), not a constant-time comparison.

### `public bool IsPkceRequired(bool isConfidentialClient)`

Decides whether a client must use PKCE.

**Parameters:**
- `isConfidentialClient` – `true` if the client can keep a secret; `false` for a public client.

**Return value:**
- `true` if `RequirePkceForAllClients` is set. This applies to every client.
- Otherwise, `true` if the client is public, and `false` if it is confidential.

`AuthServerOptions.RequirePkceForAllClients` defaults to `true`, so with default options this method returns `true` for every client.

### `public bool IsValidChallenge(string? codeChallenge, string method)`

Checks that a challenge is well formed for its method. It does not compare against any verifier.

**Parameters:**
- `codeChallenge` – the challenge to check.
- `method` – `"S256"` or `"plain"`.

**Return value:** `true` if the challenge is valid for `method`; otherwise `false`. Never throws.

| Method | Valid when |
|---|---|
| `"S256"` | Length is exactly 43. Characters are not checked. |
| `"plain"` | Passes the verifier format check (43–128 characters, allowed set below). |
| any other value | Never valid. |

`null`, empty, and whitespace challenges are always invalid.

## Verifier format

A verifier is valid when both of these hold:
- Its length is between 43 and 128 characters inclusive.
- Every character is one of `A–Z`, `a–z`, `0–9`, `-`, `.`, `_`, or `~` (the RFC 7636 unreserved set).

This check is private (`IsValidCodeVerifier`). It is used by `GenerateCodeChallenge` and `ValidateCodeVerifier`, and by `IsValidChallenge` for the `"plain"` method.

## Logging

The service logs through `ILogger<PkceValidationService>`:

| Level | When |
|---|---|
| Error | A generated verifier has an invalid length (`GenerateCodeVerifier`). |
| Warning | An invalid verifier format (`GenerateCodeChallenge`, `ValidateCodeVerifier`). |
| Warning | A missing verifier or challenge, or a verifier/challenge mismatch (`ValidateCodeVerifier`). |
| Error | An unexpected exception during validation (`ValidateCodeVerifier`). |

Log entries never include the verifier or the challenge. The one exception is the generated verifier's length, which is logged as a number.

## Usage

```csharp
// Client side (or a test): create a verifier and its S256 challenge.
var verifier = pkce.GenerateCodeVerifier();
var challenge = pkce.GenerateCodeChallenge(verifier);

// Authorization endpoint: check the challenge, then store it with the code.
if (!pkce.IsValidChallenge(challenge, "S256"))
{
    // reject the request
}

// Token endpoint: compare the presented verifier with the stored challenge.
var isValid = pkce.ValidateCodeVerifier(request.CodeVerifier, storedChallenge, storedMethod);
```
