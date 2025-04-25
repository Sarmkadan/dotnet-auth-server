// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Repository for Scope entity operations
/// </summary>
public interface IScopeRepository : IRepository<Scope, string>
{
    /// <summary>
    /// Gets a scope by scope ID
    /// </summary>
    Task<Scope?> GetByScopeIdAsync(string scopeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active scopes
    /// </summary>
    Task<IEnumerable<Scope>> GetActiveScopesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches scopes by name or description
    /// </summary>
    Task<IEnumerable<Scope>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of scope repository
/// </summary>
public class ScopeRepository : IScopeRepository
{
    private readonly Dictionary<string, Scope> _scopes = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Scope?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_scopes.TryGetValue(id, out var scope) ? scope : null);
    }

    public async Task<IEnumerable<Scope>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_scopes.Values.ToList());
    }

    public async Task<Scope> CreateAsync(Scope entity, CancellationToken cancellationToken = default)
    {
        if (_scopes.ContainsKey(entity.ScopeId))
            throw new InvalidOperationException($"Scope with ID {entity.ScopeId} already exists");

        _scopes[entity.ScopeId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<Scope> UpdateAsync(Scope entity, CancellationToken cancellationToken = default)
    {
        if (!_scopes.ContainsKey(entity.ScopeId))
            throw new InvalidOperationException($"Scope with ID {entity.ScopeId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _scopes[entity.ScopeId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(Scope entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.ScopeId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _scopes.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_scopes.ContainsKey(id));
    }

    public async Task<Scope?> GetByScopeIdAsync(string scopeId, CancellationToken cancellationToken = default)
    {
        var scope = _scopes.Values.FirstOrDefault(s =>
            s.ScopeId.Equals(scopeId, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(scope);
    }

    public async Task<IEnumerable<Scope>> GetActiveScopesAsync(CancellationToken cancellationToken = default)
    {
        var scopes = _scopes.Values.Where(s => s.IsActive).ToList();
        return await Task.FromResult(scopes);
    }

    public async Task<IEnumerable<Scope>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLower();
        var results = _scopes.Values.Where(s =>
            s.ScopeId.ToLower().Contains(lowerQuery) ||
            s.DisplayName.ToLower().Contains(lowerQuery) ||
            s.Description.ToLower().Contains(lowerQuery)).ToList();
        return await Task.FromResult(results);
    }
}

/// <summary>
/// Service for managing OAuth2 scopes
/// </summary>
public class ScopeService
{
    private readonly IScopeRepository _scopeRepository;

    public ScopeService(IScopeRepository scopeRepository)
    {
        _scopeRepository = scopeRepository;
    }

    /// <summary>
    /// Creates a new scope
    /// </summary>
    public async Task<Scope> CreateScopeAsync(
        string scopeId,
        string displayName,
        string description,
        bool isOpenIdScope = false,
        bool requiresConsent = true,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(scopeId) ||
            string.IsNullOrWhiteSpace(displayName) ||
            string.IsNullOrWhiteSpace(description))
            throw new AuthServerException(
                "invalid_request",
                "Scope ID, display name, and description are required",
                400);

        var existingScope = await _scopeRepository.GetByScopeIdAsync(scopeId, cancellationToken);
        if (existingScope != null)
            throw new AuthServerException(
                "invalid_request",
                $"Scope '{scopeId}' already exists",
                400);

        var scope = new Scope
        {
            ScopeId = scopeId,
            DisplayName = displayName,
            Description = description,
            IsOpenIdScope = isOpenIdScope,
            RequiresConsent = requiresConsent,
            IsActive = true
        };

        return await _scopeRepository.CreateAsync(scope, cancellationToken);
    }

    /// <summary>
    /// Adds a claim to a scope
    /// </summary>
    public async Task AddClaimToScopeAsync(
        string scopeId,
        string claim,
        bool isIdTokenClaim = true,
        CancellationToken cancellationToken = default)
    {
        var scope = await _scopeRepository.GetByScopeIdAsync(scopeId, cancellationToken);
        if (scope == null)
            throw new AuthServerException(
                "invalid_request",
                $"Scope '{scopeId}' not found",
                404);

        if (isIdTokenClaim)
        {
            if (!scope.IdTokenClaims.Contains(claim))
                scope.IdTokenClaims.Add(claim);
        }
        else
        {
            if (!scope.AccessTokenClaims.Contains(claim))
                scope.AccessTokenClaims.Add(claim);
        }

        await _scopeRepository.UpdateAsync(scope, cancellationToken);
    }

    /// <summary>
    /// Assigns a scope to specific roles (for RBAC)
    /// </summary>
    public async Task AssignRoleAsync(
        string scopeId,
        string role,
        CancellationToken cancellationToken = default)
    {
        var scope = await _scopeRepository.GetByScopeIdAsync(scopeId, cancellationToken);
        if (scope == null)
            throw new AuthServerException(
                "invalid_request",
                $"Scope '{scopeId}' not found",
                404);

        if (!scope.AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase))
            scope.AllowedRoles.Add(role);

        await _scopeRepository.UpdateAsync(scope, cancellationToken);
    }

    /// <summary>
    /// Gets all scopes with claims
    /// </summary>
    public async Task<IEnumerable<ScopeSummary>> GetScopesWithClaimsAsync(
        CancellationToken cancellationToken = default)
    {
        var scopes = await _scopeRepository.GetActiveScopesAsync(cancellationToken);

        return scopes.Select(s => new ScopeSummary
        {
            ScopeId = s.ScopeId,
            DisplayName = s.DisplayName,
            Description = s.Description,
            IsOpenIdScope = s.IsOpenIdScope,
            RequiresConsent = s.RequiresConsent,
            Claims = s.GetAllClaims().ToList()
        });
    }

    /// <summary>
    /// Validates requested scopes and returns those that user can access
    /// </summary>
    public async Task<IEnumerable<string>> ValidateAndFilterScopesAsync(
        IEnumerable<string> requestedScopes,
        IEnumerable<string> userRoles,
        CancellationToken cancellationToken = default)
    {
        var validScopes = new List<string>();

        foreach (var scopeId in requestedScopes)
        {
            var scope = await _scopeRepository.GetByScopeIdAsync(scopeId, cancellationToken);
            if (scope != null && scope.IsActive && scope.CanUserAccessScope(userRoles))
            {
                validScopes.Add(scopeId);
            }
        }

        return validScopes;
    }
}

/// <summary>
/// Summary view of a scope
/// </summary>
public class ScopeSummary
{
    public string ScopeId { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool IsOpenIdScope { get; set; }
    public bool RequiresConsent { get; set; }
    public IEnumerable<string> Claims { get; set; } = [];
}
