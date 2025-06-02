# Consent
The `Consent` type in the `dotnet-auth-server` project represents a user's consent to a client's request for access to their resources. It encapsulates the details of the consent, including the scopes granted, the user and client involved, and the status of the consent. This type provides methods to manage the consent, such as granting, denying, or revoking access.

## API
### Properties
* `ConsentId`: A unique identifier for the consent.
* `UserId`: The identifier of the user who granted the consent.
* `ClientId`: The identifier of the client to which the consent was granted.
* `GrantedScopes`: A string representing the scopes to which the user has granted access.
* `Status`: The current status of the consent, represented by a `ConsentStatus` enum value.
* `ExpiresAt`: The date and time at which the consent expires, or `null` if it does not expire.
* `IsOfflineConsent`: A boolean indicating whether the consent is for offline access.
* `DenialReason`: The reason for denying the consent, or `null` if the consent was granted.
* `IpAddress`: The IP address from which the consent was granted, or `null` if not recorded.
* `UserAgent`: The user agent string from which the consent was granted, or `null` if not recorded.
* `CreatedAt`: The date and time at which the consent was created.
* `UpdatedAt`: The date and time at which the consent was last updated.
* `IsValidAndApproved`: A boolean indicating whether the consent is valid and approved.
* `IsExpired`: A boolean indicating whether the consent has expired.

### Methods
* `Grant()`: Grants the consent to the client. Throws an exception if the consent is already granted or denied.
* `Deny()`: Denies the consent to the client. Throws an exception if the consent is already granted or denied.
* `Revoke()`: Revokes the consent. Throws an exception if the consent is already revoked.
* `HasScopeConsent(string scope)`: Returns a boolean indicating whether the consent includes the specified scope.
* `GetGrantedScopes()`: Returns an enumerable collection of strings representing the scopes to which the user has granted access.

## Usage
```csharp
// Example 1: Granting consent
var consent = new Consent
{
    ConsentId = "consent-123",
    UserId = "user-123",
    ClientId = "client-123",
    GrantedScopes = "read write",
    Status = ConsentStatus.Pending
};

consent.Grant();
Console.WriteLine(consent.Status); // Output: Granted

// Example 2: Checking scope consent
var consent = new Consent
{
    ConsentId = "consent-123",
    UserId = "user-123",
    ClientId = "client-123",
    GrantedScopes = "read write",
    Status = ConsentStatus.Granted
};

var hasReadScope = consent.HasScopeConsent("read");
Console.WriteLine(hasReadScope); // Output: True
```

## Notes
* The `Grant`, `Deny`, and `Revoke` methods modify the `Status` property and may throw exceptions if the consent is already in an incompatible state.
* The `HasScopeConsent` method checks whether the specified scope is included in the `GrantedScopes` string.
* The `GetGrantedScopes` method returns an enumerable collection of scopes, which can be used to iterate over the granted scopes.
* The `Consent` type is not thread-safe, and concurrent access to its properties and methods may result in inconsistent or unexpected behavior. It is recommended to synchronize access to the `Consent` instance when used in a multi-threaded environment.
