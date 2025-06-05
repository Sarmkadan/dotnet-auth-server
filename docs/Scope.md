# Scope

Represents an OAuth 2.0/OpenID Connect scope definition, including claims, roles, and consent requirements for authorization.

## API

### `public string ScopeId`
Unique identifier for the scope. Used in authorization requests and token generation.

### `public string DisplayName`
Human-readable name for the scope, suitable for UI presentation.

### `public string Description`
Detailed explanation of the scope's purpose and usage.

### `public bool IsRequired`
Indicates whether the scope is mandatory for the associated client or user.

### `public bool RequiresConsent`
Determines if user consent is required before the scope can be granted.

### `public bool IsOpenIdScope`
Specifies whether the scope is an OpenID Connect-specific scope (e.g., `openid`, `profile`).

### `public bool IsActive`
Controls whether the scope is currently available for use in authorization flows.

### `public ICollection<string> IdTokenClaims`
List of claims to include in the ID token when this scope is granted.

### `public ICollection<string> AccessTokenClaims`
List of claims to include in the access token when this scope is granted.

### `public ICollection<string> AllowedRoles`
Roles authorized to request or be granted this scope.

### `public DateTime CreatedAt`
Timestamp of when the scope was created.

### `public DateTime UpdatedAt`
Timestamp of the last modification to the scope.

### `public bool IsValid`
Indicates whether the scope configuration is valid (e.g., required fields are set).

### `public bool CanUserAccessScope`
Determines if the current user has permission to access this scope.

### `public IEnumerable<string> GetAllClaims()`
Returns all claims associated with the scope, combining ID token and access token claims.

## Usage
