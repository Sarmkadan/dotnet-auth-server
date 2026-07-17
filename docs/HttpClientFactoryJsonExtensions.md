# HttpClientFactoryJsonExtensions
The `HttpClientFactoryJsonExtensions` type provides a set of extension methods and properties for working with JSON data in the context of an `HttpClientFactory`. It enables serialization and deserialization of `HttpClientFactory` configurations to and from JSON, allowing for easier storage, transmission, and configuration of HTTP clients.

## API
* `public static string ToJson`: Serializes an `HttpClientFactoryConfig` object to a JSON string. This method takes no parameters other than the implicit `this` reference to an `HttpClientFactoryConfig` instance and returns a JSON string representation of the configuration.
* `public static HttpClientFactoryConfig? FromJson(string json)`: Deserializes a JSON string back into an `HttpClientFactoryConfig` object. It takes a JSON string as a parameter and returns an `HttpClientFactoryConfig` instance if the deserialization is successful, or `null` if the JSON string is invalid or cannot be deserialized.
* `public static bool TryFromJson(string json, out HttpClientFactoryConfig? config)`: Attempts to deserialize a JSON string into an `HttpClientFactoryConfig` object, returning a boolean indicating success and an output parameter containing the deserialized configuration if successful.
* `public TimeSpan DefaultTimeout`: Gets the default timeout for HTTP requests.
* `public TimeSpan WebhookTimeout`: Gets the timeout specifically for webhook requests.
* `public string UserAgent`: Gets the user agent string used in HTTP requests.
* `public TimeSpan ExternalLookupTimeout`: Gets the timeout for external lookup operations.

## Usage
The following examples demonstrate how to use the `HttpClientFactoryJsonExtensions` type to serialize and deserialize `HttpClientFactoryConfig` instances:
```csharp
// Example 1: Serializing an HttpClientFactoryConfig to JSON
var config = new HttpClientFactoryConfig();
// Configure the config as needed
var json = config.ToJson();
Console.WriteLine(json);

// Example 2: Deserializing JSON back to an HttpClientFactoryConfig
var json = "{\"baseUrl\":\"https://example.com\",\"timeout\":\"00:01:00\"}";
if (HttpClientFactoryJsonExtensions.TryFromJson(json, out var config))
{
    Console.WriteLine($"Deserialized config: {config.BaseUrl}, {config.Timeout}");
}
else
{
    Console.WriteLine("Failed to deserialize JSON");
}
```

## Notes
When working with `HttpClientFactoryJsonExtensions`, consider the following:
- The `FromJson` and `TryFromJson` methods may throw exceptions if the provided JSON string is malformed or if the deserialization process fails.
- The `DefaultTimeout`, `WebhookTimeout`, `UserAgent`, and `ExternalLookupTimeout` properties are part of the configuration and should be accessed after an `HttpClientFactoryConfig` instance has been successfully deserialized or created.
- Since these properties and methods are static, they are thread-safe as long as the underlying `HttpClientFactoryConfig` instances are properly synchronized. However, improper synchronization of the config instances themselves could lead to unexpected behavior.
