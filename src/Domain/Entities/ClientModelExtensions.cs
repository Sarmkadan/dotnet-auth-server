using System;
using System.Linq;

namespace DotnetAuthServer.Domain.Entities
{
    /// <summary>
    /// Extension methods for <see cref="Client"/> that provide convenient
    /// checks used throughout the service layer.
    /// </summary>
    public static class ClientModelExtensions
    {
        /// <summary>
        /// Determines whether the client is confidential.
        /// </summary>
        public static bool IsConfidential(this Client client) =>
            client?.IsConfidential ?? false;

        /// <summary>
        /// Checks if the client allows a specific grant type.
        /// </summary>
        public static bool AllowsGrantType(this Client client, string grantType) =>
            client?.AllowedGrantTypes?.Contains(grantType) ?? false;

        /// <summary>
        /// Determines whether the client has a particular redirect URI.
        /// </summary>
        public static bool HasRedirectUri(this Client client, Uri uri) =>
            client != null &&
            uri != null &&
            client.RedirectUris?.Any(r => string.Equals(r, uri.ToString(), StringComparison.OrdinalIgnoreCase)) == true;
    }
}
