# StringExtensions

A static class of extension methods on `System.String` (and `IEnumerable<string>` for `JoinScopes`) used in the dotnet‑auth‑server project for OAuth 2.0 / OIDC scope handling, URI validation and comparison, identifier checks, truncation, and log-safe masking. Namespace: `DotnetAuthServer.Extensions`.

## API

### `public static IEnumerable<string> ParseScopes(this string? scopes)`
**Purpose**  
Splits a space-delimited scope string into distinct, non-empty scope names.

**Parameters**  
- `scopes`: The raw scope string. May be `null`.

**Return Value**  
A lazily evaluated sequence of distinct scope names, in order of first appearance. Tokens are split on the space character only (tabs and commas are not delimiters; a comma stays inside its token). Returns an empty sequence when `scopes` is `null`, empty, or whitespace-only.

**Exceptions**  
None.

---

### `public static string JoinScopes(this IEnumerable<string> scopes)`
**Purpose**  
Joins scope names into a single space-delimited string, the inverse of `ParseScopes`.

**Parameters**  
- `scopes`: The scope names to join.

**Return Value**  
The elements joined with a single space. Elements that are `null`, empty, or whitespace-only are skipped. Elements are not trimmed or de-duplicated. Returns `""` when no elements remain.

**Exceptions**  
- `ArgumentNullException` if `scopes` itself is `null`.

---

### `public static bool IsValidAbsoluteUri(this string? uri)`
**Purpose**  
Checks that a string is a well-formed absolute URI whose scheme is `http` or `https`.

**Parameters**  
- `uri`: The candidate URI string.

**Return Value**  
`true` if `uri` is non-blank, parses with `Uri.TryCreate(uri, UriKind.Absolute, …)`, and has the scheme `http` or `https`; otherwise `false`.

**Exceptions**  
- `ArgumentNullException` if `uri` is `null`. Blank strings return `false` instead.

---

### `public static bool UsesHttps(this string? redirectUri)`
**Purpose**  
Determines whether a redirect URI is absolute and uses the HTTPS scheme. Intended for redirect URI policy checks.

**Parameters**  
- `redirectUri`: The redirect URI to inspect. May be `null`.

**Return Value**  
`true` if the string parses as an absolute URI with scheme `https` (case-insensitive); otherwise `false`. Note that `http` URIs return `false`; this method does not check for other schemes beyond `https`.

**Exceptions**  
None.

---

### `public static bool UriEquals(this string? uri1, string? uri2)`
**Purpose**  
Compares two URI strings by their parsed `Uri` value rather than by raw text.

**Parameters**  
- `uri1`: The first URI string.
- `uri2`: The second URI string.

**Return Value**  
- If both strings are blank, they are compared with ordinal, case-insensitive equality (so two empty strings are equal).
- Otherwise, if both parse as absolute URIs (any scheme), the result is `Uri.Equals` on the two parsed values. This is case-insensitive for the scheme and host, treats `https://a.com` and `https://a.com/` as equal, ignores the fragment, and is case-sensitive for the path. A trailing slash on a non-root path (`/x/` vs `/x`) makes the URIs unequal.
- If either string is not an absolute URI, the result is `false`.

**Exceptions**  
- `ArgumentNullException` if either `uri1` or `uri2` is `null`.

---

### `public static bool IsUrlSafe(this string? value)`
**Purpose**  
Checks whether a string contains only RFC 3986 unreserved characters, as used for client IDs, usernames, and similar identifiers.

**Parameters**  
- `value`: The string to validate.

**Return Value**  
`true` if `value` is non-blank and fully matches `^[a-zA-Z0-9\-._~]+$`; otherwise `false`.

**Exceptions**  
- `ArgumentNullException` if `value` is `null`. Blank strings return `false` instead.

---

### `public static string SafeTruncate(this string value, int maxLength)`
**Purpose**  
Truncates a string to at most `maxLength` characters. No ellipsis or other marker is added.

**Parameters**  
- `value`: The string to truncate.
- `maxLength`: The maximum number of characters to keep. Must be zero or greater.

**Return Value**  
The first `maxLength` characters of `value` if it is longer than `maxLength`; otherwise `value` unchanged.

**Exceptions**  
- `ArgumentNullException` if `value` is `null`.
- `ArgumentOutOfRangeException` if `maxLength` is negative.

---

### `public static string MaskSensitive(this string value)`
**Purpose**  
Masks a sensitive value for logging by keeping only its first and last three characters.

**Parameters**  
- `value`: The sensitive value to mask.

**Return Value**  
- `"***"` if `value` is 8 characters or shorter (including the empty string). Nothing of a short value is revealed.
- Otherwise `{first 3 characters}***{last 3 characters}`, for example `abcdefghi` becomes `abc***ghi`.

**Exceptions**  
- `ArgumentNullException` if `value` is `null`.

---

## Usage

### Example 1: Parsing and Rejoining Scopes
```csharp
string? requested = "openid profile  openid email";

IEnumerable<string> scopes = requested.ParseScopes();
// scopes yields: "openid", "profile", "email"

string normalized = scopes.JoinScopes();
// normalized == "openid profile email"
```

### Example 2: Validating a Redirect URI
```csharp
string? redirectUri = "https://example.com/callback";

if (redirectUri.IsValidAbsoluteUri() && redirectUri.UsesHttps())
{
    // Accept the redirect URI
}
```

### Example 3: Logging Safely
```csharp
string secret = "abcdefghi";
Console.WriteLine($"Token: {secret.MaskSensitive()}");   // Token: abc***ghi
Console.WriteLine(secret.SafeTruncate(4));               // abcd
```

---

## Notes

- **Null handling is not uniform.** `ParseScopes` and `UsesHttps` accept `null` and return an empty result or `false`. `IsValidAbsoluteUri`, `IsUrlSafe`, `UriEquals`, `JoinScopes`, `SafeTruncate`, and `MaskSensitive` throw `ArgumentNullException` on `null`. Check each entry's **Exceptions** section.
- **`IsUrlSafe` accepts a trailing newline.** The pattern uses `$`, which in .NET also matches before a final `\n`, so `"abc\n"` returns `true`. Callers that need a strict check should also verify the length or use `\z`.
- **`UriEquals` ignores fragments.** `https://a.com/x#f` and `https://a.com/x` compare equal.
- **`MaskSensitive` has no configurable visible length.** The visible portion is always three characters on each end.
- **Thread safety.** All methods are stateless and operate on immutable string data, so they are safe to call concurrently.
