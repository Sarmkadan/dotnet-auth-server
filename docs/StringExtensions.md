# StringExtensions

Utility extension methods for common string operations in the `dotnet-auth-server` project, covering scope parsing and joining, URI validation and comparison, URL-safety checks, truncation, and masking of sensitive data.

## API

### `ParseScopes`
```csharp
public static IEnumerable<string> ParseScopes(this string? scopes)
```
Parses a space-delimited or comma-delimited scope string into a sequence of individual, non-empty scope tokens.  
**Parameters:**  
`scopes` — the raw scope string; may be `null` or empty.  
**Returns:** an `IEnumerable<string>` of trimmed scope values. If the input is `null` or consists solely of whitespace/delimiters, the enumeration yields no elements.  
**Throws:** never throws.

### `JoinScopes`
```csharp
public static string JoinScopes(this IEnumerable<string> scopes)
```
Joins a sequence of scope tokens into a single space-separated string suitable for use in OAuth 2.0 scope parameters.  
**Parameters:**  
`scopes` — a collection of scope strings.  
**Returns:** a space-delimited string of all non-null, non-empty scopes. Returns `string.Empty` if the collection is empty or contains only null/empty entries.  
**Throws:** `ArgumentNullException` if `scopes` is `null`.

### `IsValidAbsoluteUri`
```csharp
public static bool IsValidAbsoluteUri(this string? uri)
```
Determines whether the input string represents a well-formed absolute URI with an HTTP or HTTPS scheme.  
**Parameters:**  
`uri` — the candidate URI string; may be `null`.  
**Returns:** `true` if the string is non-null, successfully parsed as an absolute `Uri` by `Uri.TryCreate`, and has a scheme of `http` or `https`; otherwise `false`.  
**Throws:** never throws.

### `UriEquals`
```csharp
public static bool UriEquals(this string? left, string? right)
```
Compares two URI strings for equality using ordinal case-insensitive comparison after normalizing trailing slashes. Both strings must be valid absolute URIs; otherwise the comparison falls back to direct ordinal case-insensitive comparison.  
**Parameters:**  
`left` — the first URI string; may be `null`.  
`right` — the second URI string; may be `null`.  
**Returns:** `true` if the normalized forms are equal or both are `null`; `false` otherwise.  
**Throws:** never throws.

### `IsUrlSafe`
```csharp
public static bool IsUrlSafe(this string? value)
```
Checks whether a string consists exclusively of characters that are safe to use in a URL path or query component without percent-encoding.  
**Parameters:**  
`value` — the string to test; may be `null`.  
**Returns:** `true` if the string is non-null, non-empty, and every character falls within the unreserved character set defined by RFC 3986 (letters, digits, hyphen, period, underscore, tilde); otherwise `false`.  
**Throws:** never throws.

### `SafeTruncate`
```csharp
public static string SafeTruncate(this string? value, int maxLength)
```
Truncates a string to a specified maximum length, appending an ellipsis suffix only when truncation actually occurs.  
**Parameters:**  
`value` — the string to truncate; may be `null`.  
`maxLength` — the maximum allowed length of the returned string, including the ellipsis if applied.  
**Returns:** the original string if it is `null` or its length is ≤ `maxLength`; otherwise a string of length `maxLength` ending with `"…"`.  
**Throws:** `ArgumentOutOfRangeException` if `maxLength` is less than the length of the ellipsis suffix (typically 1).

### `MaskSensitive`
```csharp
public static string MaskSensitive(this string? value, int visibleChars = 4)
```
Masks a potentially sensitive string by replacing all but a configurable number of trailing characters with a fixed mask character (`'*'`).  
**Parameters:**  
`value` — the string to mask; may be `null`.  
`visibleChars` — the number of characters to leave unmasked at the end of the string; defaults to 4.  
**Returns:** a masked string if the input is non-null and longer than `visibleChars`; otherwise the original string (or `null`). If `visibleChars` is zero or negative, the entire string is masked.  
**Throws:** never throws.

## Usage

### Example 1: Parsing and Rejoining Scopes
```csharp
string rawScopes = "openid profile, email offline_access";
IEnumerable<string> parsed = rawScopes.ParseScopes();
// parsed yields: ["openid", "profile", "email", "offline_access"]

string normalized = parsed.JoinScopes();
// normalized == "openid profile email offline_access"
```

### Example 2: URI Validation and Safe Truncation for Logging
```csharp
string? redirectUri = "https://example.com/callback";
if (redirectUri.IsValidAbsoluteUri())
{
    string truncated = redirectUri.SafeTruncate(30);
    Console.WriteLine($"Redirect URI: {truncated}");
    // Output: Redirect URI: https://example.com/callb…
}

string? sensitive = "sk_live_abc123xyz";
string masked = sensitive.MaskSensitive(visibleChars: 4);
Console.WriteLine(masked);
// Output: ************xyz
```

## Notes

- **Null handling:** All methods except `JoinScopes` accept `null` inputs gracefully, returning either `false`, an empty enumeration, or the original `null` value. `JoinScopes` throws `ArgumentNullException` when the collection itself is `null`, but tolerates individual null or empty elements within the collection.
- **`UriEquals` normalization:** Trailing slashes are stripped from both URIs before comparison only when both strings are valid absolute URIs. If either is not a valid absolute URI, the method performs a raw ordinal case-insensitive comparison, which may yield unexpected results for near-identical URI strings that differ only by trailing slash.
- **`SafeTruncate` edge case:** When `maxLength` is exactly the length of the ellipsis character, the result is just the ellipsis. When `maxLength` is smaller than the ellipsis length, an `ArgumentOutOfRangeException` is thrown.
- **`MaskSensitive` behavior:** If `visibleChars` is greater than or equal to the string length, the original string is returned unchanged. If `visibleChars` is zero or negative, every character is replaced with `'*'`.
- **Thread safety:** All methods are static and operate on immutable string data without shared mutable state. They are safe to call concurrently from multiple threads.
