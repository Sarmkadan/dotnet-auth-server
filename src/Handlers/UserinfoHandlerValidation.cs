#nullable enable

namespace DotnetAuthServer.Handlers;

/// <summary>
/// Provides validation helpers for <see cref="UserinfoResponse"/> instances.
/// Validates required fields, format constraints, and semantic rules according to OIDC spec.
/// </summary>
public static class UserinfoHandlerValidation
{
    /// <summary>
    /// Validates a <see cref="UserinfoResponse"/> instance and returns a list of human-readable validation problems.
    /// Returns an empty list if the instance is valid.
    /// </summary>
    /// <param name="value">The instance to validate (must not be null).</param>
    /// <returns>A read-only list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this UserinfoResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Sub (required by OIDC spec)
        if (string.IsNullOrWhiteSpace(value.Sub))
        {
            errors.Add("Sub claim is required and cannot be null or whitespace.");
        }

        // Validate UpdatedAt (if provided, should be a reasonable timestamp)
        if (value.UpdatedAt is not null)
        {
            ValidateUpdatedAt(value.UpdatedAt, errors);
        }

        // Validate Email (if provided, should be a valid email format)
        if (!string.IsNullOrWhiteSpace(value.Email) && !IsValidEmail(value.Email))
        {
            errors.Add("Email format is invalid.");
        }

        // Validate EmailVerified (if Email is provided, EmailVerified should be consistent)
        if (value.EmailVerified is true && string.IsNullOrWhiteSpace(value.Email))
        {
            errors.Add("EmailVerified cannot be true when Email is not provided.");
        }

        // Validate PhoneNumber (if provided, should be a valid phone number format)
        if (!string.IsNullOrWhiteSpace(value.PhoneNumber) && !IsValidPhoneNumber(value.PhoneNumber))
        {
            errors.Add("PhoneNumber format is invalid.");
        }

        // Validate PhoneNumberVerified (if PhoneNumber is provided, PhoneNumberVerified should be consistent)
        if (value.PhoneNumberVerified is true && string.IsNullOrWhiteSpace(value.PhoneNumber))
        {
            errors.Add("PhoneNumberVerified cannot be true when PhoneNumber is not provided.");
        }

        // Validate Address properties if Address is provided
        if (value.Address is not null)
        {
            ValidateAddress(value.Address, errors);
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="UserinfoResponse"/> instance is valid.
    /// </summary>
    /// <param name="value">The instance to check (must not be null).</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this UserinfoResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="UserinfoResponse"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with a detailed message listing all validation problems if invalid.
    /// </summary>
    /// <param name="value">The instance to validate (must not be null).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid.</exception>
    public static void EnsureValid(this UserinfoResponse value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"UserinfoResponse validation failed:{Environment.NewLine}- {
                string.Join($"{Environment.NewLine}- ", errors)}",
                nameof(value));
        }
    }

    private static void ValidateAddress(AddressInfo address, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(address);

        // At least one address component should be provided if Address is present
        var hasAddressComponents = !string.IsNullOrWhiteSpace(address.StreetAddress) ||
            !string.IsNullOrWhiteSpace(address.Locality) ||
            !string.IsNullOrWhiteSpace(address.Region) ||
            !string.IsNullOrWhiteSpace(address.PostalCode) ||
            !string.IsNullOrWhiteSpace(address.Country);

        if (!hasAddressComponents)
        {
            errors.Add("Address must contain at least one non-empty component (StreetAddress, Locality, Region, PostalCode, or Country).");
        }

        // Validate individual address components for reasonable content using pattern matching
        ValidateAddressComponent(address.StreetAddress, "StreetAddress", 200, errors);
        ValidateAddressComponent(address.Locality, "Locality", 100, errors);
        ValidateAddressComponent(address.Region, "Region", 100, errors);
        ValidateAddressComponent(address.PostalCode, "PostalCode", 20, errors);
        ValidateAddressComponent(address.Country, "Country", 100, errors);
    }

    private static void ValidateAddressComponent(string? component, string componentName, int maxLength, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(component))
        {
            return;
        }

        if (component.Length > maxLength)
        {
            errors.Add($"Address.{componentName} exceeds maximum length of {maxLength} characters.");
        }

        // Additional validation: check for control characters or invalid characters
        if (component.Any(c => char.IsControl(c) || (char.IsSeparator(c) && c != ' ')))
        {
            errors.Add($"Address.{componentName} contains invalid characters.");
        }
    }

    private static void ValidateUpdatedAt(long? timestamp, List<string> errors)
    {
        // Unix timestamp should be positive (after 1970-01-01)
        if (timestamp < 0)
        {
            errors.Add("UpdatedAt timestamp cannot be negative.");
        }

        // Reasonable upper bound: year 2100
        const long maxUnixTimestamp = 4102444800; // 2100-01-01
        if (timestamp > maxUnixTimestamp)
        {
            errors.Add("UpdatedAt timestamp appears to be in the far future.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Validate email format according to RFC 5322 standards
            // This is a simplified but robust validation that catches most common errors
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex >= email.Length - 1)
            {
                return false;
            }

            var domainPart = email[(atIndex + 1)..];
            if (string.IsNullOrWhiteSpace(domainPart) || !domainPart.Contains('.') || domainPart.Length < 3)
            {
                return false;
            }

            // Ensure domain doesn't start or end with a dot
            if (domainPart.StartsWith('.') || domainPart.EndsWith('.'))
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            // Defensive programming - any unexpected error means invalid email
            return false;
        }
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return false;
        }

        try
        {
            // Validate phone number format - should contain at least 6 digits
            // Accepts common formats: +1 (555) 123-4567, 1-555-123-4567, 5551234567, etc.
            var digitsOnly = new string(phoneNumber.Where(c => char.IsDigit(c)).ToArray());

            // Additional validation: phone number should not be all the same digit
            if (digitsOnly.Distinct().Count() == 1 && digitsOnly.Length >= 6)
            {
                return false; // Likely a fake/test number
            }

            // Reasonable length bounds: 6-15 digits is typical for international numbers
            if (digitsOnly.Length is < 6 or > 15)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            // Defensive programming - any unexpected error means invalid phone number
            return false;
        }
    }
}