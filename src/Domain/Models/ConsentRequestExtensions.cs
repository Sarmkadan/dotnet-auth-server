using System;
using System.Linq;

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Provides extension methods for <see cref="ConsentRequest"/> to simplify common operations.
/// </summary>
public static class ConsentRequestExtensions
{
    /// <summary>
    /// Determines whether this consent request has been approved by the user.
    /// </summary>
    /// <param name="consentRequest">The consent request to check.</param>
    /// <returns><see langword="true"/> if the consent request has been approved; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    public static bool IsApproved(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return consentRequest.Approved;
    }

    /// <summary>
    /// Determines whether this consent request has been denied by the user.
    /// </summary>
    /// <param name="consentRequest">The consent request to check.</param>
    /// <returns><see langword="true"/> if the consent request has been denied; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    public static bool IsDenied(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return !consentRequest.Approved && consentRequest.DenialReason != null;
    }

    /// <summary>
    /// Gets the scopes that were requested for this consent.
    /// </summary>
    /// <param name="consentRequest">The consent request containing the scopes.</param>
    /// <returns>An array of requested scope strings. Returns an empty array if no scopes are present.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    public static string[] GetRequestedScopes(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return string.IsNullOrWhiteSpace(consentRequest.GetScopesString())
            ? Array.Empty<string>()
            : consentRequest.GetScopesString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Determines whether this consent request is still pending approval.
    /// </summary>
    /// <param name="consentRequest">The consent request to check.</param>
    /// <returns><see langword="true"/> if the consent request is pending; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    public static bool IsPending(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return !consentRequest.Approved && consentRequest.DenialReason == null;
    }

    /// <summary>
    /// Gets the user ID associated with this consent request.
    /// </summary>
    /// <param name="consentRequest">The consent request containing the user ID.</param>
    /// <returns>The user ID associated with this consent request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The user ID is not set on the consent request.</exception>
    public static string GetUserIdOrThrow(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return !string.IsNullOrWhiteSpace(consentRequest.UserId)
            ? consentRequest.UserId
            : throw new InvalidOperationException("User ID is required for this consent request.");
    }

    /// <summary>
    /// Gets the client ID associated with this consent request.
    /// </summary>
    /// <param name="consentRequest">The consent request containing the client ID.</param>
    /// <returns>The client ID associated with this consent request.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="consentRequest"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The client ID is not set on the consent request.</exception>
    public static string GetClientIdOrThrow(this ConsentRequest consentRequest)
    {
        ArgumentNullException.ThrowIfNull(consentRequest);

        return !string.IsNullOrWhiteSpace(consentRequest.ClientId)
            ? consentRequest.ClientId
            : throw new InvalidOperationException("Client ID is required for this consent request.");
    }
}
