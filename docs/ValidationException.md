# ValidationException

Represents an exception that occurs when request validation fails, carrying a dictionary of field-level or global error details. It is designed to be thrown by validators and caught by middleware or controllers to produce structured error responses.

## API

### Constructors

```csharp
public ValidationException()
```
Initializes a new instance with an empty `Errors` dictionary. Use this when no specific field errors are available at construction time, or when errors will be added afterward via `AddError`.

```csharp
public ValidationException(Dictionary<string, object> errors)
```
Initializes a new instance with a pre-populated dictionary of errors.

| Parameter | Type                        | Description                                      |
|-----------|-----------------------------|--------------------------------------------------|
| `errors`  | `Dictionary<string, object>`| The error details keyed by field name or a global key. |

**Remarks:** The provided dictionary is stored directly; no defensive copy is made. Passing `null` will result in a `NullReferenceException` when `Errors` is accessed later.

### Properties

```csharp
public Dictionary<string, object> Errors { get; }
```
Gets the dictionary of validation errors. Keys are typically field names (e.g., `"email"`, `"password"`) or a global identifier (e.g., `"_"` or `"summary"`). Values are objects that describe the error; common types include `string` messages, `List<string>` for multiple messages per field, or complex error detail objects.

**Return value:** The dictionary instance held by this exception. Modifications to the returned dictionary affect the exception’s state.

### Methods

```csharp
public void AddError(string key, object value)
```
Adds or updates an error entry in the `Errors` dictionary.

| Parameter | Type   | Description                                           |
|-----------|--------|-------------------------------------------------------|
| `key`     | `string`| The error key, typically a field name or global identifier. |
| `value`   | `object`| The error detail to associate with the key.           |

**Remarks:** If `key` already exists, its value is overwritten. Passing a `null` key throws `ArgumentNullException`. Passing a `null` value is allowed and stores `null` as the error detail.

## Usage

### Example 1: Throwing with pre-built errors

```csharp
public void ValidateUserRegistration(RegisterRequest request)
{
    var errors = new Dictionary<string, object>();

    if (string.IsNullOrWhiteSpace(request.Email))
        errors["email"] = "Email is required.";
    else if (!IsValidEmail(request.Email))
        errors["email"] = "Email format is invalid.";

    if (string.IsNullOrWhiteSpace(request.Password))
        errors["password"] = "Password is required.";
    else if (request.Password.Length < 8)
        errors["password"] = new List<string> { "Password must be at least 8 characters.", "Password must include a digit." };

    if (errors.Count > 0)
        throw new ValidationException(errors);
}
```

### Example 2: Incremental construction with AddError

```csharp
public void ValidateOrderSubmission(OrderRequest request)
{
    var exception = new ValidationException();

    if (request.Quantity <= 0)
        exception.AddError("quantity", "Quantity must be greater than zero.");

    if (request.Quantity > 100)
        exception.AddError("quantity", "Quantity exceeds maximum allowed.");

    if (request.ShippingAddress == null)
        exception.AddError("shippingAddress", "Shipping address is required.");

    if (!IsValidPostalCode(request.ShippingAddress?.PostalCode))
        exception.AddError("shippingAddress.postalCode", "Invalid postal code.");

    if (exception.Errors.Count > 0)
        throw exception;
}
```

## Notes

- **Dictionary ownership:** The `Errors` dictionary is a direct reference to the instance passed to the constructor or created internally. Callers who pass a dictionary can still mutate it externally after the exception is constructed, which may lead to unexpected behavior. Consider passing a copy if external mutation is a concern.
- **Key overwrites:** `AddError` overwrites existing keys without warning. If multiple errors per field are needed, store a `List<object>` or `List<string>` as the value and append to it manually before calling `AddError`.
- **Thread safety:** This type is not thread-safe. Concurrent calls to `AddError` or concurrent reads of `Errors` while another thread is modifying the dictionary will result in undefined behavior. Instances are typically constructed and thrown on a single thread within a request scope, so this is rarely a practical concern.
- **Serialization:** When this exception is caught and its `Errors` property is serialized to JSON (e.g., in an exception-handling middleware), ensure that the values stored in the dictionary are serializable by the chosen serializer. Complex objects or circular references may cause serialization failures.
- **Inheritance:** This type derives from `Exception`. The standard exception properties (`Message`, `StackTrace`, etc.) are inherited but not populated with validation-specific information by default. Set `Message` explicitly via the base constructor if a summary message is desired.
