# UserManagementService

`UserManagementService` is the administrative facade over user accounts. It serves privileged, server-side callers such as an admin API or background jobs: listing, searching, creating, updating, deleting, role management, and lock/unlock. End-user self-service operations (authentication, password change, self-registration flows) live in `UserService`.

The class is `public sealed` and lives in `src/Services/UserManagementService.cs`, namespace `DotnetAuthServer.Services`. It has no interface. Register and inject the concrete type, as with the other services.

## Role in the system

- Reads and writes go through `IUserRepository`. Account creation and role changes are delegated to a `UserService` built per call, so the same validation and password hashing rules apply.
- Deleting an account also revokes its refresh tokens (`IRefreshTokenRepository`) and sessions (`IUserSessionRepository`).
- Lock and unlock events are written to the security log through `SecurityEventLog`. Other operations are written to the regular logger at `Information`.
- Every method that takes a `userId` throws the same not-found error if the user does not exist (see [Errors](#errors)).

## Constructor

### `public UserManagementService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, IUserSessionRepository sessionRepository, ILogger<UserManagementService> logger, ILoggerFactory loggerFactory, AuthServerOptions options)`

Creates the service from its collaborators.

- **Parameters**:
  - `userRepository`: Persistence for `User` entities.
  - `refreshTokenRepository`: Used to revoke all refresh tokens when a user is deleted, and passed to the internal `UserService`.
  - `sessionRepository`: Used to revoke all sessions when a user is deleted.
  - `logger`: Logger for administrative actions.
  - `loggerFactory`: Creates the logger for the internal `UserService` each time one is built.
  - `options`: Server options. `options.PasswordPolicy` configures the password validator.
- **Throws**: The constructor does not null-check its arguments.

## Methods

### `public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)`

Returns every registered user.

- **Parameters**:
  - `cancellationToken`: Cancels the repository call.
- **Return value**: The users as `UserResponse` projections. The projection is lazy (`Select`), so mapping runs when the sequence is enumerated.
- **Throws**: `OperationCanceledException` if cancelled.

### `public async Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)`

Returns one user by ID.

- **Parameters**:
  - `userId`: The user's ID.
  - `cancellationToken`: Cancels the repository call.
- **Return value**: The matching `UserResponse`.
- **Throws**: `AuthServerException` with status `404` if no user has that ID.

### `public async Task<IEnumerable<UserResponse>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)`

Searches users by username, email, or full name.

- **Parameters**:
  - `query`: The search text. If it is `null`, empty, or whitespace, the method returns all users (same as `GetAllUsersAsync`).
  - `cancellationToken`: Cancels the repository call.
- **Return value**: The matching users as `UserResponse` projections.
- **Throws**: `OperationCanceledException` if cancelled.

### `public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)`

Creates a user account and assigns its initial roles.

- **Parameters**:
  - `request`: The account data. `Username`, `Email`, and `Password` are required. `FullName` is optional. `Roles` lists initial roles and may be empty.
  - `cancellationToken`: Cancels the call.
- **Return value**: The created user as a `UserResponse`.
- **Behavior**:
  - The account is created active, with a new GUID `UserId`.
  - The username must be 3-50 characters of allowed characters and must be unique. The email must be well formed and unique.
  - The password must satisfy the configured password policy. The policy check uses the username.
  - Each entry in `Roles` is assigned in order. Role matching is case-insensitive, so duplicates that differ only by case are assigned once.
  - The account is saved before roles are assigned. If a role assignment fails, the account remains, with the roles assigned up to that point. There is no rollback.
- **Throws**:
  - `AuthServerException` with status `400` (`invalid_request`) for an invalid username or email, a duplicate username or email, or a password that breaks the policy.
  - `AuthServerException` with status `500` (`server_error`) for an unexpected failure during creation or role assignment.
  - `ArgumentException` if `Username`, `Email`, or `Password` is `null`, empty, or whitespace.

### `public async Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)`

Updates the mutable profile fields of an existing user. Only the fields set in the request are changed.

- **Parameters**:
  - `userId`: The user to update.
  - `request`: The changes to apply. See the rules below.
  - `cancellationToken`: Cancels the call.
- **Return value**: The updated user as a `UserResponse`.
- **Behavior**:
  - `FullName` is changed only when it is not `null`, empty, or whitespace.
  - `IsActive` is changed only when it has a value. Setting it to `false` deactivates the account. It does not revoke existing tokens.
  - `Attributes` is merged into the existing attribute bag. Keys in the request overwrite existing keys. Keys not in the request are kept. Nothing is removed.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `OperationCanceledException` if cancelled.

### `public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)`

Permanently deletes a user account.

- **Parameters**:
  - `userId`: The user to delete.
  - `cancellationToken`: Cancels the call.
- **Behavior**: Revokes all of the user's refresh tokens and sessions with the reason `Account deleted`, then removes the user. The steps run in that order and are not wrapped in a transaction. If a later step fails, earlier revocations are not undone.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `OperationCanceledException` if cancelled.

### `public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)`

Assigns a role to a user.

- **Parameters**:
  - `userId`: The user to change.
  - `role`: The role name. Matching is case-insensitive.
  - `cancellationToken`: Cancels the call.
- **Behavior**: Idempotent. If the role is already present, nothing is saved. The administrative log entry is written either way.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `ArgumentException` if `role` is `null`, empty, or whitespace. `AuthServerException` with status `500` on an unexpected storage failure.

### `public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)`

Removes a role from a user.

- **Parameters**:
  - `userId`: The user to change.
  - `role`: The role name to remove.
  - `cancellationToken`: Cancels the call.
- **Behavior**: Idempotent. If the user does not have the role, the user is still saved and the log entry is still written.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `ArgumentException` if `role` is `null`, empty, or whitespace. `AuthServerException` with status `500` on an unexpected storage failure.

### `public async Task LockUserAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default)`

Locks a user account for a fixed duration.

- **Parameters**:
  - `userId`: The user to lock.
  - `duration`: How long the lock lasts from now (UTC).
  - `cancellationToken`: Cancels the call.
- **Behavior**: Sets the lock expiry to now plus `duration` and resets the failed-login counter to `0`. Logs an account-locked security event with the expiry time.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `OperationCanceledException` if cancelled.

### `public async Task UnlockUserAsync(string userId, CancellationToken cancellationToken = default)`

Unlocks a user account.

- **Parameters**:
  - `userId`: The user to unlock.
  - `cancellationToken`: Cancels the call.
- **Behavior**: Clears the lock expiry and resets the failed-login counter to `0`. Logs an account-unlocked security event. Unlocking an account that is not locked is allowed.
- **Throws**: `AuthServerException` with status `404` if the user does not exist. `OperationCanceledException` if cancelled.

## Errors

Errors use `AuthServerException` with the codes below. `ErrorHandlingMiddleware` converts them into the OAuth `{"error","error_description"}` JSON response.

| Condition | Error code | Status |
|---|---|---|
| User ID not found | `invalid_request` | `404` |
| Invalid username/email, duplicate username/email, password policy failure | `invalid_request` | `400` |
| Unexpected failure during account creation or role assignment | `server_error` | `500` |

`ArgumentException` (and not `AuthServerException`) is thrown for blank required string arguments. Cancellation surfaces as `OperationCanceledException` and is not wrapped.

## Returned data

Methods return `UserResponse`, a read-only projection of `User`. The password hash and TOTP secret are never included. The fields are `UserId`, `Username`, `Email`, `FullName`, `IsActive`, `EmailVerified`, `Roles`, `Attributes`, `CreatedAt`, `UpdatedAt`, `LastLoginAt`, `IsLocked`, and `LockedUntil`.

`IsLocked` is computed when the response is built. An expired lock reads as not locked.

## Example

```csharp
var request = new CreateUserRequest
{
    Username = "jdoe",
    Email = "jdoe@example.com",
    Password = "a-long-initial-password",
    FullName = "Jane Doe",
    Roles = ["user"]
};

var created = await userManagement.CreateUserAsync(request, cancellationToken);

await userManagement.AssignRoleAsync(created.UserId, "auditor", cancellationToken);
await userManagement.LockUserAsync(created.UserId, TimeSpan.FromMinutes(30), cancellationToken);
```

## Known limitations

- Account creation is not transactional. A failed role assignment leaves a partially set-up account behind.
- `DeleteUserAsync` is not transactional. Revocation can succeed and the deletion can fail, leaving the user present with no active tokens or sessions.
- `UpdateUserAsync` cannot remove attribute keys. Use the attribute bag to overwrite values only.
- The service does not check whether the caller is authorized. Access control must be enforced by the caller (admin API or job).
