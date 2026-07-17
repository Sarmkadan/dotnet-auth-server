using System;
using System.Collections.Generic;
using System.Globalization;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using Microsoft.Extensions.Logging;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Extension methods for <see cref="TokenIntrospectionBenchmarks"/> that provide additional functionality
/// for working with token introspection benchmarks and test data.
/// </summary>
/// <remarks>
/// All extension methods validate their parameters and throw <see cref="ArgumentNullException"/> for null inputs.
/// </remarks>
public static class TokenIntrospectionBenchmarksExtensions
{
    /// <summary>
    /// Gets the token introspection results for all token types (valid, invalid, expired).
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A read-only list of token introspection results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static IReadOnlyList<TokenIntrospectionResult> GetAllTokenResults(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new List<TokenIntrospectionResult>
        {
            benchmarks.IntrospectValidTokenResult(),
            benchmarks.IntrospectInvalidTokenResult(),
            benchmarks.IntrospectExpiredTokenResult()
        }.AsReadOnly();
    }

    /// <summary>
    /// Gets the token introspection result for a valid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static TokenIntrospectionResult IntrospectValidTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._validToken);
    }

    /// <summary>
    /// Gets the token introspection result for an invalid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static TokenIntrospectionResult IntrospectInvalidTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._invalidToken);
    }

    /// <summary>
    /// Gets the token introspection result for an expired token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static TokenIntrospectionResult IntrospectExpiredTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._expiredToken);
    }

    /// <summary>
    /// Gets the active status for a valid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>True if the token is active; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static bool IntrospectValidToken(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks.IntrospectValidTokenResult().Active;
    }

    /// <summary>
    /// Gets the active status for an invalid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>True if the token is active; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static bool IntrospectInvalidToken(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks.IntrospectInvalidTokenResult().Active;
    }

    /// <summary>
    /// Gets the active status for an expired token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>True if the token is active; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static bool IntrospectExpiredToken(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks.IntrospectExpiredTokenResult().Active;
    }

    /// <summary>
    /// Gets the active status for all token types as a dictionary.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A dictionary mapping token type names to their active status.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static IReadOnlyDictionary<string, bool> GetTokenActiveStatus(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["ValidToken"] = benchmarks.IntrospectValidToken(),
            ["InvalidToken"] = benchmarks.IntrospectInvalidToken(),
            ["ExpiredToken"] = benchmarks.IntrospectExpiredToken()
        };
    }

    /// <summary>
    /// Gets a summary string representing the token introspection results.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A formatted summary string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="benchmarks"/> is null.</exception>
    public static string GetResultsSummary(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var validResult = benchmarks.IntrospectValidTokenResult();
        var invalidResult = benchmarks.IntrospectInvalidTokenResult();
        var expiredResult = benchmarks.IntrospectExpiredTokenResult();

        return string.Create(CultureInfo.InvariantCulture, $"""Token Introspection Results:
Valid Token: {(validResult.Active ? "ACTIVE" : "INACTIVE")}
Invalid Token: {(invalidResult.Active ? "ACTIVE" : "INACTIVE")}
Expired Token: {(expiredResult.Active ? "ACTIVE" : "INACTIVE")}
""") + Environment.NewLine;
    }
}