using System;
using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Client"/>.
    /// </summary>
    public class ClientTests
    {
        [Fact]
        public void IsValid_ReturnsTrue_WhenAllRequiredFieldsAreSet()
        {
            var client = new Client
            {
                ClientId = "client1",
                ClientName = "Test Client",
                IsConfidential = false,
                RedirectUris = new List<string> { "https://app/callback" },
                AllowedGrantTypes = new List<string> { "authorization_code" }
            };

            client.IsValid().Should().BeTrue();
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenClientIdOrClientNameIsMissing()
        {
            var missingId = new Client
            {
                ClientId = "",
                ClientName = "Test Client",
                IsConfidential = false,
                RedirectUris = new List<string> { "https://app/callback" },
                AllowedGrantTypes = new List<string> { "authorization_code" }
            };

            var missingName = new Client
            {
                ClientId = "client1",
                ClientName = "",
                IsConfidential = false,
                RedirectUris = new List<string> { "https://app/callback" },
                AllowedGrantTypes = new List<string> { "authorization_code" }
            };

            missingId.IsValid().Should().BeFalse();
            missingName.IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsValid_ReturnsFalse_WhenConfidentialClientHasNoSecret()
        {
            var client = new Client
            {
                ClientId = "client1",
                ClientName = "Confidential Client",
                IsConfidential = true,
                // ClientSecretHash left null
                RedirectUris = new List<string> { "https://app/callback" },
                AllowedGrantTypes = new List<string> { "authorization_code" }
            };

            client.IsValid().Should().BeFalse();
        }

        [Fact]
        public void IsRedirectUriValid_ReturnsTrue_ForRegisteredUri()
        {
            var client = new Client
            {
                RedirectUris = new List<string> { "https://app/callback", "https://app/alt" }
            };

            client.IsRedirectUriValid("https://app/callback").Should().BeTrue();
        }

        [Fact]
        public void IsRedirectUriValid_ReturnsFalse_ForUnregisteredOrNullUri()
        {
            var client = new Client
            {
                RedirectUris = new List<string> { "https://app/callback" }
            };

            client.IsRedirectUriValid("https://app/unknown").Should().BeFalse();
            client.IsRedirectUriValid(null).Should().BeFalse();
        }

        [Fact]
        public void IsPostLogoutRedirectUriValid_ReturnsTrue_WhenNoPostLogoutUrisConfigured()
        {
            var client = new Client
            {
                PostLogoutRedirectUris = new List<string>()
            };

            client.IsPostLogoutRedirectUriValid("https://app/logout").Should().BeTrue();
        }

        [Fact]
        public void IsPostLogoutRedirectUriValid_ReturnsTrue_WhenUriIsRegistered()
        {
            var client = new Client
            {
                PostLogoutRedirectUris = new List<string> { "https://app/logout" }
            };

            client.IsPostLogoutRedirectUriValid("https://app/logout").Should().BeTrue();
        }

        [Fact]
        public void IsPostLogoutRedirectUriValid_ReturnsFalse_WhenUriNotRegistered()
        {
            var client = new Client
            {
                PostLogoutRedirectUris = new List<string> { "https://app/logout" }
            };

            client.IsPostLogoutRedirectUriValid("https://app/other").Should().BeFalse();
        }

        [Fact]
        public void IsGrantTypeAllowed_ReturnsTrue_WhenGrantTypeIsAllowed()
        {
            var client = new Client
            {
                AllowedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
            };

            client.IsGrantTypeAllowed("refresh_token").Should().BeTrue();
        }

        [Fact]
        public void IsGrantTypeAllowed_ThrowsArgumentNullException_WhenGrantTypeIsNull()
        {
            var client = new Client
            {
                AllowedGrantTypes = new List<string> { "authorization_code" }
            };

            Action act = () => client.IsGrantTypeAllowed(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsScopeAllowed_ReturnsTrue_WhenScopeIsAllowed()
        {
            var client = new Client
            {
                AllowedScopes = new List<string> { "openid", "profile" }
            };

            client.IsScopeAllowed("profile").Should().BeTrue();
        }

        [Fact]
        public void IsScopeAllowed_ThrowsArgumentNullException_WhenScopeIsNull()
        {
            var client = new Client
            {
                AllowedScopes = new List<string> { "openid" }
            };

            Action act = () => client.IsScopeAllowed(null!);
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
