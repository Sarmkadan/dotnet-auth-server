# UserService

`UserService` (`src/Services/UserService.cs`) is a sealed, scoped domain service that handles user authentication and user management: login with password verification and lockout, account creation with username/email/password validation, profile updates, password changes, and RBAC role assignment.

It depends on `IUserRepository` for persistence, `IRefreshTokenRepository` for revoking sessions on password change, `AuthServerOptions` for the password policy and lockout threshold, and `ILogger<UserService>` for logging. Methods that fail with an unexpected (non-cancellation) exception wrap it in an `AuthServerException` with `server_error` / HTTP 500, so callers only need to handle `AuthServerException` for protocol-level failures.

## API

### `UserService(IUserRepository userRepository, IRefreshTokenRepository refreshTokenRepository, AuthServerOptions options, ILogger<UserService> logger)`
Initializes a new user service. The password validator is created internally from `options.PasswordPolicy`.

- **Parameters**:
  - `userRepository` – Repository used to look up, create, and update users.
  - `refreshTokenRepository` – Repository used to revoke a user's refresh tokens when the password changes.
  - `options` – Server options; supplies the password policy and the failed-login lockout threshold (`FailedLoginAttemptThreshold`).
  - `logger` – Logger for authentication, lifecycle, and error events.
- **Exceptions**: `ArgumentNullException` if any argument is `null`.

### `Task<User> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)`
Authenticates a user with a username and password.

On success, the user's successful login is recorded (`RecordSuccessfulLogin`) and the user is persisted.

On a failed password, the failed attempt is recorded and persisted. When the attempt reaches `FailedLoginAttemptThreshold`, the account is locked and a lockout security event is logged. Subsequent attempts against a locked account are rejected before the password is checked.

- **Parameters**:
  - `username` – The username to authenticate. Must not be null, empty, or whitespace.
  - `password` – The plain-text password. Must not be null, empty, or whitespace.
  - `cancellationToken` – Cancels the operation.
- **Return value**: The authenticated `User`.
- **Exceptions**:
  - `ArgumentException` – `username` or `password` is null or whitespace (thrown before any repository call).
  - `AuthServerException` with `invalid_grant` / 401 – Unknown username, or wrong password. Both cases return the same message (`Invalid credentials`) so the response does not reveal whether the username exists.
  - `AuthServerException` with `access_denied` / 403 – The account is locked (`Account is locked due to too many failed login attempts`) or inactive (`User account is inactive`).
  - `AuthServerException` with `server_error` / 500 – An unexpected error occurred. The original exception is attached as the inner exception.
  - `OperationCanceledException` is propagated unchanged.

Password verification accepts bcrypt hashes (`$2...`). Legacy unsalted base64(SHA-256) hashes from before the bcrypt migration are still accepted.

### `Task<User> CreateUserAsync(string username, string email, string password, string? fullName = null, CancellationToken cancellationToken = default)`
Creates a new active user with a generated `UserId` (a GUID string). The password is hashed with bcrypt (work factor 12) before storage.

Validation runs in this order:

1. Username format: 3–50 characters, only `a-z`, `A-Z`, `0-9`, `.`, `_`, `-`.
2. Email format: must parse as a `System.Net.Mail.MailAddress`, and the parsed address must equal the input exactly.
3. Password against the configured `PasswordPolicy`, with the username passed in so it can be checked against the password.
4. Username must not already exist.
5. Email must not already be registered.

- **Parameters**:
  - `username` – The desired username. Must not be null, empty, or whitespace.
  - `email` – The user's email address. Must not be null, empty, or whitespace.
  - `password` – The plain-text password. Must not be null, empty, or whitespace.
  - `fullName` – Optional display name; stored as-is.
  - `cancellationToken` – Cancels the operation.
- **Return value**: The `User` returned by `IUserRepository.CreateAsync`.
- **Exceptions**:
  - `ArgumentException` – `username`, `email`, or `password` is null or whitespace.
  - `AuthServerException` with `invalid_request` / 400 – Username format is invalid, email format is invalid, the password violates the policy (the message joins all policy errors with `; `), the username already exists (`Username already exists`), or the email is already registered (`Email already registered`).
  - `AuthServerException` with `server_error` / 500 – The created user fails the entity's internal validity check, or an unexpected error occurred.
  - `OperationCanceledException` is propagated unchanged.

### `Task<User> UpdateUserAsync(User user, string? fullName = null, Dictionary<string, object>? attributes = null, CancellationToken cancellationToken = default)`
Updates a user's profile. The changes are applied to the passed-in `User` instance and then persisted through `IUserRepository.UpdateAsync`.

- `fullName` is applied only when it is not null or whitespace. Passing `null` or whitespace leaves the current value unchanged.
- Each entry in `attributes` is written into `user.Attributes`. Existing keys are overwritten and other keys are kept. Keys are not removed.

- **Parameters**:
  - `user` – The user to update. Modified in place.
  - `fullName` – Optional new display name.
  - `attributes` – Optional custom attributes to merge into the user's attributes.
  - `cancellationToken` – Cancels the operation.
- **Return value**: The `User` returned by `IUserRepository.UpdateAsync`.
- **Exceptions**:
  - `ArgumentNullException` – `user` is `null`.
  - `AuthServerException` with `server_error` / 500 – The repository update failed or another unexpected error occurred.
  - `OperationCanceledException` is propagated unchanged.

### `Task ChangePasswordAsync(User user, string currentPassword, string newPassword, CancellationToken cancellationToken = default)`
Changes a user's password after verifying the current one.

On success, the new password is hashed and stored, the failed-login counter is reset, any lock is cleared, and all of the user's refresh tokens are revoked with the reason `Password changed`.

The service keeps no password history. The only history check is the one in `PasswordPolicy`: when `CheckAgainstHistory` is enabled, the new password is compared against the single `HistoryCheckPassword` value configured there.

- **Parameters**:
  - `user` – The user whose password is changed. Modified in place.
  - `currentPassword` – The user's current plain-text password. Must not be null, empty, or whitespace.
  - `newPassword` – The new plain-text password. Must not be null, empty, or whitespace. Checked against the password policy with the user's username.
  - `cancellationToken` – Cancels the operation.
- **Exceptions**:
  - `ArgumentNullException` – `user` is `null`.
  - `ArgumentException` – `currentPassword` or `newPassword` is null or whitespace.
  - `AuthServerException` with `invalid_grant` / 401 – `currentPassword` does not match.
  - `AuthServerException` with `invalid_request` / 400 – `newPassword` violates the password policy.
  - `AuthServerException` with `server_error` / 500 – An unexpected error occurred. Note that the password may already be updated if the failure happened after the user update, for example during refresh token revocation.
  - `OperationCanceledException` is propagated unchanged.

### `Task AssignRoleAsync(User user, string role, CancellationToken cancellationToken = default)`
Adds a role to the user's `Roles` collection. Role comparison is case-insensitive. If the user already has the role, the method returns without persisting anything.

- **Parameters**:
  - `user` – The user to update. Modified in place.
  - `role` – The role name to assign. Must not be null, empty, or whitespace.
  - `cancellationToken` – Cancels the operation.
- **Exceptions**:
  - `ArgumentNullException` – `user` is `null`.
  - `ArgumentException` – `role` is null or whitespace.
  - `AuthServerException` with `server_error` / 500 – The repository update failed or another unexpected error occurred.
  - `OperationCanceledException` is propagated unchanged.

### `Task RemoveRoleAsync(User user, string role, CancellationToken cancellationToken = default)`
Removes a role from the user's `Roles` collection and persists the change. Removing a role the user does not have is not an error. The change is still persisted in that case.

- **Parameters**:
  - `user` – The user to update. Modified in place.
  - `role` – The role name to remove. Must not be null, empty, or whitespace.
  - `cancellationToken` – Cancels the operation.
- **Exceptions**:
  - `ArgumentNullException` – `user` is `null`.
  - `ArgumentException` – `role` is null or whitespace.
  - `AuthServerException` with `server_error` / 500 – The repository update failed or another unexpected error occurred.
  - `OperationCanceledException` is propagated unchanged.

## Error summary

| Method | Argument errors | `invalid_grant` 401 | `access_denied` 403 | `invalid_request` 400 | `server_error` 500 |
|---|---|---|---|---|---|
| `AuthenticateAsync` | `ArgumentException` | Unknown user or wrong password | Locked or inactive | – | Yes |
| `CreateUserAsync` | `ArgumentException` | – | – | Format, policy, duplicate username or email | Yes |
| `UpdateUserAsync` | `ArgumentNullException` | – | – | – | Yes |
| `ChangePasswordAsync` | `ArgumentNullException`, `ArgumentException` | Wrong current password | – | Policy violation | Yes |
| `AssignRoleAsync` | `ArgumentNullException`, `ArgumentException` | – | – | – | Yes |
| `RemoveRoleAsync` | `ArgumentNullException`, `ArgumentException` | – | – | – | Yes |

## Related

- [AuthServerException](AuthServerException.md)
- [PasswordValidationService](PasswordValidationService.md)
- [AuthServerOptions](AuthServerOptions.md)
- [IRefreshTokenRepository](IRefreshTokenRepository.md)
