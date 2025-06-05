# AuthorizationGrant
The `AuthorizationGrant` type represents an authorization grant in the `dotnet-auth-server` project, encapsulating the details of an authorization request, including the client, user, scopes, and redirect URI. It provides properties to access these details and methods to manage the grant's state.

## API
* `GrantId`: A unique identifier for the authorization grant.
* `Code`: The authorization code issued to the client.
* `ClientId`: The identifier of the client that requested the authorization grant.
* `UserId`: The identifier of the user who authorized the grant.
* `RequestedScopes`: The scopes requested by the client.
* `GrantedScopes`: The scopes granted to the client.
* `RedirectUri`: The URI to which the client will be redirected after authorization.
* `State`: An optional state parameter to prevent CSRF attacks.
* `Nonce`: An optional nonce value to prevent replay attacks.
* `CodeChallenge`: An optional code challenge to verify the client's possession of a secret.
* `CodeChallengeMethod`: The method used to generate the code challenge.
* `ResponseType`: The type of response expected by the client.
* `ExpiresAt`: The date and time at which the authorization grant expires.
* `IsUsed`: A flag indicating whether the authorization grant has been used.
* `UsedAt`: The date and time at which the authorization grant was used, or `null` if it has not been used.
* `IsRevoked`: A flag indicating whether the authorization grant has been revoked.
* `CreatedAt`: The date and time at which the authorization grant was created.
* `IsValid`: A flag indicating whether the authorization grant is valid.
* `IsExpired`: A flag indicating whether the authorization grant has expired.
* `MarkAsUsed()`: Marks the authorization grant as used. This method does not throw any exceptions.

## Usage
The following examples demonstrate how to use the `AuthorizationGrant` type:
```csharp
// Create a new authorization grant
var grant = new AuthorizationGrant
{
    GrantId = "grant-123",
    Code = "code-123",
    ClientId = "client-123",
    UserId = "user-123",
    RequestedScopes = "scope1 scope2",
    GrantedScopes = "scope1",
    RedirectUri = "https://example.com/callback",
    ResponseType = "code"
};

// Mark the grant as used
grant.MarkAsUsed();
Console.WriteLine(grant.IsUsed); // Output: True

// Check if the grant is valid and not expired
if (grant.IsValid && !grant.IsExpired)
{
    Console.WriteLine("Grant is valid and not expired");
}
```

## Notes
When using the `AuthorizationGrant` type, consider the following edge cases:
* If the `ExpiresAt` property is set to a date and time in the past, the `IsExpired` property will be `true`.
* If the `IsUsed` property is `true`, the `UsedAt` property will be set to the date and time at which the grant was used.
* The `MarkAsUsed` method does not throw any exceptions, but it will set the `IsUsed` property to `true` and update the `UsedAt` property accordingly.
* The `AuthorizationGrant` type is not thread-safe, so it should not be accessed concurrently by multiple threads. If concurrent access is necessary, consider using synchronization mechanisms such as locks or concurrent collections.
