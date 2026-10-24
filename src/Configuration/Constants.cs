#nullable enable
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
        /// <summary>
        /// The authorization_code grant type.
        /// </summary>
        public const string AuthorizationCode = "authorization_code";
        /// <summary>
        /// The refresh_token grant type.
        /// </summary>
        public const string RefreshToken = "refresh_token";
        /// <summary>
        /// The client_credentials grant type.
        /// </summary>
        public const string ClientCredentials = "client_credentials";
        /// <summary>
        /// The password grant type.
        /// </summary>
        public const string Password = "password";
        /// <summary>
        /// The implicit grant type.
        /// </summary>
        public const string Implicit = "implicit";
        /// <summary>
        /// The hybrid grant type.
        /// </summary>
        public const string Hybrid = "hybrid";
        /// <summary>
        /// The OAuth 2.0 token exchange grant type (RFC 8693).
        /// </summary>
        public const string TokenExchange = "urn:ietf:params:oauth:grant-type:token-exchange";
    }

    /// <summary>
    /// Response types
    /// </summary>
    public static class ResponseTypes
    {
        /// <summary>
        /// The code response type.
        /// </summary>
        public const string Code = "code";
        /// <summary>
        /// The token response type.
        /// </summary>
        public const string Token = "token";
        /// <summary>
        /// The id_token response type.
        /// </summary>
        public const string IdToken = "id_token";
    }

    /// <summary>
    /// Scopes
    /// </summary>
    public static class Scopes
    {
        /// <summary>
        /// The openid scope.
        /// </summary>
        public const string OpenId = "openid";
        /// <summary>
        /// The profile scope.
        /// </summary>
        public const string Profile = "profile";
        /// <summary>
        /// The email scope.
        /// </summary>
        public const string Email = "email";
        /// <summary>
        /// The address scope.
        /// </summary>
        public const string Address = "address";
        /// <summary>
        /// The phone scope.
        /// </summary>
        public const string Phone = "phone";
        /// <summary>
        /// The offline_access scope, which requests a refresh token.
        /// </summary>
        public const string OfflineAccess = "offline_access";
    }

    /// <summary>
    /// Token types
    /// </summary>
    public static class TokenTypes
    {
        /// <summary>
        /// The Bearer token type.
        /// </summary>
        public const string Bearer = "Bearer";
        /// <summary>
        /// The DPoP token type (RFC 9449).
        /// </summary>
        public const string DPoP = "DPoP";
    }

    /// <summary>
    /// PKCE code challenge methods
    /// </summary>
    public static class PkceChallengeMethods
    {
        /// <summary>
        /// The plain PKCE code challenge method.
        /// </summary>
        public const string Plain = "plain";
        /// <summary>
        /// The S256 PKCE code challenge method (SHA-256).
        /// </summary>
        public const string S256 = "S256";
    }

    /// <summary>
    /// Claim names
    /// </summary>
    public static class Claims
    {
        /// <summary>
        /// The sub (subject) claim name.
        /// </summary>
        public const string Sub = "sub";
        /// <summary>
        /// The iss (issuer) claim name.
        /// </summary>
        public const string Iss = "iss";
        /// <summary>
        /// The aud (audience) claim name.
        /// </summary>
        public const string Aud = "aud";
        /// <summary>
        /// The exp (expiration time) claim name.
        /// </summary>
        public const string Exp = "exp";
        /// <summary>
        /// The iat (issued at) claim name.
        /// </summary>
        public const string Iat = "iat";
        /// <summary>
        /// The nbf (not before) claim name.
        /// </summary>
        public const string Nbf = "nbf";
        /// <summary>
        /// The nonce claim name.
        /// </summary>
        public const string Nonce = "nonce";
        /// <summary>
        /// The name claim name.
        /// </summary>
        public const string Name = "name";
        /// <summary>
        /// The given_name claim name.
        /// </summary>
        public const string GivenName = "given_name";
        /// <summary>
        /// The family_name claim name.
        /// </summary>
        public const string FamilyName = "family_name";
        /// <summary>
        /// The email claim name.
        /// </summary>
        public const string Email = "email";
        /// <summary>
        /// The email_verified claim name.
        /// </summary>
        public const string EmailVerified = "email_verified";
        /// <summary>
        /// The picture claim name.
        /// </summary>
        public const string Picture = "picture";
        /// <summary>
        /// The roles claim name.
        /// </summary>
        public const string Roles = "roles";
        /// <summary>
        /// The scope claim name.
        /// </summary>
        public const string Scope = "scope";
        /// <summary>
        /// The auth_time claim name.
        /// </summary>
        public const string AuthTime = "auth_time";
        /// <summary>
        /// The acr (authentication context class reference) claim name.
        /// </summary>
        public const string Acr = "acr";
        /// <summary>
        /// The amr (authentication methods references) claim name.
        /// </summary>
        public const string Amr = "amr";
    }

    /// <summary>
    /// Headers
    /// </summary>
    public static class Headers
    {
        /// <summary>
        /// The Authorization HTTP header name.
        /// </summary>
        public const string Authorization = "Authorization";
        /// <summary>
        /// The Content-Type HTTP header name.
        /// </summary>
        public const string ContentType = "Content-Type";
        /// <summary>
        /// The Cache-Control HTTP header name.
        /// </summary>
        public const string CacheControl = "Cache-Control";
        /// <summary>
        /// The Pragma HTTP header name.
        /// </summary>
        public const string Pragma = "Pragma";
    }

    /// <summary>
    /// Content types
    /// </summary>
    public static class ContentTypes
    {
        /// <summary>
        /// The application/json media type.
        /// </summary>
        public const string ApplicationJson = "application/json";
        /// <summary>
        /// The application/x-www-form-urlencoded media type.
        /// </summary>
        public const string ApplicationFormUrlEncoded = "application/x-www-form-urlencoded";
    }

    /// <summary>
    /// Validation rules
    /// </summary>
    public static class Validation
    {
        /// <summary>
        /// Minimum length of a client secret.
        /// </summary>
        public const int MinClientSecretLength = 32;
        /// <summary>
        /// Minimum length of a password.
        /// </summary>
        public const int MinPasswordLength = 8;
        /// <summary>
        /// Maximum length of an authorization code.
        /// </summary>
        public const int MaxCodeLength = 256;
        /// <summary>
        /// Maximum length of a token.
        /// </summary>
        public const int MaxTokenLength = 10000;
        /// <summary>
        /// Authorization code expiration, in seconds (5 minutes).
        /// </summary>
        public const int CodeExpirationSeconds = 300; // 5 minutes
        /// <summary>
        /// Access token expiration, in seconds (1 hour).
        /// </summary>
        public const int AccessTokenExpirationSeconds = 3600; // 1 hour
        /// <summary>
        /// Refresh token expiration, in seconds (30 days).
        /// </summary>
        public const int RefreshTokenExpirationSeconds = 2592000; // 30 days
    }

    /// <summary>
    /// Error codes
    /// </summary>
    public static class ErrorCodes
    {
        /// <summary>
        /// The invalid_request OAuth error code.
        /// </summary>
        public const string InvalidRequest = "invalid_request";
        /// <summary>
        /// The invalid_client OAuth error code.
        /// </summary>
        public const string InvalidClient = "invalid_client";
        /// <summary>
        /// The invalid_grant OAuth error code.
        /// </summary>
        public const string InvalidGrant = "invalid_grant";
        /// <summary>
        /// The invalid_scope OAuth error code.
        /// </summary>
        public const string InvalidScope = "invalid_scope";
        /// <summary>
        /// The invalid_token OAuth error code.
        /// </summary>
    public const string InvalidToken = "invalid_token";
        /// <summary>
        /// The unauthorized_client OAuth error code.
        /// </summary>
        public const string UnauthorizedClient = "unauthorized_client";
        /// <summary>
        /// The unsupported_grant_type OAuth error code.
        /// </summary>
        public const string UnsupportedGrantType = "unsupported_grant_type";
        /// <summary>
        /// The unsupported_response_type OAuth error code.
        /// </summary>
        public const string UnsupportedResponseType = "unsupported_response_type";
        /// <summary>
        /// The access_denied OAuth error code.
        /// </summary>
        public const string AccessDenied = "access_denied";
        /// <summary>
        /// The server_error OAuth error code.
        /// </summary>
        public const string ServerError = "server_error";
        /// <summary>
        /// The temporarily_unavailable OAuth error code.
        /// </summary>
        public const string TemporarilyUnavailable = "temporarily_unavailable";
    }
}
