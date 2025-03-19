// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Service for handling OAuth2/OIDC authorization requests and code generation
/// </summary>
public class AuthorizationService
{
    private readonly AuthServerOptions _options;
    private readonly IClientRepository _clientRepository;
    private readonly IAuthorizationGrantRepository _grantRepository;

    public AuthorizationService(
        AuthServerOptions options,
        IClientRepository clientRepository,
        IAuthorizationGrantRepository grantRepository)
    {
        _options = options;
        _clientRepository = clientRepository;
        _grantRepository = grantRepository;
    }

    /// <summary>
    /// Validates and processes an authorization request
    /// </summary>
    public async Task<AuthorizationRequest> ValidateAuthorizationRequestAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "Authorization request is missing required parameters",
                400);

        // Validate client
        var client = await _clientRepository.GetActiveClientAsync(request.ClientId, cancellationToken);
        if (client == null)
            throw new InvalidClientException("Client not found or inactive");

        // Validate response type
        if (!IsValidResponseType(request.ResponseType))
            throw new AuthServerException(
                Constants.ErrorCodes.UnsupportedResponseType,
                $"Response type '{request.ResponseType}' is not supported",
                400);

        // Validate redirect URI
        if (!client.IsRedirectUriValid(request.RedirectUri))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "The redirect_uri is not registered",
                400);

        // Validate scopes
        var requestedScopes = request.GetRequestedScopes();
        foreach (var scope in requestedScopes)
        {
            if (!_options.SupportedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase))
                throw new InvalidScopeException($"Scope '{scope}' is not supported");
        }

        // Validate PKCE
        if (request.HasPkce() && !IsValidCodeChallenge(request.CodeChallenge!))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "Invalid code_challenge format",
                400);

        if (_options.RequirePkceForAllClients && !request.HasPkce() && client.RequirePkce)
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "PKCE is required for this client",
                400);

        return request;
    }

    /// <summary>
    /// Creates and stores an authorization grant (authorization code)
    /// </summary>
    public async Task<AuthorizationGrant> CreateAuthorizationGrantAsync(
        string clientId,
        string userId,
        string grantedScopes,
        string redirectUri,
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        var code = GenerateAuthorizationCode();
        var grantId = Guid.NewGuid().ToString();

        var grant = new AuthorizationGrant
        {
            GrantId = grantId,
            Code = code,
            ClientId = clientId,
            UserId = userId,
            RequestedScopes = request.Scope ?? "",
            GrantedScopes = grantedScopes,
            RedirectUri = redirectUri,
            State = request.State,
            Nonce = request.Nonce,
            CodeChallenge = request.CodeChallenge,
            CodeChallengeMethod = request.CodeChallengeMethod,
            ResponseType = request.ResponseType ?? "",
            ExpiresAt = DateTime.UtcNow.AddSeconds(_options.AuthorizationCodeLifetimeSeconds)
        };

        await _grantRepository.CreateAsync(grant, cancellationToken);
        return grant;
    }

    /// <summary>
    /// Gets a consent prompt response for the authorization request
    /// </summary>
    public async Task<ConsentResponse> GetConsentPromptAsync(
        AuthorizationRequest request,
        User user,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetActiveClientAsync(request.ClientId, cancellationToken);
        if (client == null)
            throw new InvalidClientException("Client not found");

        var requestedScopes = request.GetRequestedScopes().ToList();

        return new ConsentResponse
        {
            ClientId = client.ClientId,
            ClientName = client.ClientName,
            ClientLogoUri = client.LogoUri,
            ClientDescription = client.Description,
            UserId = user.UserId,
            UserName = user.Username,
            RequestedScopes = requestedScopes,
            RequireConsent = client.RequireConsent && requestedScopes.Any()
        };
    }

    /// <summary>
    /// Validates PKCE code verifier
    /// </summary>
    public bool ValidatePkceCodeVerifier(string codeChallenge, string codeVerifier, string? method)
    {
        method = method ?? "plain";

        if (method == "plain")
            return codeVerifier == codeChallenge;

        if (method == "S256")
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(codeVerifier));
                var computedChallenge = Convert.ToBase64String(hash)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
                return computedChallenge == codeChallenge;
            }
        }

        return false;
    }

    /// <summary>
    /// Cleans up expired authorization grants
    /// </summary>
    public async Task CleanupExpiredGrantsAsync(CancellationToken cancellationToken = default)
    {
        await _grantRepository.DeleteExpiredAsync(cancellationToken);
    }

    /// <summary>
    /// Checks if a response type is valid
    /// </summary>
    private bool IsValidResponseType(string? responseType)
    {
        if (string.IsNullOrWhiteSpace(responseType))
            return false;

        var validTypes = new[] { "code", "token", "id_token", "code id_token", "code token", "id_token token", "code id_token token" };
        return validTypes.Contains(responseType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validates PKCE code challenge format
    /// </summary>
    private bool IsValidCodeChallenge(string codeChallenge)
    {
        // Code challenge should be between 43-128 characters for S256
        // and exactly match code_verifier for plain
        return !string.IsNullOrWhiteSpace(codeChallenge) &&
               codeChallenge.Length >= 43 &&
               codeChallenge.Length <= 128 &&
               codeChallenge.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '.' || c == '_' || c == '~');
    }

    /// <summary>
    /// Generates a cryptographically secure authorization code
    /// </summary>
    private static string GenerateAuthorizationCode()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}

/// <summary>
/// Response model for consent prompt display
/// </summary>
public class ConsentResponse
{
    public string ClientId { get; set; } = null!;
    public string ClientName { get; set; } = null!;
    public string? ClientLogoUri { get; set; }
    public string? ClientDescription { get; set; }
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public IEnumerable<string> RequestedScopes { get; set; } = [];
    public bool RequireConsent { get; set; }
}
