# ConfigurationException

`ConfigurationException` represents an invalid or unusable authorization-server configuration. It is a sealed specialization of `AuthServerException` intended for server-side failures rather than invalid client requests.

## Error response

| Property | Value |
|---|---|
| OAuth 2.0 error code | `server_error` |
| HTTP status code | `500 Internal Server Error` |
| Default message | `Server configuration error` |
| Error URI | `null` |

The inherited `ToErrorResponse()` method produces an OAuth error object. For the general constructor, `error_description` is the explicit `errorDescription` when supplied and otherwise defaults to `message`. For the property-format constructor, it is the generated exception message.

## Constructors

### General configuration error

```csharp
public ConfigurationException(
    string message = "Server configuration error",
    string? errorDescription = null,
    Exception? innerException = null)
```

- `message` supplies the inherited exception message.
- `errorDescription` optionally supplies a distinct OAuth 2.0 `error_description`; when omitted or `null`, it defaults to `message`.
- `innerException` optionally preserves the exception that caused configuration processing to fail.

### Invalid property format

```csharp
public ConfigurationException(
    string propertyName,
    string propertyValue,
    string expectedFormat,
    Exception? innerException = null)
```

This overload creates the following message:

```text
Invalid configuration for {propertyName}: '{propertyValue}'. Expected format: {expectedFormat}
```

- `propertyName` identifies the invalid configuration setting.
- `propertyValue` records the value that failed validation. Avoid including secrets or other sensitive values because the value becomes part of the exception message and error description.
- `expectedFormat` explains the required representation.
- `innerException` optionally preserves the underlying parsing or validation exception.

Neither constructor performs additional validation of its string arguments. Both fix the error code at `server_error`, the HTTP status code at `500`, and the error URI at `null`.

## Examples

Use the general constructor when a required setting is missing or a configuration relationship is invalid:

```csharp
if (string.IsNullOrWhiteSpace(options.Issuer))
    throw new ConfigurationException("Issuer is not configured");
```

Use the property-format constructor when a non-sensitive value has a known required format:

```csharp
if (!Uri.TryCreate(options.Issuer, UriKind.Absolute, out _))
{
    throw new ConfigurationException(
        nameof(options.Issuer),
        options.Issuer,
        "an absolute URI");
}
```

The second example produces an error response equivalent to:

```json
{
  "error": "server_error",
  "error_description": "Invalid configuration for Issuer: 'not-a-uri'. Expected format: an absolute URI"
}
```

## Security considerations

Configuration exceptions may be logged or serialized into an error response. Do not pass secrets, private keys, connection strings, or credentials as `propertyValue`, `message`, or `errorDescription`. Prefer naming the invalid setting and describing its expected format without exposing its contents.
