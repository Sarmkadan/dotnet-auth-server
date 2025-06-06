# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` is an ASP.NET Core middleware component designed to intercept unhandled exceptions and HTTP errors within the request pipeline, converting them into standardized OAuth 2.0-style error responses. This ensures consistent error formatting for API consumers, particularly useful in authentication and authorization flows where structured error responses are required.

## API

### `ErrorHandlingMiddleware`
**Purpose**
The constructor initializes the middleware with the next request delegate in the pipeline. This is called by the ASP.NET Core framework during middleware registration and should not be invoked directly.

**Parameters**
- `next` (`RequestDelegate`): The next middleware in the pipeline.

**Throws**
- `ArgumentNullException`: Thrown if `next` is `null`.

---

### `Task InvokeAsync(HttpContext context)`
**Purpose**
Executes the middleware logic, wrapping the invocation of the next middleware in a try-catch block to handle exceptions and HTTP errors. If an error occurs, it populates the `Error`, `ErrorDescription`, and `ErrorUri` properties and writes a standardized error response to the HTTP context.

**Parameters**
- `context` (`HttpContext`): The current HTTP context.

**Returns**
A `Task` representing the asynchronous operation.

**Throws**
- None. Exceptions are caught and handled internally, converting them into error responses.

---

### `string? Error`
**Purpose**
Gets or sets the error code (e.g., `invalid_request`, `server_error`) as defined in OAuth 2.0/RFC 6749. This property is populated when an exception or HTTP error is caught during middleware execution.

**Remarks**
- Read-only during normal operation; set internally by `InvokeAsync`.
- May be `null` if no error occurred.

---

### `string? ErrorDescription`
**Purpose**
Gets or sets a human-readable description of the error, providing additional context beyond the error code. This is included in the error response.

**Remarks**
- Read-only during normal operation; set internally by `InvokeAsync`.
- May be `null` if no error occurred or if no description is available.

---

### `string? ErrorUri`
**Purpose**
Gets or sets a URI referencing a document describing the error in detail (e.g., a link to documentation). This is optional and may not be populated.

**Remarks**
- Read-only during normal operation; set internally by `InvokeAsync`.
- May be `null` if no error occurred or if no URI is provided.

## Usage

### Example 1: Basic Middleware Registration
