# UserManagementController

The `UserManagementController` provides a RESTful API interface for performing administrative user lifecycle and security operations within the `dotnet-auth-server` system. This controller facilitates user discovery, account creation, profile updates, account deletion, role-based access control management, and account status toggling (locking/unlocking).

## API

### Constructors

*   **`public UserManagementController()`**
    Initializes a new instance of the `UserManagementController`.

### Actions

*   **`public async Task<IActionResult> GetUsersAsync`**
    Retrieves a paginated or filtered list of registered users.
    *   **Returns:** An `ActionResult` containing a collection of user objects.

*   **`public async Task<IActionResult> GetUserAsync`**
    Retrieves the details of a specific user identified by a unique identifier.
    *   **Parameters:** Expects a unique user identifier (e.g., GUID or string ID).
    *   **Returns:** An `ActionResult` containing the user profile or a `NotFound` result if the user does not exist.

*   **`public async Task<IActionResult> CreateUserAsync`**
    Creates a new user account in the system.
    *   **Parameters:** Expects a model containing user registration details.
    *   **Returns:** An `ActionResult` indicating success or validation failure.

*   **`public async Task<IActionResult> UpdateUserAsync`**
    Updates the profile information of an existing user.
    *   **Parameters:** Expects a unique user identifier and a model containing updated user details.
    *   **Returns:** An `ActionResult` indicating success or failure.

*   **`public async Task<IActionResult> DeleteUserAsync`**
    Permanently removes a user account from the system.
    *   **Parameters:** Expects a unique user identifier.
    *   **Returns:** An `ActionResult` indicating success or failure.

*   **`public async Task<IActionResult> AssignRoleAsync`**
    Assigns a specific security role to a user.
    *   **Parameters:** Expects a unique user identifier and role definition.
    *   **Returns:** An `ActionResult` indicating success or failure.

*   **`public async Task<IActionResult> RemoveRoleAsync`**
    Revokes a security role from a user.
    *   **Parameters:** Expects a unique user identifier and role definition.
    *   **Returns:** An `ActionResult` indicating success or failure.

*   **`public async Task<IActionResult> LockUserAsync`**
    Disables a user account, preventing further authentication attempts.
    *   **Parameters:** Expects a unique user identifier.
    *   **Returns:** An `ActionResult` indicating success.

*   **`public async Task<IActionResult> UnlockUserAsync`**
    Re-enables a locked user account.
    *   **Parameters:** Expects a unique user identifier.
    *   **Returns:** An `ActionResult` indicating success.

## Usage

### Example 1: Creating a New User
```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    // Implementation would typically inject a service to handle the request
    return await _userManagementController.CreateUserAsync(request);
}
```

### Example 2: Locking an Account for Security
```csharp
[HttpPost("{userId}/lock")]
public async Task<IActionResult> LockUser(string userId)
{
    // Locking a user account upon suspicious activity detection
    return await _userManagementController.LockUserAsync(userId);
}
```

## Notes

*   **Thread Safety:** As an ASP.NET Core controller, `UserManagementController` is instantiated per request. Ensure that any injected services or dependencies are either stateless or appropriately thread-safe for concurrent request handling.
*   **Authorization:** All actions within this controller require appropriate administrative privileges. Ensure that the controller or individual methods are decorated with the necessary `[Authorize]` attributes configured to require administrative scopes or roles.
*   **Error Handling:** The methods return `IActionResult` to facilitate standard HTTP response handling. Implementations should return appropriate HTTP status codes (e.g., `404 Not Found`, `400 Bad Request`, `403 Forbidden`) based on the outcome of the underlying identity operations.
