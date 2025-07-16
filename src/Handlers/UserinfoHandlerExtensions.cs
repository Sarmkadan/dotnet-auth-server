#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

/// <summary>
/// Extension methods for <see cref="UserinfoResponse"/> to provide additional functionality
/// for working with user information responses.
/// </summary>
public static class UserinfoHandlerExtensions
{
    /// <summary>
    /// Creates a simplified display name from the user's given name and family name.
    /// Returns the full name if either part is missing.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>A display name suitable for UI presentation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static string GetDisplayName(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response switch
        {
            { GivenName: null or "", FamilyName: null or "" } => response.Name ?? response.Sub ?? "Unknown User",
            { FamilyName: null or "" } => response.GivenName ?? response.Name ?? response.Sub ?? "Unknown User",
            { GivenName: null or "" } => response.FamilyName ?? response.Name ?? response.Sub ?? "Unknown User",
            _ => $"{response.GivenName} {response.FamilyName}"
        };
    }

    /// <summary>
    /// Checks if the user has verified their email address.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if email is verified; false if explicitly unverified or not provided.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasVerifiedEmail(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.EmailVerified ?? false;
    }

    /// <summary>
    /// Checks if the user has verified their phone number.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if phone number is verified; false if explicitly unverified or not provided.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasVerifiedPhone(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return response.PhoneNumberVerified ?? false;
    }

    /// <summary>
    /// Formats the user's full address as a single string.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>Formatted address string or null if no address information is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static string? FormatAddress(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        return response.Address is null
            ? null
            : string.Join(", ", new[]
            {
                response.Address.StreetAddress,
                response.Address.Locality,
                response.Address.Region,
                response.Address.PostalCode,
                response.Address.Country
            }.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    /// <summary>
    /// Determines if the userinfo response contains any profile information.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if any profile fields (name, given name, family name, updated at) are populated.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasProfileInformation(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return !string.IsNullOrWhiteSpace(response.Name) ||
               !string.IsNullOrWhiteSpace(response.GivenName) ||
               !string.IsNullOrWhiteSpace(response.FamilyName) ||
               response.UpdatedAt.HasValue;
    }

    /// <summary>
    /// Determines if the userinfo response contains any email information.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if email or email_verified fields are populated.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasEmailInformation(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return !string.IsNullOrWhiteSpace(response.Email) ||
               response.EmailVerified.HasValue;
    }

    /// <summary>
    /// Determines if the userinfo response contains any address information.
    /// </summary>
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if any address fields are populated.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasAddressInformation(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
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
    /// <param name="response">The userinfo response (must not be null).</param>
    /// <returns>True if phone number or phone number verified fields are populated.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="response"/> is null.</exception>
    public static bool HasPhoneInformation(this UserinfoResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        return !string.IsNullOrWhiteSpace(response.PhoneNumber) ||
               response.PhoneNumberVerified.HasValue;
    }
}