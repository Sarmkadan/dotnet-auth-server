# PkceValidationService

The `PkceValidationService` provides utilities for Proof Key for Code Exchange (PKCE) validation in OAuth 2.0 flows. It generates cryptographically secure code verifiers and challenges, and validates verifiers against stored challenges to prevent authorization code interception attacks.

## API

### `PkceValidationService`

Initializes a new instance of the PKCE validation service. No parameters are required as dependencies are resolved internally.

### `string GenerateCodeVerifier()`

Generates a cryptographically secure code verifier for PKCE. The verifier is a high-entropy random string encoded in URL-safe base64 without padding.

- **Returns**: A 43–128 character URL-safe base64 string.
- **Throws**: `InvalidOperationException` if cryptographic random number generation fails.

### `string GenerateCodeChallenge(string codeVerifier)`

Derives a code challenge from a given code verifier using SHA-256 and URL-safe base64 encoding.

- **Parameters**:
  - `codeVerifier` (string): The code verifier to transform. Must be 43–128 characters.
- **Returns**: The derived code challenge as a URL-safe base64 string.
- **Throws**: `ArgumentException` if `codeVerifier` is null, empty, or outside the valid length range.

### `bool ValidateCodeVerifier(string codeVerifier, string expectedChallenge)`

Validates a code verifier against an expected code challenge by recomputing the challenge and comparing it to the stored value.

- **Parameters**:
  - `codeVerifier` (string): The verifier to validate.
  - `expectedChallenge` (string): The challenge previously generated and stored.
- **Returns**: `true` if the recomputed challenge matches `expectedChallenge`; otherwise, `false`.
- **Throws**: `ArgumentException` if either parameter is null or empty.

### `bool IsPkceRequired`

Gets a value indicating whether PKCE is required for the current authorization context (e.g., confidential client, public client, or enforced policy).

- **Returns**: `true` if PKCE is required; otherwise, `false`.

### `bool IsValidChallenge(string challenge)`

Determines whether a given challenge string is valid for PKCE (i.e., correctly formatted and within length bounds).

- **Parameters**:
  - `challenge` (string): The challenge to validate.
- **Returns**: `true` if the challenge is valid; otherwise, `false`.
- **Throws**: `ArgumentException` if `challenge` is null.

## Usage
