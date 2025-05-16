# ValidationExceptionExtensions

The `ValidationExceptionExtensions` static class provides a suite of extension methods for managing `ValidationException` instances, enabling seamless manipulation of error collections, contextual error reporting, and efficient error aggregation. These utilities are designed to streamline validation workflows by providing a fluent interface for appending, merging, and querying validation errors in a type-safe manner.

## API

### AddErrors
Appends a collection of error messages to the existing `ValidationException`.

*   **Parameters:**
    *   `exception` (this): The base `ValidationException`.
    *   `errors`: An `IEnumerable<string>` containing the error messages to add.
*   **Returns:** A `ValidationException` instance updated with the new errors.
*   **Throws:** `ArgumentNullException` if `exception` or `errors` is null.

### AddErrorWithContext
Adds an error message associated with a specific property name and additional contextual information.

*   **Parameters:**
    *   `exception` (this): The base `ValidationException`.
    *   `propertyName`: The name of the property or field the error applies to.
    *   `errorMessage`: The error message description.
    *   `context`: An object containing additional context for the error.
*   **Returns:** A `ValidationException` instance updated with the contextual error.
*   **Throws:** `ArgumentNullException` if `exception`, `propertyName`, or `errorMessage` is null.

### MergeErrors
Combines all errors from another `ValidationException` instance into the current one.

*   **Parameters:**
    *   `exception` (this): The base `ValidationException`.
    *   `other`: The `ValidationException` to merge into the base.
*   **Returns:** A `ValidationException` instance containing the aggregated errors from both sources.
*   **Throws:** `ArgumentNullException` if `exception` or `other` is null.

### HasError
Determines if the `ValidationException` contains any errors associated with a specific property name.

*   **Parameters:**
    *   `exception` (this): The base `ValidationException`.
    *   `propertyName`: The name of the property or field to check for errors.
*   **Returns:** `true` if errors exist for the specified property; otherwise, `false`.
*   **Throws:** `ArgumentNullException` if `exception` or `propertyName` is null.

## Usage

```csharp
// Example 1: Adding errors and checking for a specific property error
try {
    var ex = new ValidationException("Initial error");
    ex.AddErrors(new[] { "Password too short", "Invalid email format" });
    ex.AddErrorWithContext("Username", "Username is required", new { AttemptedValue = "" });

    if (ex.HasError("Username")) {
        // Handle username validation failure
    }
} catch (ValidationException ex) {
    // Log or return validation errors
}
```

```csharp
// Example 2: Merging validation errors from multiple sources
var validationResult1 = ValidateUser(user);
var validationResult2 = ValidatePermissions(user);

if (validationResult1.HasError("General") || validationResult2.HasError("General")) {
    var combinedException = validationResult1.MergeErrors(validationResult2);
    throw combinedException;
}
```

## Notes

*   **Immutability:** Depending on the underlying `ValidationException` implementation, these methods may return a new `ValidationException` instance to maintain immutability or modify the existing instance in place; ensure your usage patterns align with the expected behavior of the `ValidationException` type in your specific project context.
*   **Thread Safety:** The `ValidationExceptionExtensions` methods themselves are stateless and thread-safe. However, thread safety of the resulting `ValidationException` instance depends entirely on the implementation of the `ValidationException` class itself. If `ValidationException` is not thread-safe, avoid sharing instances across threads without appropriate synchronization.
