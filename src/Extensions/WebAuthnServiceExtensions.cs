#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetAuthServer.Extensions;

/// <summary>
/// <see cref="IServiceCollection"/> extension methods for registering WebAuthn/FIDO2 services.
/// </summary>
public static class WebAuthnServiceExtensions
{
    /// <summary>
    /// Registers the WebAuthn/FIDO2 authentication services with the dependency-injection container.
    /// </summary>
    /// <remarks>
    /// By default an in-process <see cref="InMemoryWebAuthnCredentialStore"/> is used.
    /// Replace it with a database-backed implementation before deploying to production:
    /// <code>
    /// services.AddWebAuthn()
    /// .AddSingleton&lt;IWebAuthnCredentialStore, MyDbCredentialStore&gt;();
    /// </code>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance to allow chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddWebAuthn(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IWebAuthnCredentialStore, InMemoryWebAuthnCredentialStore>();
        services.AddScoped<IWebAuthnService, WebAuthnService>();
        return services;
    }
}

/// <summary>
/// Thread-safe, in-process implementation of <see cref="IWebAuthnCredentialStore"/>.
/// Suitable for development, integration testing, and single-node deployments without persistence requirements.
/// Replace with a database-backed implementation for production use.
/// </summary>
public sealed class InMemoryWebAuthnCredentialStore : IWebAuthnCredentialStore
{
    private readonly ConcurrentDictionary<string, WebAuthnCredential> _byCredentialId = new(StringComparer.Ordinal);

    /// <summary>
    /// Returns the active credential whose <see cref="WebAuthnCredential.CredentialId"/> matches
    /// <paramref name="credentialId"/>, or <see langword="null"/> if no such credential exists.
    /// </summary>
    /// <param name="credentialId">The credential identifier to search for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The active credential if found; otherwise, <see langword="null"/>.</returns>
    public Task<WebAuthnCredential?> FindByCredentialIdAsync(
        string credentialId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credentialId);

        _byCredentialId.TryGetValue(credentialId, out var credential);
        return Task.FromResult<WebAuthnCredential?>(credential?.IsActive == true ? credential : null);
    }

    /// <summary>
    /// Returns all active credentials registered for <paramref name="userId"/>, ordered by registration date ascending.
    /// </summary>
    /// <param name="userId">The user identifier to search for.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A read-only list of active credentials for the specified user.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="userId"/> is <see langword="null"/>.</exception>
    public Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(userId);

        IReadOnlyList<WebAuthnCredential> results = _byCredentialId.Values
            .Where(c => c.UserId == userId && c.IsActive)
            .OrderBy(c => c.CreatedAt)
            .ToList();

        return Task.FromResult(results);
    }

    /// <summary>
    /// Persists a newly registered <paramref name="credential"/>.
    /// Throws <see cref="InvalidOperationException"/> if a credential with the same ID already exists.
    /// </summary>
    /// <param name="credential">The credential to add to the store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="credential"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A credential with the same ID already exists in the store.</exception>
    public Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        if (!_byCredentialId.TryAdd(credential.CredentialId, credential))
            throw new InvalidOperationException(
                $"A credential with ID '{credential.CredentialId}' is already registered.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists updates to an existing <paramref name="credential"/> (e.g., signature counter and last-used timestamp).
    /// Throws <see cref="KeyNotFoundException"/> if the credential does not exist in the store.
    /// </summary>
    /// <param name="credential">The credential to update in the store.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="credential"/> is <see langword="null"/>.</exception>
    /// <exception cref="KeyNotFoundException">The credential does not exist in the store.</exception>
    public Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(credential);

        if (!_byCredentialId.ContainsKey(credential.CredentialId))
            throw new KeyNotFoundException($"Credential '{credential.CredentialId}' not found in the store.");

        _byCredentialId[credential.CredentialId] = credential;
        return Task.CompletedTask;
    }
}