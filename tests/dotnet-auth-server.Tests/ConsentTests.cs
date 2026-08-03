using System;
using System.Collections.Generic;
using System.Linq;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests
{
    /// <summary>
    /// Unit tests for <see cref="Consent"/>.
    /// </summary>
    public class ConsentTests
    {
        private Consent CreateDefaultConsent()
        {
            return new Consent
            {
                ConsentId = Guid.NewGuid().ToString(),
                UserId = "user-1",
                ClientId = "client-1",
                GrantedScopes = string.Empty,
                Status = ConsentStatus.Pending,
                ExpiresAt = null,
                IsOfflineConsent = false,
                DenialReason = null,
                IpAddress = null,
                UserAgent = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        [Fact]
        public void Grant_ShouldSetApprovedStatusAndUpdateProperties()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            var beforeUpdate = consent.UpdatedAt;
            var scopes = "openid profile email";

            // Act
            consent.Grant(scopes, ipAddress: "127.0.0.1", userAgent: "unit-test");

            // Assert
            consent.Status.Should().Be(ConsentStatus.Approved);
            consent.GrantedScopes.Should().Be(scopes);
            consent.IpAddress.Should().Be("127.0.0.1");
            consent.UserAgent.Should().Be("unit-test");
            consent.UpdatedAt.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public void Deny_ShouldSetRejectedStatusAndStoreReason()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            var beforeUpdate = consent.UpdatedAt;
            var reason = "User declined";

            // Act
            consent.Deny(reason);

            // Assert
            consent.Status.Should().Be(ConsentStatus.Rejected);
            consent.DenialReason.Should().Be(reason);
            consent.UpdatedAt.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public void Revoke_ShouldSetExpiredStatusAndDefaultReasonWhenNoneProvided()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            var beforeUpdate = consent.UpdatedAt;

            // Act
            consent.Revoke(null);

            // Assert
            consent.Status.Should().Be(ConsentStatus.Expired);
            consent.DenialReason.Should().Be("Manually revoked");
            consent.UpdatedAt.Should().BeAfter(beforeUpdate);
        }

        [Fact]
        public void IsValidAndApproved_ReturnsTrueWhenApprovedAndNotExpired()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            consent.Grant("read write", ipAddress: null, userAgent: null);
            consent.ExpiresAt = DateTime.UtcNow.AddHours(1); // future

            // Act
            var result = consent.IsValidAndApproved();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsValidAndApproved_ReturnsFalseWhenExpired()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            consent.Grant("read", ipAddress: null, userAgent: null);
            consent.ExpiresAt = DateTime.UtcNow.AddMinutes(-5); // past

            // Act
            var result = consent.IsValidAndApproved();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void HasScopeConsent_ShouldDetectGrantedScope()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            consent.Grant("read write delete", ipAddress: null, userAgent: null);

            // Act & Assert
            consent.HasScopeConsent("read").Should().BeTrue();
            consent.HasScopeConsent("WRITE").Should().BeTrue(); // case‑insensitive
            consent.HasScopeConsent("unknown").Should().BeFalse();
        }

        [Fact]
        public void HasScopeConsent_ReturnsFalseWhenNoScopesGranted()
        {
            // Arrange
            var consent = CreateDefaultConsent(); // GrantedScopes is empty

            // Act
            var result = consent.HasScopeConsent("any");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetGrantedScopes_ShouldReturnAllScopesAsEnumerable()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            var scopes = "openid profile email";
            consent.Grant(scopes, ipAddress: null, userAgent: null);

            // Act
            IEnumerable<string> result = consent.GetGrantedScopes();

            // Assert
            result.Should().BeEquivalentTo(new[] { "openid", "profile", "email" });
        }

        [Fact]
        public void IsExpired_ReturnsTrueWhenExpirationTimePassed()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            consent.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

            // Act
            var result = consent.IsExpired();

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void IsExpired_ReturnsFalseWhenNoExpirationSet()
        {
            // Arrange
            var consent = CreateDefaultConsent();
            consent.ExpiresAt = null;

            // Act
            var result = consent.IsExpired();

            // Assert
            result.Should().BeFalse();
        }
    }
}
