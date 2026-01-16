// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Global constants for the authorization server
/// </summary>
public static class Constants
{
    /// <summary>
    /// Grant types
    /// </summary>
    public static class GrantTypes
    {
        public const string AuthorizationCode = "authorization_code";
        public const string RefreshToken = "refresh_token";
        public const string ClientCredentials = "client_credentials";
        public const string Password = "password";
        public const string Implicit = "implicit";
        public const string Hybrid = "hybrid";
        public const string TokenExchange = "urn:ietf:params:oauth:grant-type:token-exchange";
    }

    /// <summary>
    /// Response types
    /// </summary>
    public static class ResponseTypes
    {
        public const string Code = "code";
        public const string Token = "token";
        public const string IdToken = "id_token";
    }

    /// <summary>
    /// Scopes
    /// </summary>
    public static class Scopes
    {
        public const string OpenId = "openid";
        public const string Profile = "profile";
        public const string Email = "email";
        public const string Address = "address";
        public const string Phone = "phone";
        public const string OfflineAccess = "offline_access";
    }

    /// <summary>
    /// Token types
    /// </summary>
    public static class TokenTypes
    {
        public const string Bearer = "Bearer";
        public const string DPoP = "DPoP";
    }

    /// <summary>
    /// PKCE code challenge methods
    /// </summary>
    public static class PkceChallengeMethods
    {
        public const string Plain = "plain";
        public const string S256 = "S256";
    }

    /// <summary>
    /// Claim names
    /// </summary>
    public static class Claims
    {
        public const string Sub = "sub";
        public const string Iss = "iss";
        public const string Aud = "aud";
        public const string Exp = "exp";
        public const string Iat = "iat";
        public const string Nbf = "nbf";
        public const string Nonce = "nonce";
        public const string Name = "name";
        public const string GivenName = "given_name";
        public const string FamilyName = "family_name";
        public const string Email = "email";
        public const string EmailVerified = "email_verified";
        public const string Picture = "picture";
        public const string Roles = "roles";
        public const string Scope = "scope";
        public const string AuthTime = "auth_time";
        public const string Acr = "acr";
        public const string Amr = "amr";
    }

    /// <summary>
    /// Headers
    /// </summary>
    public static class Headers
    {
        public const string Authorization = "Authorization";
        public const string ContentType = "Content-Type";
        public const string CacheControl = "Cache-Control";
        public const string Pragma = "Pragma";
    }

    /// <summary>
    /// Content types
    /// </summary>
    public static class ContentTypes
    {
        public const string ApplicationJson = "application/json";
        public const string ApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
    }

    /// <summary>
    /// Validation rules
    /// </summary>
    public static class Validation
    {
        public const int MinClientSecretLength = 32;
        public const int MinPasswordLength = 8;
        public const int MaxCodeLength = 256;
        public const int MaxTokenLength = 10000;
        public const int CodeExpirationSeconds = 300; // 5 minutes
        public const int AccessTokenExpirationSeconds = 3600; // 1 hour
        public const int RefreshTokenExpirationSeconds = 2592000; // 30 days
    }

    /// <summary>
    /// Error codes
    /// </summary>
    public static class ErrorCodes
    {
        public const string InvalidRequest = "invalid_request";
        public const string InvalidClient = "invalid_client";
        public const string InvalidGrant = "invalid_grant";
        public const string InvalidScope = "invalid_scope";
        public const string UnauthorizedClient = "unauthorized_client";
        public const string UnsupportedGrantType = "unsupported_grant_type";
        public const string UnsupportedResponseType = "unsupported_response_type";
        public const string AccessDenied = "access_denied";
        public const string ServerError = "server_error";
        public const string TemporarilyUnavailable = "temporarily_unavailable";
    }
}
