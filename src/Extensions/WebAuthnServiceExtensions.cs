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
    ///         .AddSingleton&lt;IWebAuthnCredentialStore, MyDbCredentialStore&gt;();
    /// </code>
    /// </remarks>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The same <paramref name="services"/> instance to allow chaining.</returns>
    public static IServiceCollection AddWebAuthn(this IServiceCollection services)
    {
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
    public Task<WebAuthnCredential?> FindByCredentialIdAsync(
        string credentialId, CancellationToken cancellationToken = default)
    {
        _byCredentialId.TryGetValue(credentialId, out var credential);
        return Task.FromResult<WebAuthnCredential?>(credential?.IsActive == true ? credential : null);
    }

    /// <summary>
    /// Returns all active credentials registered for <paramref name="userId"/>, ordered by registration date ascending.
    /// </summary>
    public Task<IReadOnlyList<WebAuthnCredential>> GetByUserIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
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
    public Task AddAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        if (!_byCredentialId.TryAdd(credential.CredentialId, credential))
            throw new InvalidOperationException(
                $"A credential with ID '{credential.CredentialId}' is already registered.");

        return Task.CompletedTask;
    }

    /// <summary>
    /// Persists updates to an existing <paramref name="credential"/> (e.g., signature counter and last-used timestamp).
    /// Throws <see cref="KeyNotFoundException"/> if the credential does not exist in the store.
    /// </summary>
    public Task UpdateAsync(WebAuthnCredential credential, CancellationToken cancellationToken = default)
    {
        if (!_byCredentialId.ContainsKey(credential.CredentialId))
            throw new KeyNotFoundException($"Credential '{credential.CredentialId}' not found in the store.");

        _byCredentialId[credential.CredentialId] = credential;
        return Task.CompletedTask;
    }
}
