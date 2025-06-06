# ConsentRequest

Represents a request for user consent in an OAuth or OpenID Connect authorization flow. This type encapsulates the necessary information to determine whether a user has approved or denied access to specific scopes, along with metadata such as client details, user agent, and IP address for auditing or security purposes.

## API

### `public string? UserId`
Gets or sets the unique identifier of the user providing consent. This value may be `null` if the user is not authenticated or if the consent request is anonymous.

### `public string? ClientId`
Gets or sets the identifier of the client application requesting consent. This value may be `null` if the client is not yet identified or if the request is invalid.

### `public ICollection<string> GrantedScopes`
Gets the collection of scope identifiers that the user has granted consent for. The collection may be empty if no scopes are explicitly granted.

### `public bool Approved`
Gets or sets a value indicating whether the user has approved the consent request. A value of `true` signifies explicit approval; `false` indicates denial.

### `public string? DenialReason`
Gets or sets the reason provided by the user for denying consent. This value is typically populated only when `Approved` is `false`.

### `public bool RememberConsent`
Gets or sets a value indicating whether the user's consent decision should persist across sessions. When `true`, the consent may be stored for future requests without re-prompting the user.

### `public string? IpAddress`
Gets or sets the IP address of the user's device at the time of consent. Used for logging, fraud detection, or security auditing.

### `public string? UserAgent`
Gets or sets the user agent string of the browser or client used to submit the consent request. Useful for device fingerprinting or troubleshooting.

### `public string GetScopesString()`
Returns a space-separated string representation of the `GrantedScopes` collection. Returns an empty string if `GrantedScopes` is `null` or empty.

### `public bool IsValid`
Gets a value indicating whether the consent request contains sufficient data to be considered valid. A valid request typically requires non-null `UserId` and `ClientId`, and at least one scope in `GrantedScopes` when `Approved` is `true`.

## Usage

### Example 1: Validating a Consent Request
```csharp
var consent = new ConsentRequest
{
    UserId = "user-123",
    ClientId = "client-456",
    GrantedScopes = new List<string> { "openid", "profile" },
    Approved = true,
    RememberConsent = true,
    IpAddress = "192.168.1.1",
    UserAgent = "Mozilla/5.0"
};

if (consent.IsValid)
{
    Console.WriteLine("Consent is valid. Scopes: " + consent.GetScopesString());
}
else
{
    Console.WriteLine("Invalid consent request.");
}
```

### Example 2: Handling Denied Consent
```csharp
var consent = new ConsentRequest
{
    UserId = "user-789",
    ClientId = "client-456",
    Approved = false,
    DenialReason = "User does not trust the application"
};

if (!consent.Approved && !string.IsNullOrEmpty(consent.DenialReason))
{
    Log.Warning($"Consent denied for user {consent.UserId}: {consent.DenialReason}");
}
```

## Notes

- **Null Handling**: `UserId`, `ClientId`, `DenialReason`, `IpAddress`, and `UserAgent` may be `null`. Consumers must validate these values before use.
- **Thread Safety**: This type is not thread-safe. Concurrent modifications to `GrantedScopes` or other properties may result in undefined behavior.
- **Edge Cases**: 
  - `IsValid` returns `false` if `Approved` is `true` but `GrantedScopes` is empty or `null`.
  - `GetScopesString()` returns an empty string when `GrantedScopes` is `null` or contains no elements.
  - `RememberConsent` has no effect if `Approved` is `false`.
