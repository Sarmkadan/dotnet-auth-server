using System;
using DotnetAuthServer.Security;
using Xunit;

namespace DotnetAuthServer.Tests
{
    /// <summary>
    /// Tests for refresh‑token rotation and reuse detection using <see cref="RevokedTokenStore"/>.
    /// </summary>
    public sealed class RefreshTokenRotationTests
    {
        private static readonly DateTime FutureExpiry = DateTime.UtcNow.AddHours(1);
        private const string FamilyId = "family-123";

        [Fact]
        public void Rotation_and_reuse_detection_revokes_entire_family()
        {
            // Arrange
            var store = new RevokedTokenStore();
            var tokenA = "refresh-token-a";
            var tokenB = "refresh-token-b";

            // Register both tokens as part of the same family.
            store.RegisterToken(tokenA, FutureExpiry, FamilyId);
            store.RegisterToken(tokenB, FutureExpiry, FamilyId);

            // Act: first rotation – revoke the first token.
            store.Revoke(tokenA, FutureExpiry, FamilyId);

            // Assert: tokenA is revoked, tokenB is still active.
            Assert.True(store.IsRevoked(tokenA, out var familyA));
            Assert.False(store.IsRevoked(tokenB));
            Assert.Equal(FamilyId, familyA);

            // Act: reuse detection – a revoked token is presented again.
            // The store should revoke the whole family.
            store.RevokeFamily(FamilyId);

            // Assert: tokenB is now revoked as well.
            Assert.True(store.IsRevoked(tokenB, out var familyB));
            Assert.Equal(FamilyId, familyB);
        }

        [Fact]
        public void Unknown_token_is_not_revoked()
        {
            var store = new RevokedTokenStore();
            Assert.False(store.IsRevoked("non‑existent-token"));
        }
    }
}
