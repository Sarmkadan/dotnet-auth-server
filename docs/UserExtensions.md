# UserExtensions

The `UserExtensions` class provides a set of static extension methods for interacting with user identity objects within the `dotnet-auth-server` ecosystem. These utilities simplify common tasks such as role-based access control, attribute management, and the retrieval of user-specific metadata, ensuring consistent handling of user claims and identity information throughout the authentication and authorization pipeline.

## API

### `HasRole(this ClaimsPrincipal user, string role)`
Checks if the specified user principal possesses a claim that maps to the given role.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check), `role` (The role string to search for).
*   **Returns:** `true` if the user has the specified role; otherwise, `false`.

### `HasAnyRole(this ClaimsPrincipal user, params string[] roles)`
Determines if the user principal possesses at least one of the provided roles.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check), `roles` (An array of roles to evaluate).
*   **Returns:** `true` if the user has at least one role from the provided list; otherwise, `false`.

### `GetAttribute<T>(this ClaimsPrincipal user, string key)`
Retrieves a typed attribute value associated with the user principal, typically stored within claims.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check), `key` (The attribute name).
*   **Returns:** The deserialized value of type `T` if found and convertible; otherwise, the default value of `T`.

### `SetAttribute(this ClaimsPrincipal user, string key, object value)`
Updates or adds an attribute to the user principal by modifying the underlying claim set.
*   **Parameters:** `user` (The `ClaimsPrincipal` to modify), `key` (The attribute name), `value` (The value to associate with the key).

### `IsAdmin(this ClaimsPrincipal user)`
A specialized check to determine if the user principal has administrative privileges.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check).
*   **Returns:** `true` if the user is identified as an administrator; otherwise, `false`.

### `GetDisplayName(this ClaimsPrincipal user)`
Extracts the display name of the user from their claims.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check).
*   **Returns:** The user's display name, or a default fallback if the claim is missing.

### `CanAuthenticate(this ClaimsPrincipal user)`
Validates whether the user principal satisfies the criteria to be considered authenticated within the current context.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check).
*   **Returns:** `true` if the user is authorized to authenticate; otherwise, `false`.

### `SecondsSinceLastLogin(this ClaimsPrincipal user)`
Calculates the duration in seconds since the user's last recorded login event.
*   **Parameters:** `user` (The `ClaimsPrincipal` to check).
*   **Returns:** A `long?` representing the elapsed seconds, or `null` if the last login claim is unavailable.

## Usage

### Role-Based Access Control
```csharp
if (User.HasRole("Editor") || User.HasAnyRole("Admin", "SuperUser"))
{
    // Grant access to restricted resource
}
```

### Retrieving and Storing User Attributes
```csharp
// Retrieve a custom user preference
var theme = User.GetAttribute<string>("PreferredTheme") ?? "Light";

// Update a user claim
User.SetAttribute("LastActionTimestamp", DateTime.UtcNow.Ticks);
```

## Notes

*   **Thread Safety:** As these methods operate on `ClaimsPrincipal`, they are subject to the thread-safety characteristics of that object. In most authentication contexts, `ClaimsPrincipal` is treated as read-only after creation; modifications via `SetAttribute` should be performed with caution in multi-threaded scenarios to prevent race conditions.
*   **Claim Mapping:** The functionality relies on specific claim types configured within the auth server. If the underlying authentication scheme does not map claims expected by these extensions, methods like `GetDisplayName` or `SecondsSinceLastLogin` may return unexpected default values.
*   **Type Conversion:** `GetAttribute<T>` utilizes standard type conversion. If the claim value cannot be cast or deserialized into the specified type `T`, the method will return `default(T)` rather than throwing an exception.
