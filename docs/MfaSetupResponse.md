# MfaSetupResponse
The `MfaSetupResponse` type in the `dotnet-auth-server` project represents the response to a multi-factor authentication (MFA) setup request. It contains information about the MFA setup process, including the secret key, provisioning URI, backup codes, and the current state of MFA.

## API
The `MfaSetupResponse` type has the following public members:
* `SecretKey`: A string representing the secret key for MFA.
* `ProvisioningUri`: A string representing the provisioning URI for MFA.
* `BackupCodes`: An `IList<string>` containing the backup codes for MFA.
* `Code`: A string representing the MFA code.
* `IsEnabled`: A boolean indicating whether MFA is enabled.
* `EnabledAt`: A `DateTime?` representing the date and time when MFA was enabled, or `null` if MFA is not enabled.
* `LastUsedAt`: A `DateTime?` representing the date and time when MFA was last used, or `null` if MFA has not been used.
* `BackupCodesRemaining`: An integer representing the number of backup codes remaining.

## Usage
Here are two examples of using the `MfaSetupResponse` type:
```csharp
// Example 1: Checking if MFA is enabled
MfaSetupResponse response = GetMfaSetupResponse();
if (response.IsEnabled)
{
    Console.WriteLine("MFA is enabled");
}
else
{
    Console.WriteLine("MFA is not enabled");
}

// Example 2: Using the secret key and provisioning URI to set up MFA
MfaSetupResponse response = GetMfaSetupResponse();
string secretKey = response.SecretKey;
string provisioningUri = response.ProvisioningUri;
// Use the secret key and provisioning URI to set up MFA
```

## Notes
When using the `MfaSetupResponse` type, note that the `EnabledAt` and `LastUsedAt` properties may be `null` if MFA is not enabled or has not been used, respectively. Additionally, the `BackupCodesRemaining` property will decrease each time a backup code is used. The `MfaSetupResponse` type is not thread-safe, so care should be taken when accessing its members from multiple threads. The `BackupCodes` list is a snapshot of the backup codes at the time the `MfaSetupResponse` was created, and may not reflect the current state of the backup codes.
