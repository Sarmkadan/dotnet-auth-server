using System;
using System.Linq;

namespace DotnetAuthServer.Domain.Models;

public static class ConsentRequestExtensions
{
    /// <summary>
    /// Determines whether this consent request has been approved by the user.
    /// </summary>
    public static bool IsApproved(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        return consentRequest.Approved;
    }

    /// <summary>
    /// Determines whether this consent request has been denied by the user.
    /// </summary>
    public static bool IsDenied(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        return !consentRequest.Approved && consentRequest.DenialReason != null;
    }

    /// <summary>
    /// Gets the scopes that were requested for this consent.
    /// </summary>
    public static string[] GetRequestedScopes(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        if (string.IsNullOrWhiteSpace(consentRequest.GetScopesString()))
        {
            return Array.Empty<string>();
        }

        return consentRequest.GetScopesString()
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Determines whether this consent request is still pending approval.
    /// </summary>
    public static bool IsPending(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        return !consentRequest.Approved && consentRequest.DenialReason == null;
    }

    /// <summary>
    /// Gets the user ID associated with this consent request.
    /// </summary>
    public static string GetUserIdOrThrow(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        if (string.IsNullOrWhiteSpace(consentRequest.UserId))
        {
            throw new InvalidOperationException("User ID is required for this consent request.");
        }

        return consentRequest.UserId;
    }

    /// <summary>
    /// Gets the client ID associated with this consent request.
    /// </summary>
    public static string GetClientIdOrThrow(this ConsentRequest consentRequest)
    {
        if (consentRequest == null)
        {
            throw new ArgumentNullException(nameof(consentRequest));
        }

        if (string.IsNullOrWhiteSpace(consentRequest.ClientId))
        {
            throw new InvalidOperationException("Client ID is required for this consent request.");
        }

        return consentRequest.ClientId;
    }
}