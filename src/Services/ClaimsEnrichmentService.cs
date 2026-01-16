// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Claims;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Service for enriching JWT tokens with custom claims from user profiles and roles.
/// Transforms user entities into claims that can be embedded in tokens.
/// Supports both standard OIDC claims and custom application claims.
/// </summary>
public class ClaimsEnrichmentService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ClaimsEnrichmentService> _logger;

    public ClaimsEnrichmentService(IUserRepository userRepository, ILogger<ClaimsEnrichmentService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    /// <summary>
    /// Creates a list of claims from a user for JWT token embedding.
    /// Includes standard OIDC claims and application-specific claims based on scopes.
    /// </summary>
    public async Task<List<Claim>> EnrichUserClaimsAsync(
        User user,
        IEnumerable<string> grantedScopes,
        CancellationToken cancellationToken = default)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        var claims = new List<Claim>();
        var scopeList = grantedScopes?.ToList() ?? new List<string>();

        // Always include identity claims
        claims.Add(new Claim(ClaimTypes.NameIdentifier, user.UserId));
        claims.Add(new Claim("sub", user.UserId));

        // Conditional claims based on granted scopes
        if (scopeList.Contains("profile"))
        {
            EnrichProfileClaims(claims, user);
        }

        if (scopeList.Contains("email"))
        {
            EnrichEmailClaims(claims, user);
        }

        if (scopeList.Contains("phone"))
        {
            EnrichPhoneClaims(claims, user);
        }

        if (scopeList.Contains("address"))
        {
            EnrichAddressClaims(claims, user);
        }

        // Add role claims
        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
            claims.Add(new Claim("roles", role)); // Alternative claim format
        }

        // Add application-specific claims based on user attributes
        EnrichApplicationClaims(claims, user);

        _logger.LogDebug(
            "Enriched claims for user {UserId}: {ClaimCount} claims added",
            user.UserId,
            claims.Count);

        return claims;
    }

    /// <summary>
    /// Adds profile-related claims (name, given_name, family_name, etc.)
    /// </summary>
    private void EnrichProfileClaims(List<Claim> claims, User user)
    {
        if (!string.IsNullOrWhiteSpace(user.DisplayName))
        {
            claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));
            claims.Add(new Claim("name", user.DisplayName));
        }

        if (!string.IsNullOrWhiteSpace(user.FirstName))
        {
            claims.Add(new Claim("given_name", user.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(user.LastName))
        {
            claims.Add(new Claim("family_name", user.LastName));
        }

        if (user.LastModifiedAt.HasValue)
        {
            var timestamp = (long?)user.LastModifiedAt.Value.ToUniversalTime()
                .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalSeconds;
            if (timestamp.HasValue)
            {
                claims.Add(new Claim("updated_at", timestamp.Value.ToString()));
            }
        }
    }

    /// <summary>
    /// Adds email-related claims.
    /// </summary>
    private void EnrichEmailClaims(List<Claim> claims, User user)
    {
        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
            claims.Add(new Claim("email", user.Email));
            claims.Add(new Claim("email_verified", user.EmailVerified.ToString().ToLower()));
        }
    }

    /// <summary>
    /// Adds phone-related claims.
    /// </summary>
    private void EnrichPhoneClaims(List<Claim> claims, User user)
    {
        // Phone claims would come from extended user properties
        // Placeholder for typical phone claim
        claims.Add(new Claim("phone_number_verified", "false"));
    }

    /// <summary>
    /// Adds address-related claims.
    /// </summary>
    private void EnrichAddressClaims(List<Claim> claims, User user)
    {
        // Address claims would come from extended user properties
        // Placeholder for typical address claim format
    }

    /// <summary>
    /// Adds application-specific custom claims.
    /// Can be extended to include domain-specific user attributes.
    /// </summary>
    private void EnrichApplicationClaims(List<Claim> claims, User user)
    {
        // Add organization if applicable
        if (!string.IsNullOrWhiteSpace(user.Username))
        {
            claims.Add(new Claim("preferred_username", user.Username));
        }

        // Add lock status info (useful for some apps)
        if (user.IsLocked())
        {
            claims.Add(new Claim("account_locked", "true"));
        }

        // Custom claim: account creation time
        var createdTimestamp = (long?)user.CreatedAt.ToUniversalTime()
            .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
            .TotalSeconds;
        if (createdTimestamp.HasValue)
        {
            claims.Add(new Claim("account_created", createdTimestamp.Value.ToString()));
        }
    }

    /// <summary>
    /// Filters claims based on scopes to ensure only authorized information is included.
    /// Implements data minimization principle from GDPR.
    /// </summary>
    public List<Claim> FilterClaimsByScope(List<Claim> claims, IEnumerable<string> grantedScopes)
    {
        var scopes = new HashSet<string>(grantedScopes ?? Enumerable.Empty<string>());
        var filtered = new List<Claim>();

        foreach (var claim in claims)
        {
            // Always include identity and technical claims
            if (claim.Type == ClaimTypes.NameIdentifier ||
                claim.Type == "sub" ||
                claim.Type == "iss" ||
                claim.Type == "aud" ||
                claim.Type == "iat" ||
                claim.Type == "exp")
            {
                filtered.Add(claim);
                continue;
            }

            // Filter other claims based on scope
            var isAllowed = claim.Type switch
            {
                ClaimTypes.Name or "name" or "given_name" or "family_name" or "updated_at" => scopes.Contains("profile"),
                ClaimTypes.Email or "email" or "email_verified" => scopes.Contains("email"),
                ClaimTypes.HomePhone or "phone_number" or "phone_number_verified" => scopes.Contains("phone"),
                "address" => scopes.Contains("address"),
                ClaimTypes.Role or "roles" => true, // Always include roles
                _ => true // Include custom claims by default
            };

            if (isAllowed)
            {
                filtered.Add(claim);
            }
        }

        return filtered;
    }
}
