# IPolicyEngine

The `IPolicyEngine` interface provides a centralized contract for evaluating authorization policies within the `dotnet-auth-server`. It aggregates user-related identity attributes, resource-specific metadata, and environmental context, enabling the system to make data-driven security decisions based on both user credentials and the specific context of an access request.

## API

The following properties represent the state available for policy evaluation.

| Property | Type | Purpose | Throws |
| :--- | :--- | :--- | :--- |
| `User` | `UserAttributes` | The structured user identity attributes. | N/A |
| `Resource` | `ResourceAttributes` | The structured resource attributes. | N/A |
| `Environment` | `EnvironmentAttributes` | The structured environmental context. | N/A |
| `UserId` | `string` | The unique identifier for the user. | N/A |
| `Roles` | `List<string>` | The collection of roles assigned to the user. | N/A |
| `Department` | `string` | The user's assigned department. | N/A |
| `CostCenter` | `string` | The user's associated cost center. | N/A |
| `TenureMonths` | `int` | The user's tenure duration in months. | N/A |
| `IsManager` | `bool` | Indicates if the user holds a management position. | N/A |
| `Clearances` | `List<string>` | The security clearances assigned to the user. | N/A |
| `ResourceId` | `string` | The unique identifier of the requested resource. | N/A |
| `ResourceType` | `string` | The type classification of the requested resource. | N/A |
| `Classification` | `string` | The security classification of the resource. | N/A |
| `Owner` | `string` | The owner of the resource. | N/A |
| `Tags` | `List<string>` | The metadata tags associated with the resource. | N/A |
| `CreatedAt` | `DateTime` | The creation timestamp of the resource. | N/A |
| `RequestTime` | `DateTime` | The timestamp of the current authorization request. | N/A |
| `SourceIp` | `string` | The IP address originating the request. | N/A |
| `DeviceType` | `string` | The type of device used to initiate the request. | N/A |
| `IsVpnConnected` | `bool` | Indicates if the request originated from a VPN. | N/A |

## Usage

### Example 1: Simple Role-Based Access Control
This example demonstrates verifying if a user has the necessary role and is a manager to perform an action.

```csharp
public bool AuthorizeManagerAction(IPolicyEngine policy)
{
    // Check if the user is a manager and has the 'Admin' role
    return policy.IsManager && policy.Roles.Contains("Admin");
}
```

### Example 2: Attribute-Based Access Control (ABAC)
This example demonstrates a more complex check comparing user attributes and resource metadata.

```csharp
public bool AuthorizeResourceAccess(IPolicyEngine policy)
{
    // Access is granted if the user belongs to the same department as the resource owner,
    // or if the resource classification is 'Public' and the request is secure (VPN).
    bool isSameDepartment = policy.Department == policy.Owner;
    bool isPublicSecure = policy.Classification == "Public" && policy.IsVpnConnected;
    
    return isSameDepartment || isPublicSecure;
}
```

## Notes

*   **Thread Safety**: Implementations of `IPolicyEngine` are expected to be thread-safe for read-only access. Properties should represent a snapshot of the context at the time of evaluation and should not be modified after the engine is initialized for a request.
*   **Null Values**: String properties such as `UserId`, `Department`, and `Classification` may return `null` or `string.Empty` depending on the implementation and context; consumers should perform appropriate null checks or validations.
*   **Data Consistency**: The `RequestTime` property represents the time the policy evaluation was initiated. It should be used for temporal checks rather than relying on system-wide clocks if synchronization across distributed policy engines is required.
