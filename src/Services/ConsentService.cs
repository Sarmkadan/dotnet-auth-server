// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Service for managing user consent to scope access
/// </summary>
public interface IConsentRepository : IRepository<Consent, string>
{
    /// <summary>
    /// Gets consent for a specific user and client
    /// </summary>
    Task<Consent?> GetByUserAndClientAsync(string userId, string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents for a user
    /// </summary>
    Task<IEnumerable<Consent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all consents for a client
    /// </summary>
    Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all consents for a user
    /// </summary>
    Task RevokeAllUserConsentsAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of consent repository
/// </summary>
public class ConsentRepository : IConsentRepository
{
    private readonly Dictionary<string, Consent> _consents = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Consent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_consents.TryGetValue(id, out var consent) ? consent : null);
    }

    public async Task<IEnumerable<Consent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_consents.Values.ToList());
    }

    public async Task<Consent> CreateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (_consents.ContainsKey(entity.ConsentId))
            throw new InvalidOperationException($"Consent with ID {entity.ConsentId} already exists");

        _consents[entity.ConsentId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<Consent> UpdateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (!_consents.ContainsKey(entity.ConsentId))
            throw new InvalidOperationException($"Consent with ID {entity.ConsentId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _consents[entity.ConsentId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.ConsentId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _consents.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_consents.ContainsKey(id));
    }

    public async Task<Consent?> GetByUserAndClientAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var consent = _consents.Values.FirstOrDefault(c =>
            c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
            c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(consent);
    }

    public async Task<IEnumerable<Consent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var consents = _consents.Values.Where(c =>
            c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(consents);
    }

    public async Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var consents = _consents.Values.Where(c =>
            c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(consents);
    }

    public async Task RevokeAllUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var userConsents = _consents.Values.Where(c =>
            c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var consent in userConsents)
        {
            consent.Revoke("User revoked all consents");
        }

        return await Task.CompletedTask;
    }
}

/// <summary>
/// Service for managing user consent decisions
/// </summary>
public class ConsentService
{
    private readonly IConsentRepository _consentRepository;

    public ConsentService(IConsentRepository consentRepository)
    {
        _consentRepository = consentRepository;
    }

    /// <summary>
    /// Checks if user has already granted consent for a client and scopes
    /// </summary>
    public async Task<bool> HasConsentAsync(
        string userId,
        string clientId,
        IEnumerable<string> requestedScopes,
        CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndClientAsync(userId, clientId, cancellationToken);

        if (consent == null || !consent.IsValidAndApproved())
            return false;

        // Check if all requested scopes are in granted scopes
        var grantedScopes = consent.GetGrantedScopes();
        return requestedScopes.All(scope =>
            grantedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Records a user's consent decision
    /// </summary>
    public async Task<Consent> RecordConsentAsync(
        ConsentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "Consent request is invalid",
                400);

        var consentId = Guid.NewGuid().ToString();
        var consent = new Consent
        {
            ConsentId = consentId,
            UserId = request.UserId!,
            ClientId = request.ClientId!,
            GrantedScopes = request.Approved ? request.GetScopesString() : "",
            IpAddress = request.IpAddress,
            UserAgent = request.UserAgent
        };

        if (request.Approved)
        {
            consent.Grant(request.GetScopesString(), request.IpAddress, request.UserAgent);
        }
        else
        {
            consent.Deny(request.DenialReason);
        }

        // If remembering consent, set it to not expire
        if (!request.RememberConsent)
        {
            consent.ExpiresAt = DateTime.UtcNow.AddHours(1); // Session-based consent
        }

        await _consentRepository.CreateAsync(consent, cancellationToken);
        return consent;
    }

    /// <summary>
    /// Gets the effective scopes user should be granted (subset of requested that user consented to)
    /// </summary>
    public async Task<IEnumerable<string>> GetEffectiveScopesAsync(
        string userId,
        string clientId,
        IEnumerable<string> requestedScopes,
        CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndClientAsync(userId, clientId, cancellationToken);

        if (consent?.IsValidAndApproved() != true)
            return Enumerable.Empty<string>();

        var grantedScopes = consent.GetGrantedScopes();
        // Return intersection of requested and granted scopes
        return requestedScopes.Where(scope =>
            grantedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Revokes all consents for a user (e.g., on account closure)
    /// </summary>
    public async Task RevokeAllUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        await _consentRepository.RevokeAllUserConsentsAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Revokes consent for a specific client
    /// </summary>
    public async Task RevokeConsentAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var consent = await _consentRepository.GetByUserAndClientAsync(userId, clientId, cancellationToken);
        if (consent != null)
        {
            consent.Revoke("User revoked consent");
            await _consentRepository.UpdateAsync(consent, cancellationToken);
        }
    }
}
