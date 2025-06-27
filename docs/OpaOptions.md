# OpaOptions
The `OpaOptions` type in the `dotnet-auth-server` project is used to configure the Open Policy Agent (OPA) integration. It provides a set of options that control how the OPA is enabled, its base URL, policy path, timeout, and error handling behavior. These options are crucial for customizing the OPA's decision-making process and ensuring seamless interaction with the auth server.

## API
The `OpaOptions` type exposes the following public members:
* `Enabled`: A boolean indicating whether the OPA integration is enabled.
* `BaseUrl`: A string representing the base URL of the OPA server.
* `PolicyPath`: A string specifying the path to the policy file.
* `TimeoutSeconds`: An integer representing the timeout in seconds for OPA requests.
* `FailClosedOnError`: A boolean indicating whether the auth server should fail closed (i.e., deny access) when an error occurs during OPA evaluation.

## Usage
Here are two examples of using the `OpaOptions` type in C#:
```csharp
// Example 1: Enabling OPA with default settings
var opaOptions = new OpaOptions
{
    Enabled = true,
    BaseUrl = "https://opa.example.com",
    PolicyPath = "/v1/data/example/policy",
    TimeoutSeconds = 10,
    FailClosedOnError = true
};

// Example 2: Customizing OPA settings
var customOpaOptions = new OpaOptions
{
    Enabled = true,
    BaseUrl = "https://custom-opa.example.com",
    PolicyPath = "/v1/data/custom/policy",
    TimeoutSeconds = 5,
    FailClosedOnError = false
};
```

## Notes
When using the `OpaOptions` type, consider the following edge cases and thread-safety remarks:
* The `Enabled` property should be set to `true` to enable OPA integration. If set to `false`, the OPA will not be consulted during the decision-making process.
* The `BaseUrl` and `PolicyPath` properties should be set to valid URLs and paths, respectively, to ensure proper OPA configuration.
* The `TimeoutSeconds` property should be set to a reasonable value to avoid timeouts during OPA requests.
* The `FailClosedOnError` property should be set to `true` to ensure that the auth server fails closed in case of an error during OPA evaluation.
* The `OpaOptions` type is not thread-safe by default. If multiple threads need to access and modify the `OpaOptions` instance, proper synchronization mechanisms should be implemented to avoid concurrency issues.
