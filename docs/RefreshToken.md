# RefreshToken

A `RefreshToken` in `dotnet-auth-server` represents a refresh token entity used in OAuth 2.0 and OpenID Connect flows. It encapsulates token metadata, lifecycle states, and usage tracking to facilitate secure token refresh operations while maintaining auditability and revocation capabilities.

## API

### Properties

- **`TokenId`** (string)
  A unique identifier for the refresh token. Used as a primary or secondary key in storage and lookup operations. Must be non-null and non-empty.

- **`TokenHash`** (string)
  Cryptographic hash of the raw token value. Used for secure comparison and storage. Must be non-null and non-empty.

- **`ClientId`** (string)
  Identifier of the OAuth client that requested the token. Used for client-specific validation and scoping. Must be non-null and non-empty.

- **`UserId`** (string)
  Identifier of the user associated with the token. Used for user-specific authorization checks. Must be non-null and non-empty.

- **`GrantedScopes`** (string)
  Space-separated list of OAuth scopes granted to the token. Used to enforce scope-based access control during refresh. Must be non-null (may be empty string).

- **`Version`** (int)
  Monotonically increasing version number for optimistic concurrency control during updates. Must be non-negative.

- **`PreviousTokenHash`** (string?)
  Hash of the prior refresh token in a token rotation chain. Used to detect replay attacks and validate rotation sequence. May be null if no prior token exists.

- **`ExpiresAt`** (DateTime)
  Absolute expiration timestamp of the token. Used to determine validity and trigger automatic revocation. Must be in the future when created.

- **`IsRevoked`** (bool)
  Indicates whether the token has been explicitly revoked. Used in access checks and cleanup logic. Defaults to `false`.

- **`RevokedAt`** (DateTime?)
  Timestamp when the token was revoked. Used for audit trails and reporting. Must be non-null if `IsRevoked` is `true`.

- **`RevocationReason`** (string?)
  Human-readable reason for revocation. Used for support and compliance logging. May be null if not provided.

- **`UsageCount`** (int)
  Number of times the token has been used to obtain a new access token. Used for rate limiting and anomaly detection. Must be non-negative.

- **`LastUsedAt`** (DateTime?)
  Timestamp of the most recent token usage. Used for idle timeout and security monitoring. May be null if never used.

- **`IssuedToDeviceId`** (string?)
  Identifier of the device to which the token was issued. Used for device-specific authorization and conditional access. May be null if not device-bound.

- **`CreatedAt`** (DateTime)
  Timestamp when the token was created. Used for lifecycle management and audit purposes. Must be non-null and in the past.

- **`UpdatedAt`** (DateTime)
  Timestamp of the last modification to the token record. Used for concurrency control and change tracking. Must be non-null and in the past.

- **`IsValid`** (bool)
  Computed property indicating whether the token is currently valid for use. Returns `true` if not revoked, not expired, and not otherwise disqualified. Read-only.

- **`IsExpired`** (bool)
  Computed property indicating whether the token has passed its expiration time. Returns `true` if `ExpiresAt` is in the past. Read-only.

### Methods

- **`RecordUsage()`**
  Increments the `UsageCount` and updates the `LastUsedAt` timestamp to the current UTC time. Used to track token activity and enforce usage limits. No parameters. No return value. No exceptions.

- **`Revoke(string? reason)`**
  Marks the token as revoked, sets `RevokedAt` to the current UTC time, and stores the optional `reason`. Used to invalidate tokens upon compromise, policy violation, or user logout. Accepts an optional human-readable reason. No return value. No exceptions.

## Usage
