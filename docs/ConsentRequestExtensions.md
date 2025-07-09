# ConsentRequestExtensions

The `ConsentRequestExtensions` static class provides a set of extension methods designed to simplify the interaction with `ConsentRequest` objects within the authentication server's workflow. These methods encapsulate common validation and data retrieval tasks, ensuring consistent access to request status, authorized scopes, and user or client identifiers throughout the consent processing pipeline.

## API

### IsApproved
Checks whether the specified consent request has been approved.
*   **Returns:** `bool` - `true` if the request is approved; otherwise, `false`.

### IsDenied
Checks whether the specified consent request has been denied.
*   **Returns:** `bool` - `true` if the request is denied; otherwise, `false`.

### GetRequestedScopes
Retrieves the array of scopes associated with the consent request.
*   **Returns:** `string[]` - An array containing the requested scope strings.

### IsPending
Checks whether the consent request is currently in a pending state, awaiting user action.
*   **Returns:** `bool` - `true` if the request is pending; otherwise, `false`.

### GetUserIdOrThrow
Retrieves the user identifier associated with the request.
*   **Returns:** `string` - The identifier of the user who initiated the request.
*   **Throws:** `InvalidOperationException` if the user identifier is missing from the request context.

### GetClientIdOrThrow
Retrieves the client identifier associated with the request.
*   **Returns:** `string` - The identifier of the client application that initiated the request.
*   **Throws:** `InvalidOperationException` if the client identifier is missing from the request context.

## Usage

```csharp
// Example 1: Conditional logic based on consent status
if (consentRequest.IsPending())
{
    // Redirect user to the consent prompt UI
    return View("ConsentPrompt", consentRequest);
}
else if (consentRequest.IsApproved())
{
    // Proceed with authorization code or token issuance
    return await IssueTokensAsync(consentRequest);
}

// Example 2: Extracting identifiers and scopes
try
{
    string userId = consentRequest.GetUserIdOrThrow();
    string clientId = consentRequest.GetClientIdOrThrow();
    string[] requestedScopes = consentRequest.GetRequestedScopes();

    // Perform further validation or logging
    logger.LogInformation("Processing consent for User {UserId} and Client {ClientId}", userId, clientId);
}
catch (InvalidOperationException ex)
{
    // Handle cases where the request is malformed or lacks necessary context
    logger.LogError(ex, "Failed to retrieve required identifiers from consent request.");
    return BadRequest("Invalid consent request context.");
}
```

## Notes

*   **Thread Safety:** The methods within this class are thread-safe, assuming the underlying `ConsentRequest` object is treated as immutable or is properly synchronized externally.
*   **Exception Handling:** Methods suffixed with `OrThrow` are designed to throw an `InvalidOperationException` if the required data points (User ID or Client ID) cannot be resolved. These should be invoked when the request context is expected to be complete.
*   **State Exclusivity:** The status-checking methods (`IsApproved`, `IsDenied`, `IsPending`) are intended to reflect mutually exclusive states within the request lifecycle. Ensure the underlying request state is updated consistently before performing these checks.
