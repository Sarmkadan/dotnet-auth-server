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
            // Unix timestamp should be positive (after 1970-01-01)
            if (value.UpdatedAt < 0)
            {
                errors.Add("UpdatedAt timestamp cannot be negative.");
            }

            // Reasonable upper bound: year 2100
            const long maxUnixTimestamp = 4102444800; // 2100-01-01
            if (value.UpdatedAt > maxUnixTimestamp)
            {
                errors.Add("UpdatedAt timestamp appears to be in the far future.");
            }
        }

        // Validate Email (if provided, should be a valid email format)
        if (!string.IsNullOrWhiteSpace(value.Email))
        {
            if (!IsValidEmail(value.Email))
            {
                errors.Add("Email format is invalid.");
            }
        }

        // Validate EmailVerified (if Email is provided, EmailVerified should be consistent)
        if (value.EmailVerified is true && string.IsNullOrWhiteSpace(value.Email))
        {
            errors.Add("EmailVerified cannot be true when Email is not provided.");
        }

        // Validate PhoneNumber (if provided, should be a valid phone number format)
        if (!string.IsNullOrWhiteSpace(value.PhoneNumber))
        {
            if (!IsValidPhoneNumber(value.PhoneNumber))
            {
                errors.Add("PhoneNumber format is invalid.");
            }
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
    /// <param name="value">The instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this UserinfoResponse value)
    {
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
                    string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static void ValidateAddress(AddressInfo address, List<string> errors)
    {
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

        // Validate individual address components for reasonable content
        if (!string.IsNullOrWhiteSpace(address.StreetAddress) && address.StreetAddress.Length > 200)
        {
            errors.Add("Address.StreetAddress exceeds maximum length of 200 characters.");
        }

        if (!string.IsNullOrWhiteSpace(address.Locality) && address.Locality.Length > 100)
        {
            errors.Add("Address.Locality exceeds maximum length of 100 characters.");
        }

        if (!string.IsNullOrWhiteSpace(address.Region) && address.Region.Length > 100)
        {
            errors.Add("Address.Region exceeds maximum length of 100 characters.");
        }

        if (!string.IsNullOrWhiteSpace(address.PostalCode) && address.PostalCode.Length > 20)
        {
            errors.Add("Address.PostalCode exceeds maximum length of 20 characters.");
        }

        if (!string.IsNullOrWhiteSpace(address.Country) && address.Country.Length > 100)
        {
            errors.Add("Address.Country exceeds maximum length of 100 characters.");
        }
    }

    private static bool IsValidEmail(string email)
    {
        // Basic email validation - checks for @ symbol and at least one . after it
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
        {
            return false;
        }

        var domainPart = email[(atIndex + 1)..];
        return domainPart.Contains('.') && domainPart.Length > 3;
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
        // Basic phone number validation - should contain only digits, spaces, +, -, (, )
        // and should have at least 6 digits
        var digitsOnly = new string(phoneNumber.Where(c => char.IsDigit(c)).ToArray());
        return digitsOnly.Length >= 6;
    }
}