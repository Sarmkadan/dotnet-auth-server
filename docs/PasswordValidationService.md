# PasswordValidationService

`PasswordValidationService` checks a plain-text password against a configurable password policy and returns human-readable error messages. `ValidateAndThrow` is a variant that throws an `AuthServerException` instead.

The class is `public sealed` and lives in `src/Services/PasswordValidationService.cs`, namespace `DotnetAuthServer.Services`. It has no interface. The same file defines the public `PasswordPolicyOptions` class, which holds the policy settings. The service only reads the policy; it never stores or logs the password.

## Role in the system

- Validates a new or changed password before it is hashed and stored.
- Reports every failing rule at once, so a client can show all problems in one pass.
- Maps policy failures to an OAuth `invalid_request` error (HTTP 400) when the caller uses `ValidateAndThrow`.

Policy is set once, when the service is constructed. The service does not re-read configuration.

## Constructor

### `public PasswordValidationService(AuthServerOptions options, PasswordPolicyOptions? passwordPolicy = null)`

Creates the service.

**Parameters:**
- `options` – the server options. Required; only checked for `null`. The validation rules do not read it.
- `passwordPolicy` – the policy to enforce. Optional. If `null`, a new `PasswordPolicyOptions` with all default values is used.

**Exceptions:** `ArgumentNullException` if `options` is `null`.

## Methods

### `public IReadOnlyList<string> ValidatePassword(string password, string? username = null)`

Runs every enabled rule and collects the messages for the rules that fail. It does not stop at the first failure. Rules run in the order listed below.

**Parameters:**
- `password` – the plain-text password. Must not be `null`. An empty string is not rejected by a separate rule; it fails the minimum-length rule if that rule is enabled.
- `username` – the username to compare against. Optional. The username rules run only when it is not `null`, empty, or whitespace-only.

**Return value:** a read-only list of error messages, in rule order. The list is empty when the password passes every enabled rule.

**Exceptions:** `ArgumentNullException` if `password` is `null`.

### `public void ValidateAndThrow(string password, string? username = null)`

Calls `ValidatePassword` with the same arguments. If any messages are returned, it joins them with `"; "` and throws an `AuthServerException`:

- `errorCode` – `Constants.ErrorCodes.InvalidRequest` (`invalid_request`)
- `message` – the joined messages
- `statusCode` – `400`

Because `errorDescription` is not passed, it defaults to the same joined message.

**Exceptions:**
- `AuthServerException` if the password fails one or more rules.
- `ArgumentNullException` if `password` is `null`.

Use this method where a failed check should go straight to the OAuth error middleware. Use `ValidatePassword` where the caller wants the list itself, for example to show every message in a form.

### `public override string ToString()`

Returns a one-line summary of the length and character-class settings. Only these values are included:

```
PasswordValidationService { RequireMinimumLength = ..., MinimumLength = ..., RequireMaximumLength = ..., MaximumLength = ..., RequireLowercase = ..., RequireUppercase = ... }
```

The digit, special-character, username, common-pattern and history settings are not included. The password is never included.

## Validation rules

Each rule is checked only when its flag in `PasswordPolicyOptions` is `true`. Messages are shown exactly as the code produces them.

| # | Flag | Check | Message |
|---|---|---|---|
| 1 | `RequireMinimumLength` | `password.Length < MinimumLength` | `Password must be at least {MinimumLength} characters long` |
| 2 | `RequireMaximumLength` | `password.Length > MaximumLength` | `Password must be no more than {MaximumLength} characters long` |
| 3 | `RequireLowercase` | no character satisfies `char.IsLower` | `Password must contain at least one lowercase letter (a-z)` |
| 4 | `RequireUppercase` | no character satisfies `char.IsUpper` | `Password must contain at least one uppercase letter (A-Z)` |
| 5 | `RequireDigit` | no character satisfies `char.IsDigit` | `Password must contain at least one digit (0-9)` |
| 6 | `RequireSpecialChar` | no character is outside `char.IsLetterOrDigit` | `Password must contain at least one special character (!@#$%^&* etc.)` |
| 7 | `RequireNotEqualToUsername` | `username` is not blank and equals `password` (ordinal, case-insensitive) | `Password cannot be the same as your username` |
| 8 | `RequireNotEqualToUsername` and `CheckUsernameVariations` | `username` is not blank and `password` equals `username + "123"`, `"123" + username`, or `username + username` (case-insensitive) | `Password cannot be based on your username or common variations` |
| 9 | `CheckCommonPatterns` | the lowercased password contains a pattern from `CommonPatterns` (lowercased) and `CommonPatterns` is not empty | `Password cannot contain common pattern: '{pattern}'` – one message per matching pattern |
| 10 | `CheckAgainstHistory` | `HistoryCheckPassword` is not blank and equals `password` (ordinal, case-sensitive) | `Password must be different from your previous password` |

Notes on the rules:

- **Length** is counted in UTF-16 code units (`string.Length`), not text elements.
- **Character classes** use the `char` helpers, so non-ASCII letters count as lowercase, uppercase, or letters. A "special character" is any character that is not a letter or digit, including spaces.
- **Rules 7 and 8** are skipped when `username` is `null`, empty, or whitespace. Rule 8 runs only if rule 7's flag is also `true`; `CheckUsernameVariations` has no effect on its own.
- **Rule 9** is a substring match. A password such as `MyPassword1!` matches `password` and produces one message. Several patterns can match, and then several messages are produced.
- **Rule 10** compares with the value stored in `HistoryCheckPassword`. The service does not hash or look up history itself; the caller must set this value.

## PasswordPolicyOptions

`PasswordPolicyOptions` is a `public sealed` class with settable properties. Defaults are shown below.

| Property | Type | Default | Meaning |
|---|---|---|---|
| `RequireMinimumLength` | `bool` | `true` | Enforce `MinimumLength`. |
| `MinimumLength` | `int` | `8` | Minimum number of characters. |
| `RequireMaximumLength` | `bool` | `false` | Enforce `MaximumLength`. |
| `MaximumLength` | `int` | `100` | Maximum number of characters. |
| `RequireLowercase` | `bool` | `false` | Require a lowercase letter. |
| `RequireUppercase` | `bool` | `false` | Require an uppercase letter. |
| `RequireDigit` | `bool` | `false` | Require a digit. |
| `RequireSpecialChar` | `bool` | `false` | Require a non-alphanumeric character. |
| `RequireNotEqualToUsername` | `bool` | `true` | Reject a password equal to the username. |
| `CheckUsernameVariations` | `bool` | `true` | Also reject `username123`, `123username`, `usernameusername`. Only used with `RequireNotEqualToUsername`. |
| `CheckCommonPatterns` | `bool` | `true` | Reject passwords containing any entry in `CommonPatterns`. |
| `CheckAgainstHistory` | `bool` | `false` | Reject a password equal to `HistoryCheckPassword`. |
| `HistoryCheckPassword` | `string?` | `null` | The previous password to compare against. Only used with `CheckAgainstHistory`. |
| `CommonPatterns` | `ICollection<string>` | see below | Substrings to reject. Matching is case-insensitive. |

The default `CommonPatterns` list is:

`password`, `123456`, `12345678`, `1234`, `qwerty`, `12345`, `dragon`, `baseball`, `football`, `letmein`, `monkey`, `abc123`, `mustang`, `michael`, `shadow`, `master`, `jennifer`, `111111`, `2000`, `jordan`, `superman`, `harley`, `1234567`, `freedom`, `whatever`, `trustno1`, `sunshine`, `iloveyou`, `starwars`, `login`, `admin`, `welcome`, `login123`, `password1`, `admin123`

Because `CheckCommonPatterns` is a substring check, short entries such as `2000`, `1234`, `admin` or `login` also reject longer passwords that contain them. Replace the list if this is too strict.

`PasswordPolicyOptions` does not validate its own values. For example, a `MinimumLength` of `0` or a `MaximumLength` smaller than `MinimumLength` is accepted, and the rules are applied as written. A `null` `CommonPatterns` causes a `NullReferenceException` when `CheckCommonPatterns` is `true`, so keep it non-null.

## Usage

```csharp
var policy = new PasswordPolicyOptions
{
    MinimumLength = 12,
    RequireUppercase = true,
    RequireLowercase = true,
    RequireDigit = true,
    RequireSpecialChar = true,
};

var validator = new PasswordValidationService(authServerOptions, policy);

// Collect every problem to show in a form
IReadOnlyList<string> errors = validator.ValidatePassword(candidate, user.UserName);

// Or fail fast with an OAuth-shaped error
validator.ValidateAndThrow(candidate, user.UserName);
```

## Security notes

- The service only checks the policy. It does not hash, store, or compare a password against a stored credential, so it has no timing-sensitive comparison of its own.
- Error messages describe the policy, not the password. The only password-derived text is a matched common pattern, which is taken from the policy, not from the input.
- The service does not log. Callers that log validation failures should log the rule messages, not the password.
- Rule 10 requires the caller to supply the previous password in plain text. Keep `PasswordPolicyOptions` out of logs and serialized output, since `HistoryCheckPassword` holds that value.
