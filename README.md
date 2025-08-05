## DeviceFlowHandler

The `DeviceFlowHandler` class is responsible for managing device flow authorizations, enabling devices with limited input capabilities to obtain access tokens. It provides methods for initiating, approving, denying, and polling device flow sessions.

### Usage Example

```csharp
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

// Setup dependencies
var logger = new Logger<DeviceFlowHandler>();
var deviceFlowHandler = new DeviceFlowHandler(logger);

// Initiate a device flow
var initiation = deviceFlowHandler.InitiateFlow(
    "client123",
    "openid profile");

// Display the device code and user code to the user
Console.WriteLine($"Device code: {initiation.DeviceCode}");
Console.WriteLine($"User code: {initiation.UserCode}");
Console.WriteLine($"Verification URI: {initiation.VerificationUri}");

// Approve the device flow after user verification
deviceFlowHandler.ApproveDeviceFlow(initiation.UserCode, "user123");

// Poll the status of the device flow
var pollResult = deviceFlowHandler.PollDeviceFlow(initiation.DeviceCode);
if (pollResult.Status == DeviceFlowStatus.Approved)
{
    Console.WriteLine("Device flow approved.");
}
else
{
    Console.WriteLine("Device flow denied or expired.");
}
```

This example demonstrates how to use the `DeviceFlowHandler` to initiate, approve, and poll a device flow authorization.

