// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Caching;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Extensions;

/// <summary>
/// Service for comprehensive client validation during OAuth2 flows.
/// Caches client information to improve performance and reduce database queries.
/// Validates client credentials, redirect URIs, allowed scopes, and other requirements.
/// </summary>
public class ClientValidationService
{
    private readonly IClientRepository _clientRepository;
    private readonly ICacheService _cacheService;
    private readonly ILogger<ClientValidationService> _logger;

    public ClientValidationService(
        IClientRepository clientRepository,
        ICacheService cacheService,
        ILogger<ClientValidationService> logger)
    {
        _clientRepository = clientRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Validates client credentials (client_id and optionally client_secret).
    /// Authenticates the client and checks if it's active.
    /// </summary>
    public async Task<Client> ValidateClientCredentialsAsync(
        string? clientId,
        string? clientSecret,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            _logger.LogWarning("Client validation requested without client_id");
            throw new InvalidClientException("client_id is required");
        }

        var client = await GetClientAsync(clientId, cancellationToken);
        if (client == null || !client.IsActive)
        {
            _logger.LogWarning("Client {ClientId} not found or inactive", clientId);
            throw new InvalidClientException("Client not found or inactive");
        }

        // Validate secret for confidential clients
        if (client.IsConfidential)
        {
            if (string.IsNullOrWhiteSpace(clientSecret))
            {
                _logger.LogWarning("Confidential client {ClientId} provided no secret", clientId);
                throw new InvalidClientException("Client secret is required");
            }

            // Compare secrets securely (constant-time comparison)
            if (!SecureStringCompare(clientSecret, client.ClientSecret))
            {
                _logger.LogWarning("Invalid secret provided for client {ClientId}", clientId);
                throw new InvalidClientException("Invalid client credentials");
            }
        }

        return client;
    }

    /// <summary>
    /// Validates that a redirect URI is allowed for a client.
    /// Redirect URI must match exactly (case-sensitive per OAuth2 spec).
    /// </summary>
    public async Task ValidateRedirectUriAsync(
        string? clientId,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            throw new InvalidClientException("redirect_uri is required");

        if (!redirectUri.IsValidAbsoluteUri())
            throw new InvalidClientException("redirect_uri is not a valid absolute URI");

        var client = await GetClientAsync(clientId, cancellationToken);
        if (client == null)
            throw new InvalidClientException("Client not found");

        if (!client.AllowedRedirectUris.Any(uri => uri.Equals(redirectUri, StringComparison.Ordinal)))
        {
            _logger.LogWarning(
                "Redirect URI {Uri} not allowed for client {ClientId}",
                redirectUri,
                clientId);
            throw new InvalidClientException("redirect_uri is not registered");
        }
    }

    /// <summary>
    /// Validates that requested scopes are allowed for a client.
    /// Some scopes may be restricted to certain clients (e.g., admin scopes).
    /// </summary>
    public async Task ValidateScopesAsync(
        string? clientId,
        IEnumerable<string> requestedScopes,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(clientId, cancellationToken);
        if (client == null)
            throw new InvalidClientException("Client not found");

        var allowedScopes = new HashSet<string>(client.AllowedScopes);
        var requested = new HashSet<string>(requestedScopes);

        // Check that all requested scopes are allowed for this client
        var invalidScopes = requested.Except(allowedScopes).ToList();
        if (invalidScopes.Any())
        {
            _logger.LogWarning(
                "Client {ClientId} requested invalid scopes: {Scopes}",
                clientId,
                string.Join(" ", invalidScopes));
            throw new InvalidScopeException($"Invalid scopes requested: {string.Join(" ", invalidScopes)}");
        }
    }

    /// <summary>
    /// Checks if a client is allowed to use a specific grant type.
    /// Some clients may be restricted to certain flows for security.
    /// </summary>
    public async Task ValidateGrantTypeAsync(
        string? clientId,
        string grantType,
        CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(clientId, cancellationToken);
        if (client == null)
            throw new InvalidClientException("Client not found");

        if (!client.AllowedGrantTypes.Contains(grantType))
        {
            _logger.LogWarning(
                "Client {ClientId} not allowed to use grant type {GrantType}",
                clientId,
                grantType);
            throw new UnauthorizedClientException(
                $"Client is not authorized to use grant type '{grantType}'");
        }
    }

    /// <summary>
    /// Retrieves client information with caching to reduce database load.
    /// </summary>
    private async Task<Client?> GetClientAsync(string? clientId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            return null;

        var cacheKey = $"client:{clientId}";
        var cachedClient = await _cacheService.GetAsync<Client>(cacheKey, cancellationToken);
        if (cachedClient != null)
            return cachedClient;

        var client = await _clientRepository.GetByIdAsync(clientId, cancellationToken);
        if (client != null)
        {
            // Cache for 1 hour
            await _cacheService.SetAsync(cacheKey, client, TimeSpan.FromHours(1), cancellationToken);
        }

        return client;
    }

    /// <summary>
    /// Compares two strings in constant time to prevent timing attacks.
    /// </summary>
    private static bool SecureStringCompare(string? provided, string? stored)
    {
        if (string.IsNullOrEmpty(provided) && string.IsNullOrEmpty(stored))
            return true;

        if (string.IsNullOrEmpty(provided) || string.IsNullOrEmpty(stored))
            return false;

        // Compare lengths first
        if (provided.Length != stored.Length)
            return false;

        // Constant-time comparison
        int result = 0;
        for (int i = 0; i < provided.Length; i++)
        {
            result |= provided[i] ^ stored[i];
        }

        return result == 0;
    }
}
