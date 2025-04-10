#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Cryptography;
using System.Text;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Implements RFC 7591 Dynamic Client Registration.
/// Validates the registration request, creates a <see cref="Client"/> record,
/// and returns the registered metadata plus credentials for confidential clients.
/// </summary>
public sealed class DynamicClientRegistrationService sealed
{
    private static readonly IReadOnlySet<string> SupportedGrantTypes = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        Constants.GrantTypes.AuthorizationCode,
        Constants.GrantTypes.ClientCredentials,
        Constants.GrantTypes.Implicit,
        Constants.GrantTypes.RefreshToken,
    };

    private static readonly IReadOnlySet<string> SupportedAuthMethods = new HashSet<string>(
        StringComparer.OrdinalIgnoreCase)
    {
        "none",
        "client_secret_basic",
        "client_secret_post",
    };

    private readonly IClientRepository _clientRepository;
    private readonly AuthServerOptions _options;
    private readonly ILogger<DynamicClientRegistrationService> _logger;

    public DynamicClientRegistrationService(
        IClientRepository clientRepository,
        AuthServerOptions options,
        ILogger<DynamicClientRegistrationService> logger)
    {
        _clientRepository = clientRepository;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new client and returns its credentials.
    /// </summary>
    public async Task<ClientRegistrationResponse> RegisterAsync(
        ClientRegistrationRequest request,
        CancellationToken cancellationToken = default)
    {
        Validate(request);

        var isPublic = string.Equals(
            request.TokenEndpointAuthMethod, "none", StringComparison.OrdinalIgnoreCase);

        var clientId = GenerateClientId();
        string? plainSecret = null;
        string? secretHash = null;

        if (!isPublic)
        {
            plainSecret = GenerateClientSecret();
            secretHash = HashSecret(plainSecret);
        }

        var requestedScopes = (request.Scope ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        // Intersect with server-supported scopes; unknown scopes are silently dropped
        // per RFC 7591 §3.2.2 (server MAY restrict).
        var allowedScopes = requestedScopes.Count > 0
            ? requestedScopes.Intersect(_options.SupportedScopes, StringComparer.OrdinalIgnoreCase).ToList()
            : _options.SupportedScopes.ToList();

        var client = new Client
        {
            ClientId = clientId,
            ClientName = request.ClientName!,
            ClientSecretHash = secretHash,
            IsConfidential = !isPublic,
            IsActive = true,
            RedirectUris = request.RedirectUris.ToList(),
            AllowedGrantTypes = request.GrantTypes.ToList(),
            AllowedScopes = allowedScopes,
            LogoUri = request.LogoUri,
            PolicyUri = request.PolicyUri,
            TermsOfServiceUri = request.TosUri,
            Contacts = request.Contacts.ToList(),
            RequirePkce = !isPublic
                ? _options.RequirePkceForAllClients
                : true, // public clients always need PKCE
            RequireConsent = _options.RequireUserConsent,
        };

        await _clientRepository.CreateAsync(client, cancellationToken);

        _logger.LogInformation(
            "Dynamically registered client {ClientId} (confidential={IsConfidential})",
            clientId, !isPublic);

        var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        return new ClientRegistrationResponse
        {
            ClientId = clientId,
            ClientSecret = plainSecret,
            ClientSecretExpiresAt = plainSecret is null ? null : 0, // 0 = no expiry
            ClientIdIssuedAt = now,
            ClientName = client.ClientName,
            GrantTypes = client.AllowedGrantTypes,
            RedirectUris = client.RedirectUris,
            ResponseTypes = request.ResponseTypes,
            Scope = allowedScopes.Count > 0 ? string.Join(' ', allowedScopes) : null,
            TokenEndpointAuthMethod = request.TokenEndpointAuthMethod,
            LogoUri = client.LogoUri,
            PolicyUri = client.PolicyUri,
            TosUri = client.TermsOfServiceUri,
            Contacts = client.Contacts,
        };
    }

    // -------------------------------------------------------------------------

    private void Validate(ClientRegistrationRequest request)
    {
        if (!request.IsValid())
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "client_name is required and redirect_uris must be provided for " +
                "authorization_code / implicit grant types.",
                400);

        foreach (var grantType in request.GrantTypes)
        {
            if (!SupportedGrantTypes.Contains(grantType))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    $"Grant type '{grantType}' is not supported by this server.",
                    400);
        }

        if (!SupportedAuthMethods.Contains(request.TokenEndpointAuthMethod))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                $"token_endpoint_auth_method '{request.TokenEndpointAuthMethod}' is not supported.",
                400);

        foreach (var uri in request.RedirectUris)
        {
            if (!Uri.TryCreate(uri, UriKind.Absolute, out _))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    $"redirect_uri '{uri}' is not a valid absolute URI.",
                    400);
        }
    }

    private static string GenerateClientId()
        => Guid.NewGuid().ToString("N");

    private static string GenerateClientSecret()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string HashSecret(string secret)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        return Convert.ToBase64String(hash);
    }
}
