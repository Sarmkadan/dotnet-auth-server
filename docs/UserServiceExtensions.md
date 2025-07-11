# UserServiceExtensions

`UserServiceExtensions` provides a set of convenience extension methods for managing users and handling authentication logic within the `dotnet-auth-server` infrastructure. These methods abstract complex user-related operations, such as role-based user management, bulk creation, and authentication attempts, to simplify interaction with the underlying user storage system.

## API

### CreateUserWithRoleAsync
Creates a new user and assigns them an initial role.
- **Parameters:** `IUserService` service, `User` user, `string` role.
- **Returns:** A `Task<User>` representing the created user.
- **Throws:** `ArgumentNullException` if parameters are null; `InvalidOperationException` if user creation fails.

### CreateUsersBulkAsync
Creates a collection of users in a single operation.
- **Parameters:** `IUserService` service, `IEnumerable<User>` users.
- **Returns:** A `Task<IReadOnlyList<User>>` containing the created users.
- **Throws:** `ArgumentNullException` if parameters are null; `Exception` if the batch creation fails.

### HasRole
Determines if a user possesses a specific role.
- **Parameters:** `User` user, `string` role.
- **Returns:** `bool` - `true` if the user has the role, `false` otherwise.

### GetUsersByRoleAsync
Retrieves a collection of users associated with a specific role.
- **Parameters:** `IUserService` service, `string` role.
- **Returns:** A `Task<IReadOnlyList<User>>` of matching users.
- **Throws:** `ArgumentNullException` if parameters are null.

### WithAttributesAsync
Updates or adds attributes to an existing user object.
- **Parameters:** `IUserService` service, `User` user, `IDictionary<string, string>` attributes.
- **Returns:** A `Task<User>` representing the updated user.
- **Throws:** `ArgumentNullException` if parameters are null.

### TryAuthenticateAsync
Attempts to authenticate a user based on provided credentials.
- **Parameters:** `IUserService` service, `string` username, `string` password.
- **Returns:** A `Task<(User? User, bool Success)>` tuple where `User` is the authenticated user (or null) and `Success` indicates if authentication succeeded.

## Usage

```csharp
// Example 1: Creating a user with a specific role
var newUser = new User { Username = "jdoe" };
var createdUser = await userService.CreateUserWithRoleAsync(newUser, "admin");

// Example 2: Authenticating a user
var (user, success) = await userService.TryAuthenticateAsync("jdoe", "securePassword123");
if (success)
{
    Console.WriteLine($"Welcome, {user!.Username}");
}
else
{
    Console.WriteLine("Authentication failed.");
}
```

## Notes

- **Thread Safety:** As static extension methods, these methods themselves are thread-safe. However, they rely on the underlying `IUserService` implementation. Ensure that the service implementation registered in the dependency injection container is thread-safe for concurrent access.
- **Edge Cases:** Methods accepting `IUserService` will throw `ArgumentNullException` if the service instance is null. `HasRole` performs a string comparison for roles and is case-sensitive depending on the underlying implementation's role configuration.
- **Error Handling:** Async methods (`*Async`) may throw exceptions related to network connectivity or data store failures; these should be handled by the caller or configured middleware.
