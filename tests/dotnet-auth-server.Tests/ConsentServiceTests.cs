#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Enums;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Services;
using Xunit;

/// <summary>
/// Tests for ConsentService with expiration functionality
/// </summary>
public sealed class ConsentServiceTests
{
    private readonly ConsentService _consentService;
    private readonly FakeConsentRepository _fakeRepository;

    public ConsentServiceTests()
    {
        var authServerOptions = new AuthServerOptions
        {
            ConsentExpirationDays = 30,
            SessionConsentExpirationHours = 1
        };

        _fakeRepository = new FakeConsentRepository();
        _consentService = new ConsentService(_fakeRepository, authServerOptions);
    }

    [Fact]
    public async Task RecordConsentAsync_SessionBasedConsent_SetsCorrectExpiration()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user1",
            ClientId = "client1",
            GrantedScopes = ["openid", "profile"],
            Approved = true,
            RememberConsent = false,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        var consent = await _consentService.RecordConsentAsync(request);

        // Assert
        Assert.NotNull(consent);
        Assert.NotNull(consent.ExpiresAt);
        Assert.True(consent.ExpiresAt > DateTime.UtcNow);
        Assert.False(consent.IsOfflineConsent);

        // Should expire in approximately 1 hour
        var expectedExpiration = DateTime.UtcNow.AddHours(1);
        var actualExpiration = consent.ExpiresAt.Value;
        var difference = Math.Abs((expectedExpiration - actualExpiration).TotalMinutes);

        Assert.True(difference < 1, "Session consent should expire in approximately 1 hour");
    }

    [Fact]
    public async Task RecordConsentAsync_OfflineConsent_SetsCorrectExpiration()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user2",
            ClientId = "client2",
            GrantedScopes = ["openid", "profile", "offline_access"],
            Approved = true,
            RememberConsent = true,
            IpAddress = "127.0.0.1",
            UserAgent = "Test Agent"
        };

        // Act
        var consent = await _consentService.RecordConsentAsync(request);

        // Assert
        Assert.NotNull(consent);
        Assert.NotNull(consent.ExpiresAt);
        Assert.True(consent.ExpiresAt > DateTime.UtcNow);
        Assert.True(consent.IsOfflineConsent);

        // Should expire in approximately 30 days
        var expectedExpiration = DateTime.UtcNow.AddDays(30);
        var actualExpiration = consent.ExpiresAt.Value;
        var difference = Math.Abs((expectedExpiration - actualExpiration).TotalMinutes);

        Assert.True(difference < 1, "Offline consent should expire in approximately 30 days");
    }

    [Fact]
    public async Task RecordConsentAsync_DeniedConsent_NoExpiration()
    {
        // Arrange
        var request = new ConsentRequest
        {
            UserId = "user3",
            ClientId = "client3",
            GrantedScopes = [],
            Approved = false,
            RememberConsent = true,
            DenialReason = "User declined"
        };

        // Act
        var consent = await _consentService.RecordConsentAsync(request);

        // Assert
        Assert.NotNull(consent);
        Assert.Equal(ConsentStatus.Rejected, consent.Status);
        Assert.Null(consent.ExpiresAt);
        Assert.False(consent.IsOfflineConsent);
    }

    [Fact]
    public async Task RequiresReconsent_ExpiredConsent_ReturnsTrue()
    {
        // Arrange
        var expiredConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user1",
            ClientId = "client1",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-40), // Created 40 days ago
            UpdatedAt = DateTime.UtcNow.AddDays(-40),
            ExpiresAt = DateTime.UtcNow.AddDays(-5) // Expired 5 days ago
        };

        // Act
        var requiresReconsent = _consentService.RequiresReconsent(expiredConsent);

        // Assert
        Assert.True(requiresReconsent);
    }

    [Fact]
    public async Task RequiresReconsent_ValidConsent_ReturnsFalse()
    {
        // Arrange
        var validConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user2",
            ClientId = "client2",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20) // Expires in 20 days
        };

        // Act
        var requiresReconsent = _consentService.RequiresReconsent(validConsent);

        // Assert
        Assert.False(requiresReconsent);
    }

    [Fact]
    public async Task RequiresReconsent_NearExpirationConsent_ReturnsTrue()
    {
        // Arrange - consent that has 20% or less validity remaining
        var nearExpirationConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user3",
            ClientId = "client3",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-25), // Created 25 days ago
            UpdatedAt = DateTime.UtcNow.AddDays(-25),
            ExpiresAt = DateTime.UtcNow.AddDays(5) // Expires in 5 days (16.7% of 30 days)
        };

        // Act
        var requiresReconsent = _consentService.RequiresReconsent(nearExpirationConsent);

        // Assert
        Assert.True(requiresReconsent);
    }

    [Fact]
    public async Task RequiresReconsent_RejectedConsent_ReturnsTrue()
    {
        // Arrange
        var rejectedConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user4",
            ClientId = "client4",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Rejected,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var requiresReconsent = _consentService.RequiresReconsent(rejectedConsent);

        // Assert
        Assert.True(requiresReconsent);
    }

    [Fact]
    public async Task GetConsentRemainingValidity_ActiveConsent_ReturnsRemainingTime()
    {
        // Arrange
        var validConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user5",
            ClientId = "client5",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(10) // Expires in 10 days
        };

        // Act
        var remainingValidity = _consentService.GetConsentRemainingValidity(validConsent);

        // Assert
        Assert.NotNull(remainingValidity);
        Assert.True(remainingValidity > TimeSpan.Zero);
        Assert.InRange(remainingValidity.Value.TotalDays, 9, 11); // Should be around 10 days
    }

    [Fact]
    public async Task GetConsentRemainingValidity_ExpiredConsent_ReturnsZero()
    {
        // Arrange
        var expiredConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user6",
            ClientId = "client6",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-40),
            ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
        };

        // Act
        var remainingValidity = _consentService.GetConsentRemainingValidity(expiredConsent);

        // Assert
        Assert.NotNull(remainingValidity);
        Assert.Equal(TimeSpan.Zero, remainingValidity);
    }

    [Fact]
    public async Task GetConsentRemainingValidity_NoExpiration_ReturnsNull()
    {
        // Arrange
        var noExpirationConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user7",
            ClientId = "client7",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            // ExpiresAt is null
        };

        // Act
        var remainingValidity = _consentService.GetConsentRemainingValidity(noExpirationConsent);

        // Assert
        Assert.Null(remainingValidity);
    }

    [Fact]
    public async Task GetConsentValidityPercentage_ActiveConsent_ReturnsCorrectPercentage()
    {
        // Arrange - consent created 10 days ago, expires in 20 days (30 day total)
        var validConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user8",
            ClientId = "client8",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            ExpiresAt = DateTime.UtcNow.AddDays(20)
        };

        // Act
        var percentage = _consentService.GetConsentValidityPercentage(validConsent);

        // Assert
        Assert.NotNull(percentage);
        Assert.InRange(percentage.Value, 66, 67); // Should be ~66.7%
    }

    [Fact]
    public async Task GetConsentValidityPercentage_ExpiredConsent_ReturnsZero()
    {
        // Arrange
        var expiredConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user9",
            ClientId = "client9",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-40),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var percentage = _consentService.GetConsentValidityPercentage(expiredConsent);

        // Assert
        Assert.NotNull(percentage);
        Assert.Equal(0, percentage);
    }

    [Fact]
    public async Task GetConsentValidityPercentage_NoExpiration_ReturnsNull()
    {
        // Arrange
        var noExpirationConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user10",
            ClientId = "client10",
            GrantedScopes = "openid profile",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        var percentage = _consentService.GetConsentValidityPercentage(noExpirationConsent);

        // Assert
        Assert.Null(percentage);
    }

        [Fact]
        public async Task GrantConsent_ThenQuery_ReturnsConsent()
        {
            // Arrange
            var userId = "user_grant_query";
            var clientId = "client_grant_query";
            var scopes = new[] { "openid", "profile", "email" };
            var request = new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = scopes.ToList(),
                Approved = true,
                RememberConsent = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act - Grant consent
            var consent = await _consentService.RecordConsentAsync(request);

            // Assert - Query consent
            Assert.NotNull(consent);
            Assert.Equal(userId, consent.UserId);
            Assert.Equal(clientId, consent.ClientId);
            Assert.Equal(ConsentStatus.Approved, consent.Status);
            Assert.True(consent.IsValidAndApproved());
            Assert.NotNull(consent.ExpiresAt);
        }

        [Fact]
        public async Task RevokeConsent_ThenQuery_ReturnsNoConsent()
        {
            // Arrange
            var userId = "user_revoke_query";
            var clientId = "client_revoke_query";
            var scopes = new[] { "openid", "profile" };
            var request = new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = scopes.ToList(),
                Approved = true,
                RememberConsent = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };
            await _consentService.RecordConsentAsync(request);

            // Act - Revoke consent
            await _consentService.RevokeConsentAsync(userId, clientId);

            // Assert - Query should show no consent
            var hasConsent = await _consentService.HasConsentAsync(userId, clientId, scopes);
            Assert.False(hasConsent);

            var effectiveScopes = await _consentService.GetEffectiveScopesAsync(userId, clientId, scopes);
            Assert.Empty(effectiveScopes);
        }

        [Fact]
        public async Task GrantConsent_Revoke_ThenReGrant_ReturnsNewConsent()
        {
            // Arrange
            var userId = "user_regrant";
            var clientId = "client_regrant";
            var scopes = new[] { "openid", "profile", "email" };
            var request = new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = scopes.ToList(),
                Approved = true,
                RememberConsent = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act - Grant initial consent
            var initialConsent = await _consentService.RecordConsentAsync(request);
            Assert.True(initialConsent.IsValidAndApproved());

            // Act - Revoke consent
            await _consentService.RevokeConsentAsync(userId, clientId);

            // Assert - Should not have consent after revocation
            var hasConsentAfterRevoke = await _consentService.HasConsentAsync(userId, clientId, scopes);
            Assert.False(hasConsentAfterRevoke);

            // Act - Grant consent again
            var newRequest = new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = scopes.ToList(),
                Approved = true,
                RememberConsent = true,
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };
            var reGrantedConsent = await _consentService.RecordConsentAsync(newRequest);

            // Assert - Should have new consent after re-granting
            Assert.NotNull(reGrantedConsent);
            Assert.Equal(userId, reGrantedConsent.UserId);
            Assert.Equal(clientId, reGrantedConsent.ClientId);
            Assert.Equal(ConsentStatus.Approved, reGrantedConsent.Status);
            Assert.True(reGrantedConsent.IsValidAndApproved());
            Assert.True(reGrantedConsent.IsOfflineConsent);
            Assert.NotNull(reGrantedConsent.ExpiresAt);
            Assert.True(reGrantedConsent.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task GrantConsent_WithDifferentScopes_QueryReturnsCorrectScopes()
        {
            // Arrange
            var userId = "user_scopes";
            var clientId = "client_scopes";
            var grantedScopes = new[] { "openid", "profile", "email" };
            var requestedScopes = new[] { "openid", "profile", "email", "phone" };

            var request = new ConsentRequest
            {
                UserId = userId,
                ClientId = clientId,
                GrantedScopes = grantedScopes.ToList(),
                Approved = true,
                RememberConsent = false,
                IpAddress = "127.0.0.1",
                UserAgent = "Test Agent"
            };

            // Act - Grant consent for subset of scopes
            await _consentService.RecordConsentAsync(request);

            // Assert - Query should return only granted scopes
            var effectiveScopes = await _consentService.GetEffectiveScopesAsync(userId, clientId, requestedScopes);
            Assert.Equal(3, effectiveScopes.Count());
            Assert.Contains("openid", effectiveScopes);
            Assert.Contains("profile", effectiveScopes);
            Assert.Contains("email", effectiveScopes);
            Assert.DoesNotContain("phone", effectiveScopes);
        }

    [Fact]
    public async Task HasConsentAsync_ExpiredConsent_ReturnsFalse()
    {
        // Arrange
        var expiredConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user11",
            ClientId = "client11",
            GrantedScopes = "openid profile email",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-40),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        // Act
        var hasConsent = await _consentService.HasConsentAsync(
            "user11",
            "client11",
            ["openid", "profile"],
            CancellationToken.None
        );

        // Assert
        Assert.False(hasConsent);
    }

    [Fact]
    public async Task HasConsentAsync_ValidConsent_ReturnsTrue()
    {
        // Arrange
        var validConsent = new Consent
        {
            ConsentId = Guid.NewGuid().ToString(),
            UserId = "user12",
            ClientId = "client12",
            GrantedScopes = "openid profile email",
            Status = ConsentStatus.Approved,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            ExpiresAt = DateTime.UtcNow.AddDays(25)
        };

        _fakeRepository.AddConsent(validConsent);

        // Act
        var hasConsent = await _consentService.HasConsentAsync(
            "user12",
            "client12",
            ["openid", "profile"],
            CancellationToken.None
        );

        // Assert
        Assert.True(hasConsent);
    }

    // Fake implementation for testing
    private sealed class FakeConsentRepository : IConsentRepository
    {
        private readonly Dictionary<string, Consent> _consents = [];

        public Task<Consent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            _consents.TryGetValue(id, out var consent);
            return Task.FromResult<Consent?>(consent);
        }

        public Task<IEnumerable<Consent>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IEnumerable<Consent>>(_consents.Values);
        }

        public Task<Consent> CreateAsync(Consent entity, CancellationToken cancellationToken = default)
        {
            _consents[entity.ConsentId] = entity;
            return Task.FromResult(entity);
        }

        public Task<Consent> UpdateAsync(Consent entity, CancellationToken cancellationToken = default)
        {
            _consents[entity.ConsentId] = entity;
            return Task.FromResult(entity);
        }

        public Task DeleteAsync(Consent entity, CancellationToken cancellationToken = default)
        {
            _consents.Remove(entity.ConsentId);
            return Task.CompletedTask;
        }

        public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            _consents.Remove(id);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_consents.ContainsKey(id));
        }

        public Task<Consent?> GetByUserAndClientAsync(string userId, string clientId, CancellationToken cancellationToken = default)
        {
            var consent = _consents.Values.FirstOrDefault(c =>
                c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
                c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<Consent?>(consent);
        }

        public Task<IEnumerable<Consent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            var consents = _consents.Values.Where(c =>
                c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IEnumerable<Consent>>(consents);
        }

        public Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
        {
            var consents = _consents.Values.Where(c =>
                c.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();
            return Task.FromResult<IEnumerable<Consent>>(consents);
        }

        public Task RevokeAllUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var userConsents = _consents.Values.Where(c =>
                c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();

            foreach (var consent in userConsents)
            {
                consent.Revoke("User revoked all consents");
            }

            return Task.CompletedTask;
        }

        public void AddConsent(Consent consent)
        {
            _consents[consent.ConsentId] = consent;
        }
    }
}