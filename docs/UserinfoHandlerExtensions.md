# UserinfoHandlerExtensions

The `UserinfoHandlerExtensions` class provides a set of static extension methods designed to simplify the retrieval and validation of user profile information from `IUserinfoHandler` implementations within the `dotnet-auth-server` framework. These methods abstract common operations, such as extracting display names, formatting addresses, and verifying the presence or status of specific identity claims, ensuring consistent, readable, and type-safe access to user identity data across the application.

## API

### GetDisplayName
Returns the display name of the user associated with the handler.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** A `string` containing the user's display name, or a default value if unavailable.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasVerifiedEmail
Determines whether the user has a verified email address.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if the email claim is present and marked as verified; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasVerifiedPhone
Determines whether the user has a verified phone number.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if the phone number claim is present and marked as verified; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### FormatAddress
Retrieves and formats the user's address information into a human-readable string.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** A `string` representing the formatted address, or `null` if address information is missing or incomplete.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasProfileInformation
Checks if basic profile information (e.g., name, preferred username) is available.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if profile information is present; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasEmailInformation
Checks if email-related information is available, regardless of verification status.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if email information is present; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasAddressInformation
Checks if any address-related claims are present for the user.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if address information is present; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

### HasPhoneInformation
Checks if any phone-related claims are present for the user.

*   **Parameters:** `IUserinfoHandler handler`
*   **Returns:** `true` if phone information is present; otherwise, `false`.
*   **Exceptions:** Throws `ArgumentNullException` if `handler` is null.

## Usage

### Example 1: Verifying Profile Completeness
```csharp
public void DisplayUserProfile(IUserinfoHandler userinfo)
{
    if (userinfo.HasProfileInformation())
    {
        Console.WriteLine($"Welcome, {userinfo.GetDisplayName()}");
    }
    else
    {
        Console.WriteLine("Profile information is incomplete.");
    }
}
```

### Example 2: Accessing Secure Contact Details
```csharp
public void UpdateContactDetails(IUserinfoHandler userinfo)
{
    if (userinfo.HasVerifiedEmail())
    {
        // Proceed with secure communication
    }
    
    if (userinfo.HasAddressInformation())
    {
        string? address = userinfo.FormatAddress();
        Console.WriteLine($"Shipping to: {address ?? "Not provided"}");
    }
}
```

## Notes

*   **Thread-Safety:** These extension methods are inherently stateless and operate by querying the provided `IUserinfoHandler` instance. Therefore, they are thread-safe, provided that the underlying `IUserinfoHandler` implementation itself is thread-safe.
*   **Null Handling:** Each method performs a guard check on the `handler` parameter. If the passed `IUserinfoHandler` is `null`, an `ArgumentNullException` is thrown.
*   **Missing Claims:** The boolean check methods (`Has...`) return `false` if the relevant claims are missing from the underlying identity context. The string retrieval methods (`GetDisplayName`, `FormatAddress`) return `null` or empty strings when expected data points are absent, preventing runtime exceptions during data processing. Always validate with `Has...` methods before relying on string content if strict non-null behavior is required.
