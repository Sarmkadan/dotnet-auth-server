# User Management Models

This document describes the request and response Data Transfer Objects (DTOs) used for user management operations in the DotnetAuthServer.

## CreateUserRequest

Represents the data required to create a new user account.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Username` | `string` | Yes | Unique username (3–50 alphanumeric characters, dots, underscores or hyphens). |
| `Email` | `string` | Yes | Valid email address for the account. |
| `Password` | `string` | Yes | Plain-text password; must meet the configured minimum-length policy. |
| `FullName` | `string?` | No | Optional display name shown in tokens and the UI. |
| `Roles` | `ICollection<string>` | No | Initial roles to assign. Roles are case-insensitive strings (e.g. "admin", "user"). Defaults to an empty collection. |

### Example

```json
{
  "username": "johndoe",
  "email": "johndoe@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "roles": ["user", "editor"]
}
```

## UpdateUserRequest

Represents the data required to update an existing user account. All properties are optional — only non-null values are applied.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `FullName` | `string?` | No | New display name. |
| `IsActive` | `bool?` | No | Activates or deactivates the account. |
| `Attributes` | `Dictionary<string, object>?` | No | ABAC attribute bag to merge into the existing attributes dictionary. Existing keys are overwritten; keys absent from this payload are kept. |

### Example

```json
{
  "fullName": "Johnathan Doe",
  "isActive": true,
  "attributes": {
    "department": "Engineering",
    "employeeId": "E12345"
  }
}
```

## AssignRoleRequest

Represents the data required to assign a role to a user.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `Role` | `string` | Yes | Role name to assign (case-insensitive). |

### Example

```json
{
  "role": "admin"
}
```

## ChangePasswordRequest

Represents the data required to change a user's password.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `CurrentPassword` | `string` | Yes | The user's current password for verification. |
| `NewPassword` | `string` | Yes | The new password; must meet the configured minimum-length policy. |

### Example

```json
{
  "currentPassword": "OldPass123!",
  "newPassword": "NewSecurePass456!"
}
```

## UserResponse

Read-only projection of a User entity for API responses. Sensitive fields (password hash, TOTP secret) are excluded.

### Properties

| Property | Type | Required | Description |
|----------|------|----------|-------------|
| `UserId` | `string` | Yes | Unique user identifier. |
| `Username` | `string` | Yes | Login username. |
| `Email` | `string` | Yes | Email address. |
| `FullName` | `string?` | No | Optional display name. |
| `IsActive` | `bool` | Yes | Whether the account is active. |
| `EmailVerified` | `bool` | Yes | Whether the email has been verified. |
| `Roles` | `ICollection<string>` | Yes | RBAC roles assigned to the user. Defaults to an empty collection. |
| `Attributes` | `Dictionary<string, object>` | Yes | ABAC attribute bag. Defaults to an empty dictionary. |
| `CreatedAt` | `DateTime` | Yes | Account creation timestamp (UTC). |
| `UpdatedAt` | `DateTime` | Yes | Last modification timestamp (UTC). |
| `LastLoginAt` | `DateTime?` | No | Last successful login timestamp (UTC), or null if never logged in. |
| `IsLocked` | `bool` | Yes | Whether the account is currently locked out. |
| `LockedUntil` | `DateTime?` | No | Lock expiry timestamp (UTC), or null when not locked. |

### Example

```json
{
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "username": "johndoe",
  "email": "johndoe@example.com",
  "fullName": "John Doe",
  "isActive": true,
  "emailVerified": true,
  "roles": ["user", "editor"],
  "attributes": {
    "department": "Engineering",
    "employeeId": "E12345"
  },
  "createdAt": "2026-09-15T10:30:00Z",
  "updatedAt": "2026-09-15T14:45:00Z",
  "lastLoginAt": "2026-09-15T14:45:00Z",
  "isLocked": false,
  "lockedUntil": null
}
```