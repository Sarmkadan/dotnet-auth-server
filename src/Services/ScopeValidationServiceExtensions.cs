#nullable enable

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Extensions;

/// <summary>
/// Extension methods for <see cref="ScopeValidationService"/> that provide additional scope validation utilities.
/// </summary>
public static class ScopeValidationServiceExtensions
{
    /// <summary>
    /// Checks if the requested scopes contain any of the specified required scopes.
    /// Returns true if at least one of the required scopes is present.
    /// </summary>
    /// <param name="service">The scope validation service</param>
    /// <param name="requestedScopes">The requested scopes to check</param>
    /// <param name="requiredScopes">The scopes that are required to be present</param>
    /// <returns>True if any required scope is present, otherwise false</returns>
    public static bool ContainsAnyRequiredScope(
        this ScopeValidationService service,
        IEnumerable<string> requestedScopes,
        params string[] requiredScopes)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(requiredScopes);

        var requested = new HashSet<string>(requestedScopes, StringComparer.OrdinalIgnoreCase);
        return requiredScopes.Any(scope => requested.Contains(scope));
    }

    /// <summary>
    /// Validates that all scopes in a collection are valid.
    /// Returns a tuple containing the valid scopes and invalid scopes.
    /// </summary>
    /// <param name="service">The scope validation service</param>
    /// <param name="scopes">The collection of scopes to validate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    /// A tuple where Item1 is the list of valid scopes and Item2 is the list of invalid scopes.
    /// </returns>
    public static async Task<(IReadOnlyList<string> Valid, IReadOnlyList<string> Invalid)>
        ValidateScopesWithResultsAsync(
        this ScopeValidationService service,
        IEnumerable<string> scopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(scopes);

        var scopeList = scopes.ToList();
        var validScopes = new List<string>();
        var invalidScopes = new List<string>();

        foreach (var scope in scopeList)
        {
            if (await service.ValidateScopesAsync(scope, cancellationToken) is var validated && validated.Any())
            {
                validScopes.Add(scope);
            }
            else
            {
                invalidScopes.Add(scope);
            }
        }

        return (validScopes.AsReadOnly(), invalidScopes.AsReadOnly());
    }

    /// <summary>
    /// Determines if a scope string contains only standard OIDC scopes.
    /// Standard scopes are: openid, profile, email, address, phone, offline_access
    /// </summary>
    /// <param name="service">The scope validation service</param>
    /// <param name="scopeString">The scope string to check</param>
    /// <returns>True if all scopes are standard OIDC scopes, otherwise false</returns>
    public static bool IsStandardScopesOnly(
        this ScopeValidationService service,
        string? scopeString)
    {
        ArgumentNullException.ThrowIfNull(service);

        if (string.IsNullOrWhiteSpace(scopeString))
            return true;

        var scopes = scopeString.ParseScopes();
        var standardScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "openid", "profile", "email", "address", "phone", "offline_access"
        };

        return scopes.All(scope => standardScopes.Contains(scope));
    }

    /// <summary>
    /// Gets the difference between two scope sets: scopes present in the first set but not in the second.
    /// Useful for determining what scopes were added or removed.
    /// </summary>
    /// <param name="service">The scope validation service</param>
    /// <param name="currentScopes">The current set of scopes</param>
    /// <param name="previousScopes">The previous set of scopes to compare against</param>
    /// <returns>Scopes that are in current but not in previous</returns>
    public static IEnumerable<string> GetAddedScopes(
        this ScopeValidationService service,
        IEnumerable<string> currentScopes,
        IEnumerable<string> previousScopes)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(currentScopes);
        ArgumentNullException.ThrowIfNull(previousScopes);

        var current = new HashSet<string>(currentScopes, StringComparer.OrdinalIgnoreCase);
        var previous = new HashSet<string>(previousScopes, StringComparer.OrdinalIgnoreCase);

        current.ExceptWith(previous);
        return current;
    }
}