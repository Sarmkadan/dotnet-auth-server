# ValidationException

`ValidationException` is a sealed `AuthServerException` used when request input fails validation. It represents the OAuth 2.0 `invalid_request` error with HTTP status code `400`.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `invalid_request` |
| HTTP status code | `400 Bad Request` |
| Default message | `Validation failed` |
| Default error description | The exception message |
| Error URI | `null` |

## Constructors

### General validation failure

```csharp
public ValidationException(
    string message = "Validation failed",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` sets the inherited exception message. It must not be `null` or empty.
- `errorDescription` sets the OAuth 2.0 `error_description`. When it is `null`, the message is used.
- `innerException` preserves the exception that caused the validation failure.

The constructor throws `ArgumentNullException` for a `null` message and `ArgumentException` for an empty message.

### Field-specific validation failure

```csharp
public ValidationException(
    string fieldName,
    string fieldValue,
    string validationRule,
    Exception? innerException = null)
```

This overload builds the message and error description in the following form:

```text
Validation failed for {fieldName}: '{fieldValue}'. {validationRule}
```

Each string argument must not be `null` or empty. A `null` argument causes `ArgumentNullException`; an empty argument causes `ArgumentException`. The values are included in the message but are not automatically added to `Errors`.

## Errors property

```csharp
public Dictionary<string, object> Errors { get; }
```

`Errors` is initialized as an empty, mutable dictionary for every exception instance. Callers can inspect or modify the returned dictionary. The field-specific constructor does not populate it.

## AddError method

```csharp
public void AddError(string fieldName, string errorMessage)
```

Adds a string error message under the specified field name. Both arguments must not be `null` or empty. If the field already exists, its value is replaced.

## Examples

Create a general validation error and add field details:

```csharp
var exception = new ValidationException("The request contains invalid values");
exception.AddError("email", "Email must be a valid address");
exception.AddError("displayName", "Display name is required");

throw exception;
```

Create an exception whose message identifies one failed value and rule:

```csharp
throw new ValidationException(
    fieldName: "age",
    fieldValue: "16",
    validationRule: "Age must be at least 18");
```

The second example produces this exception message:

```text
Validation failed for age: '16'. Age must be at least 18
```

## Response serialization

The inherited `ToErrorResponse()` method serializes the OAuth error code and error description. The `Errors` dictionary is separate from the inherited `Details` dictionary, so entries added with `AddError` are not automatically included by `ToErrorResponse()`.
