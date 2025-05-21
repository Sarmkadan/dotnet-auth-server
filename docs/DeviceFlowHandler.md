# DeviceFlowHandler

The `DeviceFlowHandler` manages the OAuth 2.0 Device Authorization Grant flow, enabling user authorization on devices with limited input capabilities. It handles device code generation, user verification, and token exchange, coordinating between the device, user browser, and authorization server.

## API

### `DeviceFlowHandler`
Initializes a new instance of the device flow handler with default or provided configuration.

### `DeviceInitiation InitiateFlow()`
Initiates a new device authorization flow.
- **Returns**: A `DeviceFlowInitiation` object containing `DeviceCode`, `UserCode`, `VerificationUri`, `ExpiresIn`, and `Interval` required for user authorization.
- **Throws**: `InvalidOperationException` if a flow is already active.

### `bool ApproveDeviceFlow(string userId, string? scope = null)`
Approves the pending device flow for the specified user and optional scope.
- **Parameters**:
  - `userId`: The identifier of the user approving the flow.
  - `scope`: Optional space-separated list of requested scopes.
- **Returns**: `true` if approval succeeds; `false` if no flow is pending or user mismatch.
- **Throws**: `ArgumentNullException` if `userId` is null.

### `bool DenyDeviceFlow()`
Denies the pending device flow, terminating the authorization attempt.
- **Returns**: `true` if denial succeeds; `false` if no flow is pending.
- **Throws**: None.

### `DeviceFlowPollResult PollDeviceFlow()`
Polls the current state of the device flow.
- **Returns**: A `DeviceFlowPollResult` indicating whether the flow is `Pending`, `Approved`, `Denied`, or `Expired`, along with optional error details.
- **Throws**: `InvalidOperationException` if no flow has been initiated.

### `void CompleteDeviceFlow()`
Finalizes the device flow by clearing all associated state.
- **Throws**: `InvalidOperationException` if no flow is active.

### `string DeviceCode`
Gets the unique device verification code used during polling.

### `string UserCode`
Gets the user-friendly code displayed on the device for verification.

### `string VerificationUri`
Gets the URI where the user must visit to approve or deny the flow.

### `int ExpiresIn`
Gets the lifetime in seconds of the device code before expiration.

### `int Interval`
Gets the minimum polling interval in seconds for the device.

### `DeviceFlowStatus Status`
Gets the current status of the device flow (`Pending`, `Approved`, `Denied`, or `Expired`).

### `string? UserId`
Gets the identifier of the user who approved the flow, or `null` if not approved.

### `string? Scope`
Gets the approved scope string, or `null` if not approved or denied.

### `string? Error`
Gets the error message if the flow failed, or `null` otherwise.

### `string ClientId`
Gets the client identifier associated with the flow.

## Usage

### Initiating and Approving a Flow
