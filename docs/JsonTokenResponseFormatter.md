# JsonTokenResponseFormatter

Static helper that converts a `TokenResponse` into an OAuth 2.0 token response JSON document (RFC 6749 §5.1) and parses such a document back into a `TokenResponse`. The wire format uses snake_case member names and omits null fields. The class is `public sealed` in the `DotnetAuthServer.Formatters` namespace and has no instance state. Its only public instance member is the `ToString` override.

The formatter works on a private DTO (`TokenResponseDto`) with `[JsonPropertyName]` attributes, not on `TokenResponse` directly. Only five fields are part of the JSON contract: `access_token`, `token_type`, `expires_in`, `refresh_token`, and `scope`. `TokenResponse.IdToken` and `TokenResponse.CustomProperties` are never written, and they are dropped on parse. See `docs/TokenResponse.md` for the full model.

## Behavior notes

- **Serialization uses a fixed field set and order.** Output always contains `expires_in`, because `TokenResponse.ExpiresIn` is an `int`. `access_token`, `token_type`, `refresh_token`, and `scope` are omitted when their value is `null`. Keys are written in the order `access_token`, `token_type`, `expires_in`, `refresh_token`, `scope`. Output is compact (no indentation).
- **Serialization does not fail on null fields.** A `TokenResponse` whose `AccessToken` is `null` (the property is declared non-nullable but may be left `null` at runtime) serializes without an `access_token` key.
- **Parsing never throws.** `ParseTokenResponse` catches every exception and returns `null`. That covers malformed JSON, a non-object root such as `[]` or `"x"`, wrong member types (for example `expires_in` as a string or a fractional number), numeric overflow, and a `null` argument.
- **Parsing a literal `null` returns `null`.** If the JSON text deserializes to `null`, the method returns `null` without creating a `TokenResponse`.
- **Missing members get defaults on parse.** An absent `access_token` becomes `string.Empty`, so a successful parse can still yield an empty token. Callers must check it. An absent `token_type` becomes `"Bearer"`. An absent `expires_in` becomes `0`. `refresh_token` and `scope` stay `null` when absent.
- **Unknown members are ignored.** Members such as `id_token` or any custom extension are skipped silently. Member names are matched case-sensitively against the snake_case names, as the options do not enable case-insensitive matching.
- **Escaping follows `System.Text.Json` defaults.** Non-ASCII characters and HTML-sensitive characters (`<`, `>`, `&`, `'`, `+`) are written as `\uXXXX` escapes. JWT access tokens contain only base64url characters and `.`, so they are not affected.
- **Thread safety.** The shared `JsonSerializerOptions` instance is private and read-only after first use. Both methods keep no mutable state, so they are safe to call concurrently. The `TokenResponse` passed to `FormatTokenResponse` must not be mutated by another thread while it is being serialized.

## API

### `public static string? FormatTokenResponse(TokenResponse? response)`
Serializes `response` to a compact JSON token response. Returns `null` if `response` is `null`. Otherwise it copies `AccessToken`, `TokenType`, `ExpiresIn`, `RefreshToken`, and `Scope` into the private DTO and serializes that DTO. It does not throw for a `TokenResponse` with null members.

Example output for `AccessToken = "abc"`, `TokenType = "Bearer"`, `ExpiresIn = 3600`:

```json
{"access_token":"abc","token_type":"Bearer","expires_in":3600}
```

### `public static TokenResponse? ParseTokenResponse(string json)`
Deserializes `json` into the private DTO and maps it to a new `TokenResponse`:

| JSON member | `TokenResponse` property | When absent |
|---|---|---|
| `access_token` | `AccessToken` | `string.Empty` |
| `token_type` | `TokenType` | `"Bearer"` |
| `expires_in` | `ExpiresIn` | `0` |
| `refresh_token` | `RefreshToken` | `null` |
| `scope` | `Scope` | `null` |

Returns `null` if `json` is malformed, has the wrong member types, is a non-object JSON value, or is the literal `null`. Never throws. `IdToken` and `CustomProperties` are never set by this method.

### `public override string ToString()`
Returns the constant string `JsonTokenResponseFormatter { AccessToken = null, TokenType = null, ExpiresIn = null, RefreshToken = null, Scope = null }`. The text does not reflect any state, because the formatter has none. Use it only as a type label in logs. It is not a serialized form of any token response.

## Usage

### Serializing a token response

```csharp
var tokenResponse = new TokenResponse
{
    AccessToken = "eyJhbGciOiJIUzI1NiJ9.e30.signature",
    TokenType = "Bearer",
    ExpiresIn = 3600,
    Scope = "openid profile"
};

string? json = JsonTokenResponseFormatter.FormatTokenResponse(tokenResponse);
// {"access_token":"eyJhbGciOiJIUzI1NiJ9.e30.signature","token_type":"Bearer","expires_in":3600,"scope":"openid profile"}
```

### Parsing a token response

```csharp
const string body = """{"access_token":"abc","expires_in":3600}""";

TokenResponse? response = JsonTokenResponseFormatter.ParseTokenResponse(body);
// response.TokenType == "Bearer" (default applied), response.RefreshToken == null
if (response is { AccessToken.Length: > 0 })
{
    Console.WriteLine($"Expires in {response.ExpiresIn}s");
}
```
