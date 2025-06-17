# SecretsService

Provides cryptographic operations for secret management, including secure random secret generation, salted iterative hashing, verification, and token creation. The service encapsulates the hash parameters (salt, iteration count, algorithm) used for a particular secret derivation, enabling consistent verification and masking of sensitive values.

## API

### public SecretsService

Constructor. Initializes a new instance with the specified hash parameters.

- **Parameters:**
  - `string hash`: The hash algorithm name (e.g., `"SHA256"`, `"SHA512"`).
  - `string salt`: A base64-encoded salt value.
  - `int iterations`: The number of iterations for the key derivation.
- **Throws:** `ArgumentNullException` if `hash` or `salt` is null; `ArgumentException` if `hash` is empty or `iterations` is less than 1.

### public string GenerateSecureSecret

Generates a cryptographically strong random secret string.

- **Parameters:** None.
- **Returns:** A base64-encoded string of random bytes suitable for use as a secret.
- **Throws:** `System.Security.Cryptography.CryptographicException` if the underlying random number generator fails.

### public SecretHash HashSecret

Hashes a secret using the configured salt, iterations, and algorithm.

- **Parameters:**
  - `string secret`: The plaintext secret to hash.
- **Returns:** A `SecretHash` object containing the derived hash bytes and associated metadata.
- **Throws:** `ArgumentNullException` if `secret` is null; `ArgumentException` if `secret` is empty.

### public bool VerifySecret

Verifies a plaintext secret against a previously computed hash.

- **Parameters:**
  - `string secret`: The plaintext secret to verify.
  - `SecretHash storedHash`: The hash object to compare against.
- **Returns:** `true` if the secret produces a hash matching `storedHash`; otherwise `false`.
- **Throws:** `ArgumentNullException` if either argument is null.

### public string GenerateToken

Creates a time-limited or random token string for authentication or session purposes.

- **Parameters:** None.
- **Returns:** A base64-encoded token string.
- **Throws:** `System.Security.Cryptography.CryptographicException` if random generation fails.

### public static string MaskSecret

Obscures a secret string for safe display or logging.

- **Parameters:**
  - `string secret`: The secret to mask.
- **Returns:** A masked representation (e.g., retaining only the first and last few characters with asterisks in between). Returns `null` if `secret` is null; returns an empty string if `secret` is empty.
- **Throws:** None.

### public string Hash

Gets the hash algorithm name used by this instance (e.g., `"SHA256"`).

### public string Salt

Gets the base64-encoded salt value used by this instance.

### public int Iterations

Gets the number of iterations used for key derivation.

### public string Algorithm

Gets the algorithm identifier string. Typically identical to `Hash` but may include additional derivation scheme information.

### public override string ToString

Returns a string representation of the service configuration.

- **Returns:** A formatted string containing the algorithm, iteration count, and masked salt.

## Usage

### Example 1: Creating and Verifying a User Secret

```csharp
// Initialize the service with SHA512, a generated salt, and 100,000 iterations
var saltBytes = System.Security.Cryptography.RandomNumberGenerator.GetBytes(32);
var salt = Convert.ToBase64String(saltBytes);
var service = new SecretsService("SHA512", salt, 100_000);

// Generate a new secret and hash it for storage
string userSecret = service.GenerateSecureSecret();
SecretHash storedHash = service.HashSecret(userSecret);

// Later, verify a provided secret against the stored hash
bool isValid = service.VerifySecret("attempted-secret", storedHash);

Console.WriteLine($"Verification result: {isValid}");
Console.WriteLine($"Service config: {service}");
```

### Example 2: Masking Secrets for Audit Logging

```csharp
var service = new SecretsService("SHA256", "c2FsdC12YWx1ZQ==", 50_000);
string apiKey = service.GenerateSecureSecret();

// Log the masked version to avoid exposing the full key
string maskedKey = SecretsService.MaskSecret(apiKey);
Console.WriteLine($"Generated key (masked): {maskedKey}");

// Generate a short-lived token
string sessionToken = service.GenerateToken();
Console.WriteLine($"Session token (masked): {SecretsService.MaskSecret(sessionToken)}");
```

## Notes

- **Immutability:** `Hash`, `Salt`, `Iterations`, and `Algorithm` are set at construction and do not change. Instances are effectively immutable with respect to their derivation parameters.
- **Thread Safety:** All public instance methods (`GenerateSecureSecret`, `HashSecret`, `VerifySecret`, `GenerateToken`) are thread-safe; they do not mutate shared instance state. The static method `MaskSecret` is inherently thread-safe.
- **Empty and Null Handling:** `MaskSecret` gracefully handles null and empty inputs without throwing. Other methods throw `ArgumentNullException` or `ArgumentException` for null or empty secrets where hashing or verification would be meaningless.
- **Salt Encoding:** The `Salt` property returns a base64-encoded string. Consumers must ensure proper encoding when supplying salts to the constructor.
- **Iteration Count:** Higher values increase computational cost for hashing and verification, improving resistance to brute-force attacks at the expense of performance. The constructor enforces a minimum of 1 iteration.
- **SecretHash Comparison:** `VerifySecret` performs a constant-time comparison where possible to mitigate timing attacks.
