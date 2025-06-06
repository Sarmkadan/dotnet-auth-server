# ApiResponse

A generic container for API responses that standardizes success and error payloads across the `dotnet-auth-server` project. It includes typed data payloads for successful responses and structured error details for failures, along with metadata such as timestamps, trace identifiers, and status codes. The type provides static factory methods for common response patterns and supports pagination through a non-generic variant.

## API

### Generic Type `ApiResponse<T>`

#### `public bool Success`
Indicates whether the operation completed successfully. `true` for successful responses; `false` for errors.

#### `public T? Data`
The typed payload returned on success. May be `null` when `Success` is `false`.

#### `public string? Error`
A machine-readable error code or identifier. Typically non-null only when `Success` is `false`.

#### `public string? Message`
A human-readable status or error message. Present in both success and error responses.

#### `public int? Code`
An HTTP-like status code. Typically `200` for success and a 4xx/5xx code for errors.

#### `public string? TraceId`
A unique identifier for tracing the request across services. Useful for correlating logs.

#### `public DateTime Timestamp`
The UTC time when the response was generated.

#### `public static ApiResponse<T> SuccessResponse(T? data, string? message = null, int? code = 200)`
Constructs a successful response with typed data.

- **Parameters**
  - `data`: The payload to include in the response.
  - `message`: Optional human-readable message.
  - `code`: Optional status code (defaults to `200`).
- **Return Value**
  An `ApiResponse<T>` with `Success` set to `true`, populated `Data`, and provided metadata.

#### `public static ApiResponse<T> ErrorResponse(string? error, string? message = null, int? code = null)`
Constructs an error response without typed data.

- **Parameters**
  - `error`: Machine-readable error code or identifier.
  - `message`: Optional human-readable message.
  - `code`: Optional status code (defaults to `null`, implying a server error).
- **Return Value**
  An `ApiResponse<T>` with `Success` set to `false`, `Data` set to `null`, and populated error metadata.
- **Throws**
  `ArgumentNullException` if `error` is `null` or whitespace.

### Non-Generic Type `ApiResponse`

#### `public bool Success`
Indicates whether the operation completed successfully.

#### `public string? Message`
A human-readable status or error message.

#### `public string? Error`
A machine-readable error code or identifier.

#### `public int? Code`
An HTTP-like status code.

#### `public string? TraceId`
A unique identifier for tracing the request across services.

#### `public DateTime Timestamp`
The UTC time when the response was generated.

#### `public List<T> Items`
A paginated list of typed items. Only present in paginated responses.

#### `public int PageNumber`
The current page number in a paginated response. Starts at `1`.

#### `public static ApiResponse SuccessResponse(string? message = null, int? code = 200)`
Constructs a successful non-generic response.

- **Parameters**
  - `message`: Optional human-readable message.
  - `code`: Optional status code (defaults to `200`).
- **Return Value**
  An `ApiResponse` with `Success` set to `true` and provided metadata.

#### `public static ApiResponse ErrorResponse(string? error, string? message = null, int? code = null)`
Constructs an error non-generic response.

- **Parameters**
  - `error`: Machine-readable error code or identifier.
  - `message`: Optional human-readable message.
  - `code`: Optional status code (defaults to `null`, implying a server error).
- **Return Value**
  An `ApiResponse` with `Success` set to `false` and populated error metadata.
- **Throws**
  `ArgumentNullException` if `error` is `null` or whitespace.

## Usage
