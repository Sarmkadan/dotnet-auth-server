# JwksHandler

The `JwksHandler` class provides functionality for managing and validating JSON Web Key Sets (JWKS) within the authentication server infrastructure. It encapsulates a collection of cryptographic keys (`JwkKey`) and exposes specific key properties to facilitate the retrieval of public key material and the asynchronous validation of key identifiers (`kid`) against the loaded set.

## API

### Constructors

#### `public JwksHandler()`
Initializes a new instance of the `JwksHandler` class. This constructor prepares the handler to manage a list of keys and configure specific key property filters such as Key Type (`Kty`) or Algorithm (`Alg`).

### Properties

#### `public List<JwkKey> Keys`
Gets or sets the list of JSON Web Keys managed by this handler. This collection represents the actual key set used for cryptographic operations and validation.

#### `public string? Kty`
Gets or sets the expected Key Type (e.g., "RSA", "EC"). When set, this property may be used to filter or validate keys within the `Keys` collection based on their cryptographic algorithm family.

#### `public string? Kid`
Gets or sets a specific Key ID. This property allows the handler to target or validate a single specific key within the set rather than the entire collection.

#### `public string? Use`
Gets or sets the intended use of the key (e.g., "sig" for signature, "enc" for encryption). This restricts operations to keys designated for a specific purpose.

#### `public string? Alg`
Gets or sets the specific algorithm (e.g., "RS256", "ES256") associated with the key context.

#### `public string? K`
Gets or sets the cryptographic key value, typically used for symmetric keys or octet sequences represented as a base64url-encoded string.

#### `public string? N`
Gets or sets the modulus value for RSA public keys, represented as a base64url-encoded string.

#### `public string? E`
Gets or sets the exponent value for RSA public keys, represented as a base64url-encoded string.

### Methods

#### `public async Task<JwksResponse> GetJwksAsync()`
Asynchronously retrieves the current set of keys formatted as a `JwksResponse` object.
*   **Return Value**: A `JwksResponse` containing the serialized representation of the `Keys` list.
*   **Exceptions**: May throw exceptions if the internal key state is corrupted or if serialization fails.

#### `public async Task<bool> IsValidKeyIdAsync(string? kid)`
Asynchronously validates whether a provided Key ID exists and is considered valid within the current `Keys` collection, optionally respecting the configured `Kty`, `Use`, or `Alg` filters.
*   **Parameters**:
    *   `kid`: The Key ID string to validate.
*   **Return Value**: Returns `true` if a matching and valid key is found; otherwise, returns `false`.
*   **Exceptions**: Generally does not throw for invalid inputs, returning `false` instead, unless the internal `Keys` collection is in an invalid state.

## Usage

### Example 1: Retrieving the Public Key Set
This example demonstrates initializing the handler and retrieving the full JWKS response to expose public keys to a client.

```csharp
using DotNetAuthServer.Handlers;
using DotNetAuthServer.Models;

public async Task ExposeJwksEndpoint()
{
    var handler = new JwksHandler();
    
    // Assume Keys are populated via dependency injection or configuration
    // handler.Keys = ... 

    var jwksResponse = await handler.GetJwksAsync();
    
    // Return jwksResponse as JSON to the HTTP client
    Console.WriteLine($"Exposed {jwksResponse.Keys.Count} keys.");
}
```

### Example 2: Validating a Token's Key ID
This example checks if a specific `kid` extracted from a JWT header corresponds to a valid key currently managed by the handler.

```csharp
using DotNetAuthServer.Handlers;

public async Task<bool> ValidateTokenKey(string tokenKid)
{
    var handler = new JwksHandler();
    
    // Configure specific constraints if required
    handler.Alg = "RS256";
    handler.Use = "sig";

    bool isValid = await handler.IsValidKeyIdAsync(tokenKid);

    if (!isValid)
    {
        // Handle invalid or missing key scenario
        return false;
    }

    return true;
}
```

## Notes

*   **Thread Safety**: The `Keys` property exposes a mutable `List<JwkKey>`. If the collection is modified concurrently while `GetJwksAsync` or `IsValidKeyIdAsync` is enumerating it, a runtime exception may occur. External synchronization or the use of immutable collections is recommended when modifying `Keys` in a multi-threaded environment.
*   **Null Handling**: The `IsValidKeyIdAsync` method accepts a nullable string for the `kid` parameter. Passing `null` will typically result in a `false` return value unless the implementation explicitly supports matching keys with null identifiers.
*   **Property Filtering**: Setting scalar properties like `Kty`, `Alg`, or `Use` on the handler instance acts as a contextual filter. The behavior of `IsValidKeyIdAsync` depends on whether the target key matches both the provided `kid` and these configured property constraints.
*   **RSA Specifics**: The properties `N` and `E` are specific to RSA keys. If the handler is configured with `Kty` set to "EC" (Elliptic Curve), these properties should remain null or unused to avoid logical inconsistencies during validation.
