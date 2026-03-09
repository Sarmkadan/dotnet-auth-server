#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Security;

/// <summary>
/// Service for issuing and managing OAuth2/OIDC tokens
/// </summary>
public sealed class TokenService
{
    private readonly AuthServerOptions _options;
    private readonly IUserRepository _userRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IAuthorizationGrantRepository _grantRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly LoginRateLimiter _loginRateLimiter;

    public TokenService(
        AuthServerOptions options,
        IUserRepository userRepository,
        IClientRepository clientRepository,
        IAuthorizationGrantRepository grantRepository,
        IRefreshTokenRepository refreshTokenRepository,
        LoginRateLimiter loginRateLimiter)
    {
        _options = options;
        _userRepository = userRepository;
        _clientRepository = clientRepository;
        _grantRepository = grantRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _loginRateLimiter = loginRateLimiter;
    }

    /// <summary>
    /// Handles token request and returns a token response
    /// </summary>
    public async Task<TokenResponse> HandleTokenRequestAsync(
        TokenRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!request.IsValid())
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "The request is missing required parameters",
                400);

        if (string.IsNullOrWhiteSpace(request.GrantType))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "grant_type is required",
                400);

        return request.GrantType switch
        {
            Constants.GrantTypes.AuthorizationCode =>
                await HandleAuthorizationCodeGrantAsync(request, cancellationToken),
            Constants.GrantTypes.RefreshToken =>
                await HandleRefreshTokenGrantAsync(request, cancellationToken),
            Constants.GrantTypes.ClientCredentials =>
                await HandleClientCredentialsGrantAsync(request, cancellationToken),
            Constants.GrantTypes.Password =>
                await HandlePasswordGrantAsync(request, cancellationToken),
            _ => throw new AuthServerException(
                Constants.ErrorCodes.UnsupportedGrantType,
                $"Grant type '{request.GrantType}' is not supported",
                400)
        };
    }

    /// <summary>
    /// Handles authorization code grant (standard OAuth2 code flow)
    /// </summary>
    private async Task<TokenResponse> HandleAuthorizationCodeGrantAsync(
        TokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.IsValidForGrantType(Constants.GrantTypes.AuthorizationCode))
            throw new InvalidGrantException("Authorization code or redirect_uri is missing");

        var grant = await _grantRepository.GetByCodeAsync(request.Code, cancellationToken);
        if (grant is null || !grant.IsValid())
            throw new InvalidGrantException("Authorization code is invalid or expired");

        if (!grant.RedirectUri.Equals(request.RedirectUri, StringComparison.OrdinalIgnoreCase))
            throw new InvalidGrantException("Redirect URI does not match");

        // Validate PKCE if required
        if (!string.IsNullOrWhiteSpace(grant.CodeChallenge))
        {
            if (!grant.ValidatePkceCodeVerifier(request.CodeVerifier))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "PKCE code verifier is invalid",
                    400);
        }

        grant.MarkAsUsed();
        await _grantRepository.UpdateAsync(grant, cancellationToken);

        var user = await _userRepository.GetByIdAsync(grant.UserId, cancellationToken);
        if (user is null)
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "User not found",
                500);

        var client = await _clientRepository.GetActiveClientAsync(request.ClientId, cancellationToken);

        var scopes = grant.GrantedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateAccessToken(user, request.ClientId, scopes,
            GetAccessTokenLifetime(client));
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(
            user.UserId, request.ClientId, grant.GrantedScopes,
            GetRefreshTokenLifetime(client), cancellationToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = Constants.TokenTypes.Bearer,
            ExpiresIn = GetAccessTokenLifetime(client),
            RefreshToken = refreshToken,
            Scope = grant.GrantedScopes
        };
    }

    /// <summary>
    /// Handles refresh token grant with proper token rotation.
    /// The old token is revoked *before* the new token is created so that
    /// a concurrent replay of the same token is rejected rather than silently
    /// succeeding.  Clock-skew tolerance is applied when evaluating expiry so
    /// that clients whose clocks lag the server by up to
    /// <see cref="AuthServerOptions.ClockSkewToleranceSeconds"/> are still served.
    /// </summary>
    private async Task<TokenResponse> HandleRefreshTokenGrantAsync(
        TokenRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.IsValidForGrantType(Constants.GrantTypes.RefreshToken))
            throw new InvalidGrantException("Refresh token is missing");

        var tokenHash = HashToken(request.RefreshToken!);
        var token = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (token is null || token.IsRevoked)
            throw new InvalidGrantException("Refresh token is invalid or expired");

        // Apply clock-skew tolerance: accept tokens that expired within the
        // tolerance window so clients with slightly-behind clocks are not
        // rejected.  Tokens that expired well beyond the window are refused.
        var tolerance = TimeSpan.FromSeconds(_options.ClockSkewToleranceSeconds);
        if (DateTime.UtcNow > token.ExpiresAt.Add(tolerance))
            throw new InvalidGrantException("Refresh token is invalid or expired");

        // Revoke the old token *before* issuing a new one.  This is the
        // rotation lock: any concurrent request presenting the same token will
        // find it already revoked and be rejected as a replay.
        token.Revoke("Refresh token rotation");
        await _refreshTokenRepository.UpdateAsync(token, cancellationToken);

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "User not found",
                500);

        var client = await _clientRepository.GetActiveClientAsync(token.ClientId, cancellationToken);

        var scopes = token.GrantedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateAccessToken(user, token.ClientId, scopes,
            GetAccessTokenLifetime(client));
        var newRefreshToken = request.RefreshToken;

        if (_options.AutoRefreshTokenRotation)
        {
            newRefreshToken = GenerateTokenValue();
            var newTokenHash = HashToken(newRefreshToken);
            var newToken = new RefreshToken
            {
                TokenId = Guid.NewGuid().ToString(),
                TokenHash = newTokenHash,
                ClientId = token.ClientId,
                UserId = token.UserId,
                GrantedScopes = token.GrantedScopes,
                Version = token.Version + 1,
                PreviousTokenHash = token.TokenHash,
                ExpiresAt = DateTime.UtcNow.AddSeconds(GetRefreshTokenLifetime(client))
            };
            await _refreshTokenRepository.CreateAsync(newToken, cancellationToken);
        }

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = Constants.TokenTypes.Bearer,
            ExpiresIn = GetAccessTokenLifetime(client),
            RefreshToken = newRefreshToken,
            Scope = token.GrantedScopes
        };
    }

    /// <summary>
    /// Handles client credentials grant (machine-to-machine)
    /// </summary>
    private async Task<TokenResponse> HandleClientCredentialsGrantAsync(
        TokenRequest request,
        CancellationToken cancellationToken)
    {
        var client = await _clientRepository.GetActiveClientAsync(request.ClientId, cancellationToken);
        if (client is null)
            throw new InvalidClientException("Client not found or inactive");

        if (client.IsConfidential && string.IsNullOrWhiteSpace(request.ClientSecret))
            throw new InvalidClientException("Client secret is required");

        var scopes = (request.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateClientCredentialsAccessToken(client, scopes);

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = Constants.TokenTypes.Bearer,
            ExpiresIn = GetAccessTokenLifetime(client),
            Scope = request.Scope
        };
    }

    /// <summary>
    /// Handles resource owner password credentials grant (legacy)
    /// </summary>
    private async Task<TokenResponse> HandlePasswordGrantAsync(
        TokenRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "Username and password are required",
                400);

        // Enforce per-username and per-IP rate limiting before touching the database
        _loginRateLimiter.ThrowIfBlocked(request.Username, request.IpAddress);

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken);
        if (user is null || user.IsLocked())
        {
            _loginRateLimiter.RecordFailure(request.Username, request.IpAddress);
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidGrant,
                "Invalid credentials",
                400);
        }

        _loginRateLimiter.RecordSuccess(request.Username);

        var client = await _clientRepository.GetActiveClientAsync(request.ClientId, cancellationToken);

        var scopes = (request.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var accessToken = GenerateAccessToken(user, request.ClientId, scopes,
            GetAccessTokenLifetime(client));
        var refreshToken = await GenerateAndStoreRefreshTokenAsync(
            user.UserId, request.ClientId, request.Scope ?? "",
            GetRefreshTokenLifetime(client), cancellationToken);

        return new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = Constants.TokenTypes.Bearer,
            ExpiresIn = GetAccessTokenLifetime(client),
            RefreshToken = refreshToken,
            Scope = request.Scope
        };
    }

    /// <summary>
    /// Generates a JWT access token for a user
    /// </summary>
    private string GenerateAccessToken(User user, string clientId, IEnumerable<string> scopes,
        int lifetimeSeconds)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddSeconds(lifetimeSeconds);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(Constants.Claims.Sub, user.UserId),
            new(Constants.Claims.Iss, _options.IssuerUrl),
            new(Constants.Claims.Aud, clientId),
            new(Constants.Claims.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
            new(Constants.Claims.Exp, new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString()),
            new(Constants.Claims.Scope, string.Join(" ", scopes))
        };

        // Add user claims
        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(Constants.Claims.Email, user.Email));
        if (user.EmailVerified)
            claims.Add(new Claim(Constants.Claims.EmailVerified, "true"));

        foreach (var role in user.Roles)
            claims.Add(new Claim(Constants.Claims.Roles, role));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            audience: clientId,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates a JWT access token for client credentials flow
    /// </summary>
    private string GenerateClientCredentialsAccessToken(Client client, IEnumerable<string> scopes)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddSeconds(GetAccessTokenLifetime(client));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(Constants.Claims.Sub, client.ClientId),
            new(Constants.Claims.Iss, _options.IssuerUrl),
            new(Constants.Claims.Aud, client.ClientId),
            new(Constants.Claims.Iat, new DateTimeOffset(now).ToUnixTimeSeconds().ToString()),
            new(Constants.Claims.Exp, new DateTimeOffset(expiresAt).ToUnixTimeSeconds().ToString()),
            new(Constants.Claims.Scope, string.Join(" ", scopes))
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            audience: client.ClientId,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Generates and stores a refresh token
    /// </summary>
    private async Task<string> GenerateAndStoreRefreshTokenAsync(
        string userId,
        string clientId,
        string scopes,
        int lifetimeSeconds,
        CancellationToken cancellationToken)
    {
        var tokenValue = GenerateTokenValue();
        var tokenHash = HashToken(tokenValue);

        var refreshToken = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = tokenHash,
            ClientId = clientId,
            UserId = userId,
            GrantedScopes = scopes,
            ExpiresAt = DateTime.UtcNow.AddSeconds(lifetimeSeconds)
        };

        await _refreshTokenRepository.CreateAsync(refreshToken, cancellationToken);
        return tokenValue;
    }

    /// <summary>
    /// Returns the effective access token lifetime: client-specific setting if configured,
    /// otherwise the global default.
    /// </summary>
    private int GetAccessTokenLifetime(Client? client)
        => client?.AccessTokenLifetime > 0 ? client.AccessTokenLifetime : _options.AccessTokenLifetimeSeconds;

    /// <summary>
    /// Returns the effective refresh token lifetime: client-specific setting if configured,
    /// otherwise the global default.
    /// </summary>
    private int GetRefreshTokenLifetime(Client? client)
        => client?.RefreshTokenLifetime > 0 ? client.RefreshTokenLifetime : _options.RefreshTokenLifetimeSeconds;

    /// <summary>
    /// Generates a cryptographically secure token value
    /// </summary>
    private static string GenerateTokenValue()
    {
        var bytes = new byte[32];
        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Hashes a token value using SHA256
    /// </summary>
    private static string HashToken(string token)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}
