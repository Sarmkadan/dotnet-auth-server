# UserManagementService

The `UserManagementService` class provides the core business logic for managing user accounts, roles, and account status within the `dotnet-auth-server` application. It encapsulates operations such as creating, retrieving, updating, deleting users, assigning and removing roles, and locking or unlocking user accounts. All public methods are asynchronous and return `UserResponse` objects (or collections thereof) that represent the user data exposed to callers.

## API

### `public UserManagementService(/* dependencies */)`

Initializes a new instance of the service. The constructor typically accepts dependencies such as a database context, a user store, and an authorization service. The exact parameters are implementation‑specific and are resolved by dependency injection.

### `public async Task<IEnumerable<UserResponse>> GetAllUsersAsync()`

Retrieves all users in the system.

- **Returns**: A collection of `UserResponse` objects representing every registered user.
- **Throws**: `UnauthorizedAccessException` if the caller does not have the required permission to list users.

### `public async Task<UserResponse> GetUserByIdAsync(string userId)`

Retrieves a single user by its unique identifier.

- **Parameters**:
  - `userId` – The identifier of the user to retrieve.
- **Returns**: A `UserResponse` for the specified user.
- **Throws**: `ArgumentNullException` if `userId` is `null` or empty.  
  `KeyNotFoundException` if no user with the given identifier exists.

### `public async Task<IEnumerable<UserResponse>> SearchUsersAsync(string query)`

Searches for users whose properties (e.g., username, email) match the provided query string.

- **Parameters**:
  - `query` – A search term used to filter users. May be `null` or empty, in which case all users are returned.
- **Returns**: A collection of matching `UserResponse` objects.
- **Throws**: `UnauthorizedAccessException` if the caller lacks search permissions.

### `public async Task<UserResponse> CreateUserAsync(CreateUserRequest request)`

Creates a new user account based on the provided request data.

- **Parameters**:
  - `request` – A `CreateUserRequest` object containing required fields (e.g., username, email, password).
- **Returns**: A `UserResponse` representing the newly created user.
- **Throws**: `ArgumentNullException` if `request` is `null`.  
  `ArgumentException` if the request data is invalid (e.g., missing required fields, invalid email format).  
  `InvalidOperationException` if a user with the same username or email already exists.

### `public async Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request)`

Updates an existing user’s profile information.

- **Parameters**:
  - `userId` – The identifier of the user to update.
  - `request` – An `UpdateUserRequest` containing the fields to modify.
- **Returns**: A `UserResponse` reflecting the updated state.
- **Throws**: `ArgumentNullException` if `userId` or `request` is `null`.  
  `KeyNotFoundException` if the user does not exist.  
  `ArgumentException` if the update data is invalid.

### `public async Task DeleteUserAsync(string userId)`

Permanently removes a user account from the system.

- **Parameters**:
  - `userId` – The identifier of the user to delete.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `userId` is `null` or empty.  
  `KeyNotFoundException` if the user does not exist.

### `public async Task AssignRoleAsync(string userId, string roleName)`

Assigns a role to the specified user.

- **Parameters**:
  - `userId` – The identifier of the user.
  - `roleName` – The name of the role to assign.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if either parameter is `null` or empty.  
  `KeyNotFoundException` if the user or the role does not exist.  
  `InvalidOperationException` if the user already has the specified role.

### `public async Task RemoveRoleAsync(string userId, string roleName)`

Removes a role from the specified user.

- **Parameters**:
  - `userId` – The identifier of the user.
  - `roleName` – The name of the role to remove.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if either parameter is `null` or empty.  
  `KeyNotFoundException` if the user or the role does not exist.  
  `InvalidOperationException` if the user does not currently have the specified role.

### `public async Task LockUserAsync(string userId)`

Locks the user account, preventing the user from signing in.

- **Parameters**:
  - `userId` – The identifier of the user to lock.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `userId` is `null` or empty.  
  `KeyNotFoundException` if the user does not exist.  
  `InvalidOperationException` if the account is already locked.

### `public async Task UnlockUserAsync(string userId)`

Unlocks a previously locked user account, restoring sign‑in capability.

- **Parameters**:
  - `userId` – The identifier of the user to unlock.
- **Returns**: A `Task` representing the asynchronous operation.
- **Throws**: `ArgumentNullException` if `userId` is `null` or empty.  
  `KeyNotFoundException` if the user does not exist.  
  `InvalidOperationException` if the account is not currently locked.

## Usage

### Example 1: Creating a user and assigning a role

```csharp
public async Task<UserResponse> CreateAdminUserAsync(UserManagementService userService)
{
    var createRequest = new CreateUserRequest
    {
        Username = "jdoe",
        Email = "jdoe@example.com",
        Password = "SecureP@ss1"
    };

    UserResponse newUser = await userService.CreateUserAsync(createRequest);
    await userService.AssignRoleAsync(newUser.Id, "Administrator");
    return newUser;
}
```

### Example 2: Searching for users and locking an account

```csharp
public async Task LockInactiveUsersAsync(UserManagementService userService, string searchPattern)
{
    IEnumerable<UserResponse> users = await userService.SearchUsersAsync(searchPattern);
    foreach (var user in users)
    {
        if (user.LastLoginDate < DateTime.UtcNow.AddMonths(-6))
        {
            await userService.LockUserAsync(user.Id);
        }
    }
}
```

## Notes

- **Parameter validation**: All methods that accept a user identifier or role name throw `ArgumentNullException` when the value is `null` or empty. Callers should validate inputs before invoking these methods.
- **Duplicate detection**: `CreateUserAsync` throws `InvalidOperationException` if a user with the same username or email already exists. The exact uniqueness constraints depend on the underlying identity store.
- **Role existence**: `AssignRoleAsync` and `RemoveRoleAsync` throw `KeyNotFoundException` if the specified role does not exist in the system. Roles must be created separately before they can be assigned.
- **Account state transitions**: `LockUserAsync` and `UnlockUserAsync` throw `InvalidOperationException` if the account is already in the requested state. Check the current lock status via `GetUserByIdAsync` if needed.
- **Thread safety**: This service is not guaranteed to be thread‑safe. It relies on shared dependencies (e.g., an Entity Framework `DbContext`) that are not designed for concurrent use. In an ASP.NET Core application, register the service as **scoped** to ensure a single instance per request. Avoid sharing the same instance across multiple threads or long‑running background operations without proper synchronization.
- **Authorization**: Several methods (e.g., `GetAllUsersAsync`, `SearchUsersAsync`) may throw `UnauthorizedAccessException` if the caller lacks the required permission. The exact authorization policy is configured externally and is not enforced by the service itself.
