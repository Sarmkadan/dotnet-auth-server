#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;

using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using System.Globalization;

/// <summary>
/// Extension methods for <see cref="ClientService"/> that provide additional client management functionality
/// </summary>
public static class ClientServiceExtensions
{
    /// <summary>
    /// Registers a new public OAuth2 client with default settings
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="clientName">The client display name</param>
    /// <param name="redirectUris">Registered redirect URIs for authorization flow</param>
    /// <param name="allowedScopes">Allowed scopes for this client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The registered client</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="AuthServerException">Thrown when registration fails</exception>
    public static async Task<Client> RegisterPublicClientAsync(
        this ClientService clientService,
        string clientName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(redirectUris);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        return await clientService.RegisterClientAsync(
            clientName,
            isConfidential: false,
            redirectUris,
            [Constants.GrantTypes.AuthorizationCode],
            allowedScopes,
            cancellationToken);
    }

    /// <summary>
    /// Registers a new confidential OAuth2 client with PKCE support
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="clientName">The client display name</param>
    /// <param name="redirectUris">Registered redirect URIs for authorization flow</param>
    /// <param name="allowedScopes">Allowed scopes for this client</param>
    /// <param name="requirePkce">Whether PKCE is required for this client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The registered client</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="AuthServerException">Thrown when registration fails</exception>
    public static async Task<Client> RegisterConfidentialClientAsync(
        this ClientService clientService,
        string clientName,
        IEnumerable<string> redirectUris,
        IEnumerable<string> allowedScopes,
        bool requirePkce = true,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(redirectUris);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        var grantTypes = new List<string> { Constants.GrantTypes.AuthorizationCode };
        if (requirePkce)
        {
            grantTypes.Add(Constants.GrantTypes.Hybrid);
        }

        return await clientService.RegisterClientAsync(
            clientName,
            isConfidential: true,
            redirectUris,
            grantTypes,
            allowedScopes,
            cancellationToken);
    }

    /// <summary>
    /// Registers a client for client credentials grant type (machine-to-machine)
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="clientName">The client display name</param>
    /// <param name="allowedScopes">Allowed scopes for this client</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The registered client</returns>
    /// <exception cref="ArgumentNullException">Thrown when required parameters are null</exception>
    /// <exception cref="AuthServerException">Thrown when registration fails</exception>
    public static async Task<Client> RegisterClientCredentialsClientAsync(
        this ClientService clientService,
        string clientName,
        IEnumerable<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        return await clientService.RegisterClientAsync(
            clientName,
            isConfidential: true,
            redirectUris: [],
            [Constants.GrantTypes.ClientCredentials],
            allowedScopes,
            cancellationToken);
    }

    /// <summary>
    /// Updates client metadata without changing core configuration
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="client">The client to update</param>
    /// <param name="clientName">New client display name</param>
    /// <param name="description">New client description</param>
    /// <param name="contacts">New contact emails</param>
    /// <param name="logoUri">New logo URI</param>
    /// <param name="policyUri">New privacy policy URI</param>
    /// <param name="termsOfServiceUri">New terms of service URI</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated client</returns>
    /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
    /// <exception cref="AuthServerException">Thrown when update fails</exception>
    public static async Task<Client> UpdateClientMetadataAsync(
        this ClientService clientService,
        Client client,
        string? clientName = null,
        string? description = null,
        IEnumerable<string>? contacts = null,
        string? logoUri = null,
        string? policyUri = null,
        string? termsOfServiceUri = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(client);

        return await clientService.UpdateClientAsync(
            client,
            clientName,
            redirectUris: null,
            allowedScopes: null,
            corsOrigins: null,
            cancellationToken);
    }

    /// <summary>
    /// Checks if a client has a specific grant type enabled
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="client">The client to check</param>
    /// <param name="grantType">The grant type to check for</param>
    /// <returns>True if the grant type is allowed, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
    public static bool HasGrantType(
        this ClientService clientService,
        Client client,
        string grantType)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(grantType);

        return client.IsGrantTypeAllowed(grantType);
    }

    /// <summary>
    /// Checks if a client has a specific scope enabled
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="client">The client to check</param>
    /// <param name="scope">The scope to check for</param>
    /// <returns>True if the scope is allowed, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
    public static bool HasScope(
        this ClientService clientService,
        Client client,
        string scope)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrEmpty(scope);

        return client.IsScopeAllowed(scope);
    }

    /// <summary>
    /// Checks if a redirect URI is valid for the given client
    /// </summary>
    /// <param name="clientService">The client service instance</param>
    /// <param name="client">The client to check</param>
    /// <param name="redirectUri">The redirect URI to validate</param>
    /// <returns>True if the redirect URI is valid, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when client is null</exception>
    public static bool IsValidRedirectUri(
        this ClientService clientService,
        Client client,
        string? redirectUri)
    {
        ArgumentNullException.ThrowIfNull(clientService);
        ArgumentNullException.ThrowIfNull(client);

        return client.IsRedirectUriValid(redirectUri);
    }


}