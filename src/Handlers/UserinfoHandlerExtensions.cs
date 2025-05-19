#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

/// <summary>
/// Extension methods for <see cref="UserinfoHandler"/> to provide additional functionality
/// for working with user information responses.
/// </summary>
public static class UserinfoHandlerExtensions
{
    /// <summary>
    /// Creates a simplified display name from the user's given name and family name.
    /// Returns the full name if either part is missing.
    /// </summary>
    /// <param name="handler">The userinfo handler instance</param>
    /// <param name="response">The userinfo response</param>
    /// <returns>A display name suitable for UI presentation</returns>
    public static string GetDisplayName(this UserinfoResponse response)
    {
        if (string.IsNullOrWhiteSpace(response.GivenName) && string.IsNullOrWhiteSpace(response.FamilyName))
        {
            return response.Name ?? response.Sub ?? "Unknown User";
        }

        if (string.IsNullOrWhiteSpace(response.FamilyName))
        {
            return response.GivenName ?? response.Name ?? response.Sub ?? "Unknown User";
        }

        if (string.IsNullOrWhiteSpace(response.GivenName))
        {
            return response.FamilyName ?? response.Name ?? response.Sub ?? "Unknown User";
        }

        return $"{response.GivenName} {response.FamilyName}";
    }

    /// <summary>
    /// Checks if the user has verified their email address.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if email is verified or not provided, false if explicitly unverified</returns>
    public static bool HasVerifiedEmail(this UserinfoResponse response)
    {
        return response.EmailVerified ?? true;
    }

    /// <summary>
    /// Checks if the user has verified their phone number.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if phone number is verified or not provided, false if explicitly unverified</returns>
    public static bool HasVerifiedPhone(this UserinfoResponse response)
    {
        return response.PhoneNumberVerified ?? true;
    }

    /// <summary>
    /// Formats the user's full address as a single string.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>Formatted address string or null if no address information is available</returns>
    public static string? FormatAddress(this UserinfoResponse response)
    {
        if (response.Address is null)
        {
            return null;
        }

        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(response.Address.StreetAddress))
        {
            parts.Add(response.Address.StreetAddress);
        }

        if (!string.IsNullOrWhiteSpace(response.Address.Locality))
        {
            parts.Add(response.Address.Locality);
        }

        if (!string.IsNullOrWhiteSpace(response.Address.Region))
        {
            parts.Add(response.Address.Region);
        }

        if (!string.IsNullOrWhiteSpace(response.Address.PostalCode))
        {
            parts.Add(response.Address.PostalCode);
        }

        if (!string.IsNullOrWhiteSpace(response.Address.Country))
        {
            parts.Add(response.Address.Country);
        }

        return parts.Count > 0 ? string.Join(", ", parts) : null;
    }

    /// <summary>
    /// Determines if the userinfo response contains any profile information.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if any profile fields (name, given name, family name, updated at) are populated</returns>
    public static bool HasProfileInformation(this UserinfoResponse response)
    {
        return !string.IsNullOrWhiteSpace(response.Name) ||
               !string.IsNullOrWhiteSpace(response.GivenName) ||
               !string.IsNullOrWhiteSpace(response.FamilyName) ||
               response.UpdatedAt.HasValue;
    }

    /// <summary>
    /// Determines if the userinfo response contains any email information.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if email or email_verified fields are populated</returns>
    public static bool HasEmailInformation(this UserinfoResponse response)
    {
        return !string.IsNullOrWhiteSpace(response.Email) ||
               response.EmailVerified.HasValue;
    }

    /// <summary>
    /// Determines if the userinfo response contains any address information.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if any address fields are populated</returns>
    public static bool HasAddressInformation(this UserinfoResponse response)
    {
        return response.Address is not null &&
               (!string.IsNullOrWhiteSpace(response.Address.StreetAddress) ||
                !string.IsNullOrWhiteSpace(response.Address.Locality) ||
                !string.IsNullOrWhiteSpace(response.Address.Region) ||
                !string.IsNullOrWhiteSpace(response.Address.PostalCode) ||
                !string.IsNullOrWhiteSpace(response.Address.Country));
    }

    /// <summary>
    /// Determines if the userinfo response contains any phone information.
    /// </summary>
    /// <param name="response">The userinfo response</param>
    /// <returns>True if phone number or phone number verified fields are populated</returns>
    public static bool HasPhoneInformation(this UserinfoResponse response)
    {
        return !string.IsNullOrWhiteSpace(response.PhoneNumber) ||
               response.PhoneNumberVerified.HasValue;
    }
}