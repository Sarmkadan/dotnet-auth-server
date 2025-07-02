# JwtTokenFormatter

The `JwtTokenFormatter` class serves as a utility for parsing, inspecting, and representing the structure of a JSON Web Token (JWT) within the `dotnet-auth-server` application. It provides structured access to token metadata, headers, payload claims, and the raw token string, enabling consistent logging, debugging, and security analysis of authentication tokens.

## API

- **`JwtTokenFormatter()`**
  Initializes a new instance of the `JwtTokenFormatter` class.

- **`InspectToken`** (`TokenInspection?`)
  Gets or sets the result of an inspection performed on the token. Returns `null` if no inspection has been associated with the formatter instance.

- **`FormatForLogging`** (`string`)
  A string representation of the token optimized for logging purposes.

- **`Header`** (`TokenHeader`)
  The header component of the JWT, containing metadata about the token.

- **`Payload`** (`TokenPayload`)
  The payload component of the JWT, containing the claims and other data.

- **`Raw`** (`string`)
  The original, unmodified raw JWT string.

- **`Alg`** (`string?`)
  The algorithm used for the token, if specified in the header.

- **`Typ`** (`string?`)
  The type of the token, if specified in the header.

- **`Kid`** (`string?`)
  The Key ID (kid) of the token, if specified in the header.

- **`Subject`** (`string?`)
  The subject (sub) claim of the token, identifying the principal.

- **`Issuer`** (`string?`)
  The issuer (iss) claim of the token, identifying the authority that issued it.

- **`Audience`** (`string?`)
  The audience (aud) claim of the token, identifying the intended recipients.

- **`IssuedAt`** (`DateTime?`)
  The time (iat) at which the token was issued.

- **`ExpiresAt`** (`DateTime?`)
  The expiration time (exp) after which the token must not be accepted.

- **`NotBefore`** (`DateTime?`)
  The time (nbf) before which the token must not be accepted.

- **`Claims`** (`Dictionary<string, List<string>>`)
  A collection of all claims extracted from the token payload, mapped by claim type to a list of values.

## Usage

### Example 1: Inspecting Token Metadata
This example demonstrates how to initialize the formatter and access basic metadata for logging.

```csharp
var tokenFormatter = new JwtTokenFormatter();
// ... assume tokenFormatter is populated from a raw string

if (tokenFormatter.ExpiresAt.HasValue && tokenFormatter.ExpiresAt < DateTime.UtcNow)
{
    Console.WriteLine("Token has expired.");
}

Console.WriteLine($"Token Subject: {tokenFormatter.Subject ?? "Unknown"}");
Console.WriteLine($"Log output: {tokenFormatter.FormatForLogging}");
```

### Example 2: Accessing Claims
This example demonstrates how to safely retrieve claims from the token's payload.

```csharp
var tokenFormatter = new JwtTokenFormatter();
// ... assume tokenFormatter is populated

if (tokenFormatter.Claims.TryGetValue("roles", out var roles))
{
    foreach (var role in roles)
    {
        Console.WriteLine($"User has role: {role}");
    }
}
```

## Notes

- **Nullability**: Properties such as `Alg`, `Subject`, `Issuer`, `Audience`, and the `DateTime?` fields may be `null` if the corresponding data is absent in the processed JWT. Callers should implement null checks to prevent `NullReferenceException` errors.
- **Data Integrity**: The `Claims` dictionary contains values parsed from the token payload. Depending on the input source, these values should be treated as untrusted and validated against expected formats before use in business logic.
- **Thread-Safety**: Instances of `JwtTokenFormatter` are intended to be immutable once fully populated. While the class members are public, callers should not modify properties of an instance shared across multiple threads to avoid race conditions.
