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
public class UserinfoHandler
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserinfoHandler> _logger;

    public UserinfoHandler(IUserRepository userRepository, ILogger<UserinfoHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

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
        if (user == null)
        {
            _logger.LogWarning("Userinfo requested for unknown user {UserId}", userId);
            return null;
        }

        var scopes = ExtractScopes(principal);
        var response = new UserinfoResponse { Sub = userId };

        // 'openid' scope: always include sub, add other claims conditionally
        if (scopes.Contains("profile"))
        {
            response.Name = user.DisplayName ?? user.Username;
            response.GivenName = user.FirstName;
            response.FamilyName = user.LastName;
            response.UpdatedAt = (long?)user.LastModifiedAt?.ToUniversalTime()
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
public class UserinfoResponse
{
    public string Sub { get; set; } = string.Empty;

    // Profile scope
    public string? Name { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public long? UpdatedAt { get; set; }

    // Email scope
    public string? Email { get; set; }
    public bool? EmailVerified { get; set; }

    // Address scope
    public AddressInfo? Address { get; set; }

    // Phone scope
    public string? PhoneNumber { get; set; }
    public bool? PhoneNumberVerified { get; set; }
}

/// <summary>
/// OpenID Connect address information.
/// </summary>
public class AddressInfo
{
    public string? StreetAddress { get; set; }
    public string? Locality { get; set; }
    public string? Region { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
}
