# PasswordValidationService

Provides validation for user passwords according to configurable rules, including length constraints, character composition, and checks against common patterns, historical passwords, and username variations.

## API

### `public PasswordValidationService`

Initializes a new instance of the `PasswordValidationService` with default validation rules.

### `public IReadOnlyList<string> ValidatePassword(string password)`

Validates the specified password against all configured rules and returns a list of validation error messages. Returns an empty list if the password is valid.

- **Parameters**
  - `password` – The password to validate.
- **Return Value**
  - A read-only list of error messages describing validation failures.
- **Throws**
  - `ArgumentNullException` – If `password` is `null`.

### `public void ValidateAndThrow(string password)`

Validates the specified password and throws a `PasswordValidationException` if validation fails.

- **Parameters**
  - `password` – The password to validate.
- **Throws**
  - `PasswordValidationException` – If the password fails any validation rule.
  - `ArgumentNullException` – If `password` is `null`.

### `public bool RequireMinimumLength`

Gets or sets a value indicating whether a minimum password length is required.

### `public int MinimumLength`

Gets or sets the minimum required password length. Must be greater than zero if `RequireMinimumLength` is `true`.

### `public bool RequireMaximumLength`

Gets or sets a value indicating whether a maximum password length is required.

### `public int MaximumLength`

Gets or sets the maximum allowed password length. Must be greater than `MinimumLength` if `RequireMaximumLength` is `true`.

### `public bool RequireLowercase`

Gets or sets a value indicating whether the password must contain at least one lowercase letter.

### `public bool RequireUppercase`

Gets or sets a value indicating whether the password must contain at least one uppercase letter.

### `public bool RequireDigit`

Gets or sets a value indicating whether the password must contain at least one digit.

### `public bool RequireSpecialChar`

Gets or sets a value indicating whether the password must contain at least one special character.

### `public bool RequireNotEqualToUsername`

Gets or sets a value indicating whether the password must not match the username or common variations.

### `public bool CheckUsernameVariations`

Gets or sets a value indicating whether to check for common username variations (e.g., reversed, capitalized) when validating against the username.

### `public bool CheckCommonPatterns`

Gets or sets a value indicating whether to check for common weak patterns (e.g., "password", "123456").

### `public bool CheckAgainstHistory`

Gets or sets a value indicating whether to check the password against historical passwords.

### `public string? HistoryCheckPassword`

Gets or sets the password to compare against historical entries when `CheckAgainstHistory` is `true`.

### `public ICollection<string> CommonPatterns`

Gets the collection of common patterns to check against when `CheckCommonPatterns` is `true`.

## Usage

### Example 1: Basic Validation with Default Rules
