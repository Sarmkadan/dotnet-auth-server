# AuthServerException

Exception type thrown by the dotnet-auth-server to signal authentication or authorization failures. Encapsulates OAuth 2.0 / OpenID Connect error details and allows attaching additional diagnostic information.

## API

### `public string ErrorCode`
Gets the OAuth 2.0 / OpenID Connect error code identifying the failure. Must be a non-empty string per RFC 6749 and RFC 6750. Corresponds to the `error` field in error responses.

### `public int StatusCode`
Gets the HTTP status code to return to the client. Must be a valid 4xx or 5xx status code. Common values include 400 (Bad Request), 401 (Unauthorized), and 403 (Forbidden).

### `public string? ErrorDescription`
Gets an optional human-readable description of the error. Corresponds to the `error_description` field in error responses. May be null if no description is provided.

### `public string? ErrorUri`
Gets an optional URI identifying a human-readable web page with additional information about the error. Corresponds to the `error_uri` field in error responses. May be null if no URI is provided.

### `public Dictionary<string, object> Details`
Gets a dictionary of additional key/value pairs to include in the error response. Values may be strings, numbers, booleans, or nested dictionaries. Never null; initialized to an empty dictionary if no details are provided.

### `public AuthServerException(string errorCode, int statusCode, string? errorDescription = null, string? errorUri = null, Dictionary<string, object>? details = null)`
Constructs a new exception with the specified error code and status code. Optional parameters allow including a description, a URI, and additional details. Throws `ArgumentNullException` if `errorCode` is null. Throws `ArgumentException` if `errorCode` is empty or if `statusCode` is outside the range 400–599.

### `public Dictionary<string, object> ToErrorResponse()`
Converts the exception into a dictionary suitable for serialization as an OAuth 2.0 / OpenID Connect error response. Returns a dictionary containing the `error`, `error_description`, and `error_uri` fields if present, followed by all entries from `Details`. The returned dictionary is a new instance and is never null.

## Usage

```csharp
// Example 1: Basic error with description
try
{
    // Some operation that may fail
}
catch (AuthServerException ex) when (ex.StatusCode == 400)
{
    var response = ex.ToErrorResponse();
    // response = { { "error", "invalid_request" }, { "error_description", "Missing required parameter: client_id" } }
}

// Example 2: Error with details and custom status code
var details = new Dictionary<string, object>
{
    { "trace_id", Guid.NewGuid().ToString() },
    { "retry_after", 30 }
};
var ex = new AuthServerException(
    "rate_limit_exceeded",
    429,
    "Too many requests",
    "https://docs.example.com/errors/rate-limit",
    details);
var response = ex.ToErrorResponse();
// response = {
//   { "error", "rate_limit_exceeded" },
//   { "error_description", "Too many requests" },
//   { "error_uri", "https://docs.example.com/errors/rate-limit" },
//   { "trace_id", "..." },
//   { "retry_after", 30 }
// }
```

## Notes

`ErrorCode` must be a non-empty string; passing an empty string throws `ArgumentException`. `StatusCode` must be between 400 and 599 inclusive; values outside this range throw `ArgumentException`. `Details` is never null; if `null` is passed to the constructor, an empty dictionary is used. `ToErrorResponse` allocates a new dictionary and is safe to call from any thread; the returned dictionary is mutable and may be modified without affecting the exception instance.
