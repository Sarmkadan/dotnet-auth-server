# ApiResponseExtensions

The `ApiResponseExtensions` class provides a set of static extension methods designed to streamline the construction, modification, and validation of `ApiResponse` and `ApiResponse<T>` objects within the `dotnet-auth-server` project. These methods offer a fluent interface for setting response properties such as data payloads, status codes, error details, and tracing identifiers, while providing utility methods for inspecting the state of response instances.

## API

### WithData<T>
Attaches a data payload to an `ApiResponse` object, returning a new `ApiResponse<T>` instance containing the specified data.
- **Parameters:** `ApiResponse response`, `T data`
- **Returns:** `ApiResponse<T>`
- **Throws:** ArgumentNullException if the provided `response` is null.

### WithError (Overload 1)
Sets error information on an `ApiResponse` object using an error message.
- **Parameters:** `ApiResponse response`, `string message`
- **Returns:** `ApiResponse`

### WithError (Overload 2)
Sets error information on an `ApiResponse` object using an error code and a descriptive message.
- **Parameters:** `ApiResponse response`, `string errorCode`, `string message`
- **Returns:** `ApiResponse`

### WithMessage
Appends a status or informational message to an `ApiResponse` object.
- **Parameters:** `ApiResponse response`, `string message`
- **Returns:** `ApiResponse`

### HasData<T>
Checks if an `ApiResponse<T>` instance contains a non-null data payload.
- **Parameters:** `ApiResponse<T> response`
- **Returns:** `bool` (true if data is present, otherwise false)

### IsSuccess
Determines if an `ApiResponse` signifies a successful operation, typically based on the HTTP status code or an internal success flag.
- **Parameters:** `ApiResponse response`
- **Returns:** `bool`

### UpdateData<T>
Applies a transformation function to the existing data payload of an `ApiResponse<T>` instance and returns the updated `ApiResponse<T>`.
- **Parameters:** `ApiResponse<T> response`, `Func<T, T> updateAction`
- **Returns:** `ApiResponse<T>`

### WithStatusCode
Assigns an HTTP status code to an `ApiResponse` object.
- **Parameters:** `ApiResponse response`, `int statusCode`
- **Returns:** `ApiResponse`

### WithTraceId
Attaches a unique trace identifier to an `ApiResponse` object for logging and diagnostic purposes.
- **Parameters:** `ApiResponse response`, `string traceId`
- **Returns:** `ApiResponse`

## Usage

### Building a Response
```csharp
var response = new ApiResponse()
    .WithStatusCode(200)
    .WithTraceId("trace-123")
    .WithData(new UserProfile { Id = "user-1", Name = "John Doe" });
```

### Validating and Updating
```csharp
public ApiResponse<UserProfile> ProcessUser(ApiResponse<UserProfile> response)
{
    if (ApiResponseExtensions.HasData(response))
    {
        return response.UpdateData(user => {
            user.LastAccessed = DateTime.UtcNow;
            return user;
        });
    }
    
    return response.WithError("ERR001", "User data not found.");
}
```

## Notes

- **Immutability:** The extension methods generally return a modified instance of the response. Depending on the underlying `ApiResponse` implementation, this may involve returning a new object or modifying the existing instance. Users should rely on the returned value.
- **Thread Safety:** These extension methods operate on the provided `ApiResponse` instance. If the underlying `ApiResponse` object is mutable and shared across threads, standard concurrency precautions are required. 
- **Null Handling:** While the methods are designed to be fluent, passing `null` as the initial `ApiResponse` target will likely result in an `ArgumentNullException`. Ensure that the initial response object is properly instantiated.
