# OpaClient
The `OpaClient` class is designed to interact with an Open Policy Agent (OPA) server, allowing for the evaluation of policies and the retrieval of results. It provides a simple and intuitive interface for integrating policy-based decision making into applications.

## API
* `public OpaClient`: The constructor for the `OpaClient` class, used to create a new instance.
* `public async Task<bool?> EvaluatePolicyAsync`: Evaluates a policy against the provided input and returns the result. The method returns a `bool?` value, indicating whether the policy was evaluated successfully and the result of the evaluation. If an error occurs during evaluation, the method may throw an exception.
* `public OpaInputDocument Input`: Gets or sets the input document used for policy evaluation.
* `public string Subject`: Gets or sets the subject associated with the policy evaluation.
* `public List<string> Roles`: Gets or sets the list of roles associated with the policy evaluation.
* `public List<string> Scopes`: Gets or sets the list of scopes associated with the policy evaluation.
* `public Dictionary<string, string> Claims`: Gets or sets the dictionary of claims associated with the policy evaluation.
* `public bool Result`: Gets the result of the policy evaluation.

## Usage
The following examples demonstrate how to use the `OpaClient` class to evaluate policies:
```csharp
// Example 1: Evaluating a policy with a simple input
var client = new OpaClient();
client.Input = new OpaInputDocument { /* initialize input document */ };
client.Subject = "user1";
var result = await client.EvaluatePolicyAsync();
if (result.HasValue)
{
    Console.WriteLine($"Policy evaluation result: {result}");
}
else
{
    Console.WriteLine("Policy evaluation failed");
}

// Example 2: Evaluating a policy with roles and scopes
var client2 = new OpaClient();
client2.Input = new OpaInputDocument { /* initialize input document */ };
client2.Subject = "user2";
client2.Roles = new List<string> { "admin", "moderator" };
client2.Scopes = new List<string> { "read:articles", "write:articles" };
var result2 = await client2.EvaluatePolicyAsync();
if (result2.HasValue)
{
    Console.WriteLine($"Policy evaluation result: {result2}");
}
else
{
    Console.WriteLine("Policy evaluation failed");
}
```

## Notes
When using the `OpaClient` class, note that the `EvaluatePolicyAsync` method may throw exceptions if errors occur during policy evaluation. Additionally, the `Result` property will only be set after a successful policy evaluation. The `OpaClient` class is designed to be thread-safe, allowing for concurrent policy evaluations. However, the `Input`, `Subject`, `Roles`, `Scopes`, and `Claims` properties should be accessed and modified in a thread-safe manner to avoid inconsistencies.
