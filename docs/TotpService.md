# TotpService

The `TotpService` class provides functionality for managing Time‑Based One‑Time Password (TOTP) multi‑factor authentication within the authentication server. It handles the generation of provisioning data, verification of user‑supplied codes, and enabling or disabling MFA for a user account.

## API

### `TotpService()`
Creates a new instance of the service.  
**Parameters:** none.  
**Return value:** a ready‑to‑use `TotpService` object.  
**Exceptions:** none under normal conditions.

### `public async Task<MfaSetupResponse> InitiateSetupAsync(string userId, string issuer = null, string accountName = null)`
Starts the MFA enrollment process for a user.  
**Parameters:**  
- `userId` – identifier of the user for whom MFA is being configured (must not be null or empty).  
- `issuer` – optional name of the service or organization that will appear in the authenticator app.  
- `accountName` – optional user‑friendly account name; if omitted, `userId` is used.  
**Return value:** a `Task` that completes with an `MfaSetupResponse` containing the generated secret key, a Base32‑encoded string, and a provisioning URI suitable for QR code generation.  
**Exceptions:**  
- `ArgumentException` if `userId` is null, empty, or whitespace.  
- `InvalidOperationException` if MFA is already enabled for the specified user.  
- `ObjectDisposedException` if the service instance has been disposed.

### `public async Task ConfirmSetupAsync(string userId, string code)`
Confirms that the user possesses the TOTP device by validating a one‑time code during enrollment.  
**Parameters:**  
- `userId` – identifier of the user whose setup is being confirmed.  
- `code` – the 6‑digit TOTP code entered by the user.  
**Return value:** a completed `Task`.  
**Exceptions:**  
- `ArgumentException` if either parameter is null or empty.  
- `InvalidOperationException` if no pending setup exists for the user.  
- `UnauthorizedAccessException` if the supplied code does not match the expected value.

### `public async Task<bool> VerifyAsync(string userId, string code)`
Verifies a TOTP code for an already‑enabled MFA factor.  
**Parameters:**  
- `userId` – identifier of the user.  
- `code` – the TOTP code to validate.  
**Return value:** a `Task<bool>` where `true` indicates a valid code and `false` indicates an invalid or expired code.  
**Exceptions:**  
- `ArgumentException` if `userId` or `code` is null or empty.  
- `InvalidOperationException` if MFA is not enabled for the user.

### `public async Task<MfaStatusResponse> GetStatusAsync(string userId)`
Retrieves the current MFA status for a user.  
**Parameters:**  
- `userId` – identifier of the user.  
**Return value:** a `Task<MfaStatusResponse>` containing properties such as `IsEnabled` and, if enabled, the date/time of enrollment.  
**Exceptions:**  
- `ArgumentException` if `userId` is null or empty.  
- `KeyNotFoundException` if no user record exists for the identifier.

### `public async Task DisableMfaAsync(string userId)`
Disables MFA for a user, removing the associated secret and any enrollment data.  
**Parameters:**  
- `userId` – identifier of the user.  
**Return value:** a completed `Task`.  
**Exceptions:**  
- `ArgumentException` if `userId` is null or empty.  
- `InvalidOperationException` if MFA is not currently enabled for the user.

### `public bool VerifyTotpCode(string secretKey, string code)`
Validates a TOTP code against a supplied secret key without consulting persistent storage.  
**Parameters:**  
- `secretKey` – the raw Base32‑encoded secret associated with the user's TOTP device.  
- `code` – the TOTP code to verify.  
**Return value:** `true` if the code matches the current or adjacent time step; otherwise `false`.  
**Exceptions:**  
- `ArgumentNullException` if either parameter is null.  
- `FormatException` if `secretKey` is not a valid Base32 string.

### `public static string BuildProvisioningUri(string issuer, string accountName, byte[] secretKey)`
Constructs an `otpauth://` URI that can be encoded into a QR code for provisioning a TOTP authenticator app.  
**Parameters:**  
- `issuer` – the service or organization name.  
- `accountName` – the user account identifier (often an email address).  
- `secretKey` – the raw secret bytes (typically the output of a cryptographically secure RNG).  
**Return value:** a URI string suitable for QR code generation.  
**Exceptions:**  
- `ArgumentNullException` if any parameter is null.  
- `ArgumentException` if `issuer` or `accountName` is empty, or if `secretKey` is empty.

### `public static string EncodeBase32(byte[] input)`
Encodes an arbitrary byte array into a Base32 string (RFC 4648).  
**Parameters:**  
- `input` – the data to encode.  
**Return value:** a Base32‑encoded string with no padding omitted per the TOTP spec.  
**Exceptions:**  
- `ArgumentNullException` if `input` is null.

### `public static byte[] DecodeBase32(string input)`
Decodes a Base32 string back into its original byte array.  
**Parameters:**  
- `input` – the Base32‑encoded string (case‑insensitive, whitespace ignored).  
**Return value:** the decoded byte array.  
**Exceptions:**  
- `ArgumentNullException` if `input` is null.  
- `FormatException` if `input` contains characters outside the Base32 alphabet or has incorrect padding.

## Usage

### Example 1: Enrolling a new MFA device
```csharp
var totpService = new TotpService();

// Generate provisioning data for user "alice@example.com"
var response = await totpService.InitiateSetupAsync(
    userId: "alice@example.com",
    issuer: "MyAuthServer",
    accountName: "alice@example.com");

// Show the QR code URI to the user (e.g., embed in an HTML img tag)
Console.WriteLine("Scan this URI with your authenticator app:");
Console.WriteLine(response.ProvisioningUri);

// After the user scans the QR code and enters a code, confirm the setup
Console.Write("Enter the 6‑digit code from your authenticator app: ");
string userCode = Console.ReadLine();
await totpService.ConfirmSetupAsync(userId: "alice@example.com", code: userCode);
Console.WriteLine("MFA successfully enabled.");
```

### Example 2: Verifying a login attempt
```csharp
var totpService = new TotpService();

string userId = "bob@example.com";
Console.Write("Enter your 6‑digit TOTP code: ");
string code = Console.ReadLine();

bool isValid = await totpService.VerifyAsync(userId: userId, code: code);
if (isValid)
{
    Console.WriteLine("Code accepted. Proceed with login.");
}
else
{
    Console.WriteLine("Invalid code. Access denied.");
}
```

## Notes

- **Clock drift:** TOTP validation typically allows a one‑step tolerance (±30 seconds) to accommodate minor clock differences between server and client. Implementations that rely on `VerifyTotpCode` should apply the same window if stricter security is required.  
- **Secret storage:** The secret key returned by `InitiateSetupAsync` must be persisted securely (e.g., encrypted at rest) and never logged or transmitted in plain text after enrollment.  
- **Thread safety:** All static methods (`BuildProvisioningUri`, `EncodeBase32`, `DecodeBase32`) are pure and thread‑safe. Instance methods do not retain mutable state beyond their dependencies (such as a database context); therefore, concurrent calls on the same `TotpService` instance are safe provided those dependencies are themselves thread‑safe or scoped appropriately.  
- **Error handling:** Methods throw exceptions for invalid arguments or illegal state transitions (e.g., trying to confirm a setup that does not exist). Consumers should catch the specific exception types listed above to provide meaningful feedback to users.  
- **Replay protection:** The service does not retain used codes; reliance on the time‑based nature of TOTP prevents replay within the same time step. For additional protection, consider storing a timestamp of the last successful verification and rejecting codes from earlier steps.
