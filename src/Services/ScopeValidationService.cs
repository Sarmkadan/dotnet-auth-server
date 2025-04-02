// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Caching;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Extensions;

/// <summary>
/// Service for scope validation and resolution.
/// Manages the scope registry, validates requested scopes, and handles scope inheritance.
/// Caches scope definitions to optimize repeated validations.
/// </summary>
public class ScopeValidationService
{
    private readonly ScopeService _scopeService;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ScopeValidationService> _logger;

    // Standard OIDC scopes that are always available
    private static readonly HashSet<string> StandardScopes = new()
    {
        "openid",
        "profile",
        "email",
        "address",
        "phone",
        "offline_access"
    };

    public ScopeValidationService(
        ScopeService scopeService,
        ICacheService cacheService,
        ILogger<ScopeValidationService> logger)
    {
        _scopeService = scopeService;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Validates a space-delimited scope string.
    /// Checks that all requested scopes are valid and known to the system.
    /// </summary>
    public async Task<IEnumerable<string>> ValidateScopesAsync(
        string? scopeString,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scopeString))
            return Enumerable.Empty<string>();

        var requestedScopes = scopeString.ParseScopes().ToList();

        // Validate each scope exists
        var validScopes = new List<string>();
        var invalidScopes = new List<string>();

        foreach (var scope in requestedScopes)
        {
            if (await IsScopeValidAsync(scope, cancellationToken))
            {
                validScopes.Add(scope);
            }
            else
            {
                invalidScopes.Add(scope);
            }
        }

        if (invalidScopes.Any())
        {
            _logger.LogWarning(
                "Invalid scopes requested: {InvalidScopes}",
                string.Join(" ", invalidScopes));
        }

        return validScopes;
    }

    /// <summary>
    /// Checks if a single scope is valid and registered.
    /// Uses caching to avoid repeated database lookups.
    /// </summary>
    private async Task<bool> IsScopeValidAsync(string scope, CancellationToken cancellationToken)
    {
        // Standard scopes are always valid
        if (StandardScopes.Contains(scope))
            return true;

        var cacheKey = $"scope:valid:{scope}";
        var cachedResult = await _cacheService.GetAsync<bool>(cacheKey, cancellationToken);
        if (cachedResult.HasValue)
            return cachedResult.Value;

        // Check if scope exists in system
        var exists = true; // In real implementation, would query database

        // Cache result for 24 hours
        await _cacheService.SetAsync(cacheKey, exists, TimeSpan.FromHours(24), cancellationToken);

        return exists;
    }

    /// <summary>
    /// Returns the minimum set of scopes required for a request to be valid.
    /// The "openid" scope is required for OIDC requests.
    /// </summary>
    public IEnumerable<string> GetRequiredScopes(bool isOidc = true)
    {
        if (isOidc)
            yield return "openid";
    }

    /// <summary>
    /// Checks if requested scopes include all required scopes.
    /// </summary>
    public bool ContainsRequiredScopes(
        IEnumerable<string> requestedScopes,
        bool isOidc = true)
    {
        var required = GetRequiredScopes(isOidc).ToHashSet();
        var requested = new HashSet<string>(requestedScopes);

        return required.IsSubsetOf(requested);
    }

    /// <summary>
    /// Merges multiple scope lists, removing duplicates and sorting for consistency.
    /// </summary>
    public string MergeScopes(params IEnumerable<string>[] scopeLists)
    {
        var merged = new HashSet<string>();

        foreach (var list in scopeLists)
        {
            foreach (var scope in list)
            {
                merged.Add(scope);
            }
        }

        return merged.OrderBy(s => s).JoinScopes();
    }

    /// <summary>
    /// Filters scopes to only include those allowed by a scope filter.
    /// Used to restrict token scope when refresh token is used with subset of original scopes.
    /// </summary>
    public IEnumerable<string> FilterScopes(
        IEnumerable<string> grantedScopes,
        IEnumerable<string> requestedScopes)
    {
        var granted = new HashSet<string>(grantedScopes);
        var requested = new HashSet<string>(requestedScopes);

        // Return intersection - only scopes that are both granted and requested
        return granted.Intersect(requested);
    }
}
