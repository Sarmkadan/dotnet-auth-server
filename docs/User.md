# User

Represents an authenticated user in the `dotnet-auth-server` system, containing core identity information, security state, and extensible attributes for application-specific data.

## API

### Properties

#### `UserId`
A unique identifier for the user, typically a GUID or database-generated string.

#### `Username`
The user's login identifier, used for authentication and display purposes.

#### `Email`
The user's primary email address, used for account recovery and notifications.

#### `FullName`
The user's full name, optional and nullable. May be updated by the user or administrators.

#### `PasswordHash`
The hashed representation of the user's password, stored securely. Never exposed in plaintext.

#### `EmailVerified`
Indicates whether the user has confirmed their email address via a verification token.

#### `IsActive`
Determines if the account is enabled and usable. Inactive accounts cannot log in.

#### `Roles`
A collection of role names (e.g., "admin", "user") assigned to the user, enabling role-based authorization.

#### `Attributes`
A dictionary of application-specific key-value pairs, allowing flexible user metadata storage.

#### `CreatedAt`
The timestamp when the user record was created, typically set once and immutable.

#### `UpdatedAt`
The timestamp of the last modification to the user record.

#### `LastLoginAt`
The timestamp of the user's most recent successful login, or `null` if never logged in.

#### `FailedLoginAttempts`
The number of consecutive failed login attempts, used to detect brute-force attacks.

#### `LockedUntil`
The timestamp until which the account is locked due to too many failed attempts, or `null` if unlocked.

#### `IsValid`
A computed property indicating whether the account is active, not locked, and otherwise eligible for login.

#### `IsLocked`
A computed property indicating whether the account is currently locked due to failed login attempts or manual lockout.

### Methods

#### `LockAccount()`
Locks the user account immediately by setting `LockedUntil` to a distant future date and resetting `FailedLoginAttempts` to zero.

#### `RecordFailedLogin()`
Increments `FailedLoginAttempts` and sets `LockedUntil` if the threshold is exceeded. Does not modify `LastLoginAt`.

#### `RecordSuccessfulLogin()`
Updates `LastLoginAt` to the current UTC time, resets `FailedLoginAttempts` to zero, and clears `LockedUntil`.

## Usage
