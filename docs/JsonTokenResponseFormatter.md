# JsonTokenResponseFormatter

The `JsonTokenResponseFormatter` class provides essential utilities for the serialization and deserialization of OAuth 2.0 token responses in the `dotnet-auth-server` project. It facilitates the conversion between structured `TokenResponse` objects and JSON-formatted strings, ensuring consistent handling of access tokens, refresh tokens, and associated metadata according to industry standard specifications.

## API

### Static Members

- `public static string FormatTokenResponse(TokenResponse response)`
  Converts a `TokenResponse` instance into a corresponding JSON-formatted string.

- `public static TokenResponse? ParseTokenResponse(string json)`
  Parses a JSON-formatted string into a `TokenResponse` object. Returns `null` if the input string is invalid or cannot be parsed.

### Instance Properties

- `public string? AccessToken`
  Gets or sets the access token string issued by the authorization server.

- `public string? TokenType`
  Gets or sets the type of the access token, such as "Bearer".

- `public int? ExpiresIn`
  Gets or sets the lifetime in seconds of the access token.

- `public string? RefreshToken`
  Gets or sets the refresh token string, if issued.

- `public string? Scope`
  Gets or sets the scope of the access token.

## Usage

### Example: Serializing a Token Response
```csharp
var tokenResponse = new TokenResponse {
    AccessToken = "eyJhbGciOiJIUzI1Ni...",
    TokenType = "Bearer",
    ExpiresIn = 3600
};

string json = JsonTokenResponseFormatter.FormatTokenResponse(tokenResponse);
Console.WriteLine(json);
```

### Example: Deserializing a JSON Response
```csharp
string jsonResponse = "{\"access_token\":\"eyJhbGciOiJIUzI1Ni...\", \"token_type\":\"Bearer\", \"expires_in\":3600}";

TokenResponse? response = JsonTokenResponseFormatter.ParseTokenResponse(jsonResponse);

if (response != null) {
    Console.WriteLine($"Token: {response.AccessToken}");
}
```

## Notes

- **Error Handling**: The `ParseTokenResponse` method expects valid JSON formatted as a standard OAuth 2.0 token response. If the input is null, empty, or contains malformed JSON, the method may return `null` or throw an exception depending on the underlying JSON library implementation.
- **Thread Safety**: The static methods `FormatTokenResponse` and `ParseTokenResponse` are designed to be stateless, rendering them thread-safe for concurrent operations, provided that the `TokenResponse` object passed to `FormatTokenResponse` is not modified by another thread during serialization.
