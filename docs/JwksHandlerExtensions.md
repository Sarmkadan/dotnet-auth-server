# JwksHandlerExtensions

`JwksHandlerExtensions` provides a collection of static extension methods that facilitate the management and querying of JSON Web Key Sets (JWKS) within the authentication server infrastructure. These utilities abstract common operations such as filtering keys by cryptographic type, searching for keys by identifier, and isolating keys designated specifically for signing operations, thereby simplifying the implementation of secure token validation and issuance processes.

## API

### GetFirstKeyAsync
Retrieves the first available JSON Web Key from the managed JWKS handler.

*   **Parameters:** `this IJwksHandler handler`
*   **Returns:** A `JwkKey` object representing the first key in the set, or `null` if the set is empty.
*   **Exceptions:** May throw exceptions related to network connectivity or deserialization if the underlying handler fails to retrieve or parse the key set.

### ContainsKeyIdAsync
Verifies whether a key with the specified identifier exists within the current JWKS.

*   **Parameters:**
    *   `this IJwksHandler handler`
    *   `string kid` – The unique identifier of the key to locate.
*   **Returns:** `true` if a key with the provided identifier exists; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `kid` is `null`.

### GetKeysByTypeAsync
Retrieves a list of all keys matching a specified key type (e.g., "RSA", "EC").

*   **Parameters:**
    *   `this IJwksHandler handler`
    *   `string keyType` – The string representation of the cryptographic key type.
*   **Returns:** A `List<JwkKey>` containing all matching keys. Returns an empty list if no matches are found.
*   **Exceptions:** Throws `ArgumentException` if `keyType` is invalid or null.

### GetSigningKeysAsync
Retrieves all keys currently configured for signing operations within the JWKS provider.

*   **Parameters:** `this IJwksHandler handler`
*   **Returns:** A `List<JwkKey>` containing keys marked for signing.
*   **Exceptions:** None explicitly thrown by the extension method; however, it inherits potential exceptions from the underlying `IJwksHandler` implementation.

## Usage

### Retrieving Signing Keys for Token Validation
```csharp
public async Task<List<JwkKey>> GetActiveSigningKeys(IJwksHandler jwksHandler)
{
    // Retrieve all keys currently designated for signing
    var signingKeys = await jwksHandler.GetSigningKeysAsync();

    return signingKeys;
}
```

### Checking for a Specific Key Identifier
```csharp
public async Task<bool> IsKeyActive(IJwksHandler jwksHandler, string keyId)
{
    // Verify if the key ID exists in the JWKS provider
    bool exists = await jwksHandler.ContainsKeyIdAsync(keyId);
    
    return exists;
}
```

## Notes

*   **Thread Safety:** These extension methods are thread-safe, provided the underlying `IJwksHandler` implementation is also thread-safe.
*   **Asynchronous Behavior:** As these methods perform I/O-bound operations (typically retrieving keys from a remote endpoint or cached store), they must be awaited to prevent thread-blocking issues.
*   **Edge Cases:** In scenarios where the JWKS provider is unavailable or returns an empty payload, these methods will behave according to their documented return types (e.g., returning `null` or an empty `List<JwkKey>`). Consumers should implement appropriate null checks when calling `GetFirstKeyAsync`.
