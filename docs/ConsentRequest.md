# ConsentRequest

Represents a user's decision for an OAuth 2.0 or OpenID Connect consent request. The model identifies the user and client, records whether consent was approved, and carries the scopes and request metadata used when the decision is persisted.

Namespace: `DotnetAuthServer.Domain.Models`

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `UserId` | `string?` | `null` | Identifier of the user making the consent decision. |
| `ClientId` | `string?` | `null` | Identifier of the client requesting access. |
| `GrantedScopes` | `ICollection<string>` | Empty collection | Scopes granted by the user. An approved request must contain at least one scope. |
| `Approved` | `bool` | `false` | Indicates whether the user approved the request. |
| `DenialReason` | `string?` | `null` | Optional reason for denying consent. The model does not require this value when `Approved` is `false`. |
| `RememberConsent` | `bool` | `false` | Indicates whether the consent decision should be remembered. |
| `IpAddress` | `string?` | `null` | IP address associated with the request. |
| `UserAgent` | `string?` | `null` | User-agent value associated with the request. |

## Methods

### `GetScopesString()`

Returns the entries in `GrantedScopes` joined by a single space, preserving their collection order.

```csharp
var request = new ConsentRequest
{
    GrantedScopes = ["openid", "profile", "email"]
};

var scopes = request.GetScopesString();
// "openid profile email"
```

An empty collection produces an empty string. This method does not trim, filter, validate, or remove duplicate scope values.

### `IsValid()`

Returns `true` when:

- `UserId` is not null, empty, or whitespace;
- `ClientId` is not null, empty, or whitespace; and
- the request is denied, or the approved request contains at least one granted scope.

In equivalent form:

```csharp
!string.IsNullOrWhiteSpace(UserId) &&
!string.IsNullOrWhiteSpace(ClientId) &&
(!Approved || GrantedScopes.Count > 0)
```

`IsValid()` checks the collection count only. It does not verify that individual scope entries are non-empty or authorized for the client.

### `ToString()`

Returns a diagnostic representation containing `UserId`, `ClientId`, the comma-separated granted scopes, `Approved`, `DenialReason`, and `RememberConsent`. `IpAddress` and `UserAgent` are not included.

Because the result can contain user, client, scope, and denial information, avoid writing it to logs unless the destination and data-retention policy are appropriate.

## Usage

### Approved consent

```csharp
var request = new ConsentRequest
{
    UserId = "user-123",
    ClientId = "client-456",
    GrantedScopes = ["openid", "profile"],
    Approved = true,
    RememberConsent = true,
    IpAddress = "192.0.2.10",
    UserAgent = "ExampleBrowser/1.0"
};

if (!request.IsValid())
{
    throw new InvalidOperationException("The consent request is invalid.");
}
```

### Denied consent

```csharp
var request = new ConsentRequest
{
    UserId = "user-123",
    ClientId = "client-456",
    Approved = false,
    DenialReason = "User declined access"
};

// Valid: a denied request does not require granted scopes.
var isValid = request.IsValid();
```

## Notes

- `GrantedScopes` is initialized to an empty mutable collection.
- `Approved` and `RememberConsent` default to `false`.
- The model performs only structural validation. Callers remain responsible for confirming that the user, client, and scopes are valid and permitted.
- Instances are mutable and should not be shared across threads while being modified.
