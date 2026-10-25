using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetAuthServer.Security;

/// <summary>
/// Extension methods for converting between space-delimited scope strings and scope collections.
/// </summary>
public static class ScopeStringExtensions
{
    /// <summary>
    /// Splits a space-delimited scope string into a set of scopes.
    /// </summary>
    /// <param name="scopeString">Space-delimited scope string.</param>
    /// <returns>A set containing each scope.</returns>
    public static HashSet<string> ToScopeSet(this string scopeString)
    {
        return new HashSet<string>(scopeString.Split(' '));
    }

    /// <summary>
    /// Determines whether a space-delimited scope string contains a given scope.
    /// </summary>
    /// <param name="scopeString">Space-delimited scope string.</param>
    /// <param name="scope">Scope to look for.</param>
    /// <returns>true if the scope is present; otherwise false.</returns>
    public static bool ContainsScope(this string scopeString, string scope)
    {
        var scopeSet = ToScopeSet(scopeString);
        return scopeSet.Contains(scope);
    }

    /// <summary>
    /// Determines whether a space-delimited scope string contains every one of the given scopes.
    /// </summary>
    /// <param name="scopeString">Space-delimited scope string.</param>
    /// <param name="scopes">Scopes that must all be present.</param>
    /// <returns>true if every scope is present; otherwise false.</returns>
    public static bool HasAllScopes(this string scopeString, params string[] scopes)
    {
        var scopeSet = ToScopeSet(scopeString);
        return scopeSet.IsSupersetOf(scopes);
    }

    /// <summary>
    /// Determines whether a space-delimited scope string contains at least one of the given scopes.
    /// </summary>
    /// <param name="scopeString">Space-delimited scope string.</param>
    /// <param name="scopes">Scopes of which at least one must be present.</param>
    /// <returns>true if any scope is present; otherwise false.</returns>
    public static bool HasAnyScope(this string scopeString, params string[] scopes)
    {
        var scopeSet = ToScopeSet(scopeString);
        return scopeSet.Overlaps(scopes);
    }

    /// <summary>
    /// Joins a collection of scopes into a space-delimited scope string.
    /// </summary>
    /// <param name="scopes">Scopes to join.</param>
    /// <returns>A space-delimited scope string.</returns>
    public static string ToScopeString(this IEnumerable<string> scopes)
    {
        return string.Join(" ", scopes);
    }
}