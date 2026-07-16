// existing content ...

## WebAuthnCredential

The `WebAuthnCredential` entity represents a WebAuthn/FIDO2 public-key credential associated with a user account,
enabling passwordless and phishing-resistant authentication via platform authenticators.

### Usage Example

```csharp
using DotnetAuthServer.Domain.Entities;

// Create a new WebAuthn credential
var credential = new WebAuthnCredential
{
    Id = Guid.NewGuid().ToString(),
    CredentialId = "cred-123",
    PublicKey = new byte[] { 1, 2, 3 }, // Example public key bytes
    Algorithm = -7, // ES256
    UserId = "user-123",
    FriendlyName = "Security Key",
    AaGuid = "00000000-0000-0000-0000-000000000000",
    BackupEligible = true,
    BackedUp = false,
    IsActive = true
};

// Check some properties
Console.WriteLine($"Credential ID: {credential.CredentialId}");
Console.WriteLine($"User ID: {credential.UserId}");
Console.WriteLine($"Algorithm: {credential.Algorithm}");
Console.WriteLine($"Created At: {credential.CreatedAt}");
Console.WriteLine($"Last Used At: {credential.LastUsedAt?.ToString() ?? "null"}");
Console.WriteLine($"Is Active: {credential.IsActive}");
```
