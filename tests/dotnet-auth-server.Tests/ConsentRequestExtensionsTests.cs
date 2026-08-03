using System;
using System.Collections.Generic;
using System.Linq;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests
{
    public class ConsentRequestExtensionsTests
    {
        private ConsentRequest CreateValidConsentRequest(bool approved = false, string? denialReason = null, IEnumerable<string>? scopes = null, string? userId = "user-1", string? clientId = "client-1")
        {
            return new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = scopes?.ToList() ?? new List<string>(),
                Approved = approved,
                DenialReason = denialReason
            };
        }

        [Fact]
        public void IsApproved_ReturnsCorrectValue_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(approved: true);

            // Act & Assert
            consentRequest.IsApproved().Should().BeTrue();

            consentRequest = CreateValidConsentRequest(approved: false);
            consentRequest.IsApproved().Should().BeFalse();

            // Null
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.IsApproved())
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsDenied_ReturnsCorrectValue_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(approved: false, denialReason: "User declined");

            // Act & Assert
            consentRequest.IsDenied().Should().BeTrue();

            // Approved true -> not denied
            consentRequest = CreateValidConsentRequest(approved: true, denialReason: "Any reason");
            consentRequest.IsDenied().Should().BeFalse();

            // Pending (not approved, no denial reason) -> not denied
            consentRequest = CreateValidConsentRequest(approved: false, denialReason: null);
            consentRequest.IsDenied().Should().BeFalse();

            // Null
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.IsDenied())
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetRequestedScopes_ReturnsCorrectArray_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(scopes: new[] { "openid", "profile", "email" });

            // Act & Assert
            consentRequest.GetRequestedScopes().Should().BeEquivalentTo(new[] { "openid", "profile", "email" });

            // Empty scopes
            consentRequest = CreateValidConsentRequest(scopes: Array.Empty<string>());
            consentRequest.GetRequestedScopes().Should().BeEmpty();

            // Whitespace-only scopes string (GetScopesString returns empty string)
            consentRequest = CreateValidConsentRequest(scopes: new[] { "" });
            consentRequest.GetRequestedScopes().Should().BeEmpty();

            // Null
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.GetRequestedScopes())
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsPending_ReturnsCorrectValue_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(approved: false, denialReason: null);

            // Act & Assert
            consentRequest.IsPending().Should().BeTrue();

            // Approved true -> not pending
            consentRequest = CreateValidConsentRequest(approved: true, denialReason: null);
            consentRequest.IsPending().Should().BeFalse();

            // Denied -> not denied
            consentRequest = CreateValidConsentRequest(approved: false, denialReason: "Reason");
            consentRequest.IsPending().Should().BeFalse();

            // Null
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.IsPending())
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetUserIdOrThrow_ReturnsValue_ThrowsOnMissingUserId_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(userId: "user-123");

            // Act & Assert
            consentRequest.GetUserIdOrThrow().Should().Be("user-123");

            // UserId null
            consentRequest = CreateValidConsentRequest(userId: null);
            FluentActions.Invoking(() => consentRequest.GetUserIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("User ID is required for this consent request.");

            // UserId empty
            consentRequest = CreateValidConsentRequest(userId: string.Empty);
            FluentActions.Invoking(() => consentRequest.GetUserIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("User ID is required for this consent request.");

            // UserId whitespace
            consentRequest = CreateValidConsentRequest(userId: "   ");
            FluentActions.Invoking(() => consentRequest.GetUserIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("User ID is required for this consent request.");

            // Null consent request
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.GetUserIdOrThrow())
                .Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void GetClientIdOrThrow_ReturnsValue_ThrowsOnMissingClientId_And_ThrowsOnNull()
        {
            // Arrange
            var consentRequest = CreateValidConsentRequest(clientId: "client-456");

            // Act & Assert
            consentRequest.GetClientIdOrThrow().Should().Be("client-456");

            // ClientId null
            consentRequest = CreateValidConsentRequest(clientId: null);
            FluentActions.Invoking(() => consentRequest.GetClientIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Client ID is required for this consent request.");

            // ClientId empty
            consentRequest = CreateValidConsentRequest(clientId: string.Empty);
            FluentActions.Invoking(() => consentRequest.GetClientIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Client ID is required for this consent request.");

            // ClientId whitespace
            consentRequest = CreateValidConsentRequest(clientId: "   ");
            FluentActions.Invoking(() => consentRequest.GetClientIdOrThrow())
                .Should().Throw<InvalidOperationException>()
                .WithMessage("Client ID is required for this consent request.");

            // Null consent request
            ConsentRequest? nullRequest = null;
            FluentActions.Invoking(() => nullRequest.GetClientIdOrThrow())
                .Should().Throw<ArgumentNullException>();
        }
    }
}