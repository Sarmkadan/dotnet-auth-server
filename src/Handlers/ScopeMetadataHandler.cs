// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using DotnetAuthServer.Caching;

/// <summary>
/// Handler for retrieving and managing OAuth2 scope metadata.
/// Provides information about available scopes including descriptions and required consent.
/// Used by clients to display scope consent screens and by servers for validation.
/// </summary>
public class ScopeMetadataHandler
{
    private readonly ICacheService _cacheService;
    private readonly ILogger<ScopeMetadataHandler> _logger;

    // Standard OIDC scopes with descriptions
    private static readonly Dictionary<string, ScopeMetadata> StandardScopes = new()
    {
        {
            "openid", new ScopeMetadata
            {
                Name = "openid",
                DisplayName = "OpenID Connect",
                Description = "Verify your identity",
                RequiresConsent = false
            }
        },
        {
            "profile", new ScopeMetadata
            {
                Name = "profile",
                DisplayName = "Profile",
                Description = "Access your profile information (name, picture, etc.)",
                RequiresConsent = true
            }
        },
        {
            "email", new ScopeMetadata
            {
                Name = "email",
                DisplayName = "Email",
                Description = "Access your email address",
                RequiresConsent = true
            }
        },
        {
            "phone", new ScopeMetadata
            {
                Name = "phone",
                DisplayName = "Phone",
                Description = "Access your phone number",
                RequiresConsent = true
            }
        },
        {
            "address", new ScopeMetadata
            {
                Name = "address",
                DisplayName = "Address",
                Description = "Access your address",
                RequiresConsent = true
            }
        },
        {
            "offline_access", new ScopeMetadata
            {
                Name = "offline_access",
                DisplayName = "Offline Access",
                Description = "Stay logged in even when not using the application",
                RequiresConsent = true
            }
        }
    };

    public ScopeMetadataHandler(ICacheService cacheService, ILogger<ScopeMetadataHandler> logger)
    {
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets metadata for a specific scope.
    /// Returns null if scope is not recognized.
    /// </summary>
    public async Task<ScopeMetadata?> GetScopeMetadataAsync(
        string scopeName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scopeName))
            return null;

        var cacheKey = $"scope_metadata:{scopeName}";
        var cached = await _cacheService.GetAsync<ScopeMetadata>(cacheKey, cancellationToken);
        if (cached != null)
            return cached;

        ScopeMetadata? metadata = null;
        if (StandardScopes.TryGetValue(scopeName, out var standardScope))
        {
            metadata = standardScope;
        }

        if (metadata != null)
        {
            // Cache for 24 hours
            await _cacheService.SetAsync(cacheKey, metadata, TimeSpan.FromHours(24), cancellationToken);
        }

        return metadata;
    }

    /// <summary>
    /// Gets metadata for multiple scopes.
    /// </summary>
    public async Task<IEnumerable<ScopeMetadata>> GetScopesMetadataAsync(
        IEnumerable<string> scopeNames,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ScopeMetadata>();

        foreach (var scopeName in scopeNames)
        {
            var metadata = await GetScopeMetadataAsync(scopeName, cancellationToken);
            if (metadata != null)
            {
                results.Add(metadata);
            }
        }

        return results;
    }

    /// <summary>
    /// Gets all available scopes.
    /// </summary>
    public async Task<IEnumerable<ScopeMetadata>> GetAllScopesAsync(CancellationToken cancellationToken = default)
    {
        var scopes = new List<ScopeMetadata>();

        foreach (var scopeName in StandardScopes.Keys)
        {
            var metadata = await GetScopeMetadataAsync(scopeName, cancellationToken);
            if (metadata != null)
            {
                scopes.Add(metadata);
            }
        }

        return scopes;
    }

    /// <summary>
    /// Gets scopes that require user consent.
    /// Useful for displaying consent screens.
    /// </summary>
    public IEnumerable<ScopeMetadata> GetScopesRequiringConsent(IEnumerable<string> scopeNames)
    {
        return scopeNames
            .Where(name => StandardScopes.TryGetValue(name, out var meta) && meta.RequiresConsent)
            .Select(name => StandardScopes[name]);
    }

    /// <summary>
    /// Registers a custom scope metadata.
    /// Used for application-specific scopes beyond standard OIDC.
    /// </summary>
    public void RegisterCustomScope(ScopeMetadata metadata)
    {
        if (metadata == null || string.IsNullOrWhiteSpace(metadata.Name))
        {
            _logger.LogWarning("Cannot register scope with missing name");
            return;
        }

        StandardScopes[metadata.Name] = metadata;
        _logger.LogInformation("Custom scope registered: {ScopeName}", metadata.Name);
    }
}

/// <summary>
/// Metadata about an OAuth2 scope.
/// </summary>
public class ScopeMetadata
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresConsent { get; set; }
    public string? Icon { get; set; }
    public List<string> RelatedScopes { get; set; } = new();
}
