#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for OAuth2 client registration and management
/// </summary>
public sealed class ClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly ILogger<ClientService> _logger;

    public ClientService(IClientRepository clientRepository, ILogger<ClientService> logger)
    {
        _clientRepository = clientRepository ?? throw new ArgumentNullException(nameof(clientRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a new OAuth2 client
    /// </summary>
    public async Task<Client> RegisterClientAsync(
        string clientName,
        bool isConfidential,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedGrantTypes,
        IEnumerable<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Client name cannot be null or whitespace", nameof(clientName));

        if (redirectUris is null)
            throw new ArgumentNullException(nameof(redirectUris));

        if (allowedGrantTypes is null)
            throw new ArgumentNullException(nameof(allowedGrantTypes));

        if (allowedScopes is null)
            throw new ArgumentNullException(nameof(allowedScopes));

        try
        {
            if (!redirectUris.Any())
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "At least one redirect URI is required",
                    400);

            if (!allowedGrantTypes.Any())
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "At least one grant type must be allowed",
                    400);

            // Validate redirect URIs
            foreach (var uri in redirectUris)
            {
                if (!IsValidRedirectUri(uri))
                    throw new AuthServerException(
                        Constants.ErrorCodes.InvalidRequest,
                        $"Invalid redirect URI: {uri}",
                        400);
            }

            // Validate scopes
            foreach (var scope in allowedScopes)
            {
                if (string.IsNullOrWhiteSpace(scope))
                    throw new AuthServerException(
                        Constants.ErrorCodes.InvalidRequest,
                        "Scope cannot be empty",
                        400);
            }

            var client = new Client
            {
                ClientId = GenerateClientId(),
                ClientName = clientName,
                IsConfidential = isConfidential,
                ClientSecretHash = isConfidential ? GenerateAndHashClientSecret() : null,
                RedirectUris = new List<string>(redirectUris),
                AllowedGrantTypes = new List<string>(allowedGrantTypes),
                AllowedScopes = new List<string>(allowedScopes),
                IsActive = true
            };

            if (!client.IsValid())
                throw new AuthServerException(
                    Constants.ErrorCodes.ServerError,
                    "Client registration failed validation",
                    500);

            var createdClient = await _clientRepository.CreateAsync(client, cancellationToken);
            _logger.LogInformation("Client registered successfully: {ClientId}, Name: {ClientName}", createdClient.ClientId, clientName);
            return createdClient;
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error registering client with name: {ClientName}", clientName);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Client registration failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Updates client configuration
    /// </summary>
    public async Task<Client> UpdateClientAsync(
        Client client,
        string? clientName = null,
        IEnumerable<string>? redirectUris = null,
        IEnumerable<string>? allowedScopes = null,
        IEnumerable<string>? corsOrigins = null,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            if (!string.IsNullOrWhiteSpace(clientName))
                client.ClientName = clientName;

            if (redirectUris is not null)
            {
                foreach (var uri in redirectUris)
                {
                    if (!IsValidRedirectUri(uri))
                        throw new AuthServerException(
                            Constants.ErrorCodes.InvalidRequest,
                            $"Invalid redirect URI: {uri}",
                            400);
                }
                client.RedirectUris = new List<string>(redirectUris);
            }

            if (allowedScopes is not null)
            {
                client.AllowedScopes = new List<string>(allowedScopes);
            }

            if (corsOrigins is not null)
            {
                client.AllowedCorsOrigins = new List<string>(corsOrigins);
            }

            client.UpdatedAt = DateTime.UtcNow;
            var updatedClient = await _clientRepository.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Client updated successfully: {ClientId}", client.ClientId);
            return updatedClient;
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error updating client: {ClientId}", client?.ClientId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Client update failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Rotates the client secret
    /// </summary>
    public async Task<string> RotateClientSecretAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            if (!client.IsConfidential)
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "Only confidential clients have secrets",
                    400);

            var newSecret = GenerateClientSecret();
            var newSecretHash = HashClientSecret(newSecret);

            client.ClientSecretHash = newSecretHash;
            client.UpdatedAt = DateTime.UtcNow;

            await _clientRepository.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Client secret rotated successfully: {ClientId}", client.ClientId);
            return newSecret;
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error rotating client secret for client: {ClientId}", client?.ClientId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Client secret rotation failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Deactivates a client
    /// </summary>
    public async Task DeactivateClientAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            client.IsActive = false;
            client.UpdatedAt = DateTime.UtcNow;
            await _clientRepository.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Client deactivated: {ClientId}", client.ClientId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error deactivating client: {ClientId}", client?.ClientId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Client deactivation failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Reactivates a client
    /// </summary>
    public async Task ReactivateClientAsync(
        Client client,
        CancellationToken cancellationToken = default)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            client.IsActive = true;
            client.UpdatedAt = DateTime.UtcNow;
            await _clientRepository.UpdateAsync(client, cancellationToken);
            _logger.LogInformation("Client reactivated: {ClientId}", client.ClientId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error reactivating client: {ClientId}", client?.ClientId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Client reactivation failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Validates a client's secret
    /// </summary>
    public bool ValidateClientSecret(Client client, string? providedSecret)
    {
        if (client is null)
            throw new ArgumentNullException(nameof(client));

        try
        {
            if (!client.IsConfidential)
                return true;

            if (string.IsNullOrWhiteSpace(providedSecret))
                return false;

            var providedHash = HashClientSecret(providedSecret);
            return providedHash.Equals(client.ClientSecretHash, StringComparison.Ordinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating client secret for client: {ClientId}", client.ClientId);
            return false;
        }
    }

    /// <summary>
    /// Checks if a URI is a valid redirect URI
    /// </summary>
    private static bool IsValidRedirectUri(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return false;

        // Allow localhost for development
        if (uri.StartsWith("http://localhost", StringComparison.OrdinalIgnoreCase) ||
            uri.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow https for production
        if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return true;

        // Allow custom schemes (e.g., myapp://)
        if (uri.Contains("://") && !uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    /// <summary>
    /// Generates a unique client ID
    /// </summary>
    private static string GenerateClientId()
    {
        return $"client_{Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16)}";
    }

    /// <summary>
    /// Generates a cryptographically secure client secret
    /// </summary>
    private static string GenerateClientSecret()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Generates and hashes a client secret
    /// </summary>
    private static string GenerateAndHashClientSecret()
    {
        return HashClientSecret(GenerateClientSecret());
    }

    /// <summary>
    /// Hashes a client secret using SHA256
    /// </summary>
    private static string HashClientSecret(string secret)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(secret));
            return Convert.ToBase64String(hash);
        }
    }
}
