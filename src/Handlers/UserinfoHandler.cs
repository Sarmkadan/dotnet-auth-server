#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using System.Collections.Generic;
using System.Security.Claims;
using DotnetAuthServer.Data.Repositories;

/// <summary>
/// Handler for OpenID Connect userinfo endpoint (OIDC spec).
/// Returns claims about the authenticated user based on their access token.
/// Scope claims control what information is returned (openid, profile, email, etc.)
/// </summary>
public sealed class UserinfoHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserinfoHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserinfoHandler"/> class.
    /// </summary>
    /// <param name="userRepository">Repository used to load the user.</param>
    /// <param name="logger">Logger used to record userinfo requests.</param>
    public UserinfoHandler(IUserRepository userRepository, ILogger<UserinfoHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Returns the handler name.
    /// </summary>
    /// <returns>The string "UserinfoHandler".</returns>
    public override string ToString() => "UserinfoHandler";

    /// <summary>
    /// Retrieves user information based on claims from an access token.
    /// Only returns claims that are allowed by the token's scope.
    /// </summary>
    public async Task<UserinfoResponse?> GetUserinfoAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirst("sub")?.Value;
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Userinfo request without valid subject claim");
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            _logger.LogWarning("Userinfo requested for unknown user {UserId}", userId);
            return null;
        }

        var scopes = ExtractScopes(principal);
        var response = new UserinfoResponse { Sub = userId };

        // 'openid' scope: always include sub, add other claims conditionally
        if (scopes.Contains("profile"))
        {
            var nameParts = user.FullName?.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            response.Name = user.FullName ?? user.Username;
            response.GivenName = nameParts?.FirstOrDefault();
            response.FamilyName = nameParts?.LastOrDefault();
            response.UpdatedAt = (long?)user.UpdatedAt.ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalSeconds ?? 0;
        }

        if (scopes.Contains("email"))
        {
            response.Email = user.Email;
            response.EmailVerified = user.EmailVerified;
        }

        if (scopes.Contains("address"))
        {
            // Address claims would be populated from user extended info
            // Placeholder for typical address claims
        }

        if (scopes.Contains("phone"))
        {
            // Phone number claims
        }

        _logger.LogInformation(
            "Userinfo returned for user {UserId} with scopes {Scopes}",
            userId,
            string.Join(" ", scopes));

        return response;
    }

    private static HashSet<string> ExtractScopes(ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst("scope")?.Value;
        var scopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(scopeClaim))
        {
            foreach (var scope in scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                scopes.Add(scope);
            }
        }

        return scopes;
    }
}

/// <summary>
/// OpenID Connect UserInfo response model.
/// Only populated fields that are allowed by token scopes.
/// </summary>
public sealed class UserinfoResponse
{
    /// <summary>
    /// Subject identifier of the user.
    /// </summary>
    public string Sub { get; set; } = string.Empty;

    // Profile scope

    /// <summary>
    /// Full name of the user.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Given (first) name of the user.
    /// </summary>
    public string? GivenName { get; set; }

    /// <summary>
    /// Family (last) name of the user.
    /// </summary>
    public string? FamilyName { get; set; }

    /// <summary>
    /// Time the user's information was last updated, as seconds since the Unix epoch.
    /// </summary>
    public long? UpdatedAt { get; set; }

    // Email scope

    /// <summary>
    /// Email address of the user.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Whether the email address has been verified.
    /// </summary>
    public bool? EmailVerified { get; set; }

    // Address scope

    /// <summary>
    /// Postal address of the user.
    /// </summary>
    public AddressInfo? Address { get; set; }

    // Phone scope

    /// <summary>
    /// Phone number of the user.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Whether the phone number has been verified.
    /// </summary>
    public bool? PhoneNumberVerified { get; set; }

    /// <summary>
    /// Returns a string representation of this userinfo response.
    /// </summary>
    /// <returns>A string representation of the response.</returns>
    public override string ToString() => $"UserinfoHandler {{ Sub = {Sub}, Name = {Name}, GivenName = {GivenName}, FamilyName = {FamilyName}, UpdatedAt = {UpdatedAt}, Email = {Email} }}";
}

/// <summary>
/// OpenID Connect address information.
/// </summary>
public sealed class AddressInfo
{
    /// <summary>
    /// Street address component, including house number.
    /// </summary>
    public string? StreetAddress { get; set; }

    /// <summary>
    /// City or locality component.
    /// </summary>
    public string? Locality { get; set; }

    /// <summary>
    /// State, province, or region component.
    /// </summary>
    public string? Region { get; set; }

    /// <summary>
    /// Postal or zip code component.
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// Country component.
    /// </summary>
    public string? Country { get; set; }
}
