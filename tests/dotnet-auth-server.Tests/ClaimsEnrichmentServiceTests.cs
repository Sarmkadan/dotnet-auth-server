using System;
using System.Security.Claims;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests.Services
{
    public class ClaimsEnrichmentServiceTests
    {
        private readonly ClaimsEnrichmentService _service;
        private readonly Mock<IUserRepository> _userRepository = new();
        private readonly Mock<ILogger<ClaimsEnrichmentService>> _logger = new();

        public ClaimsEnrichmentServiceTests()
        {
            _service = new ClaimsEnrichmentService(_userRepository.Object, _logger.Object);
        }

        [Fact]
        public void EnrichUserClaims_AddsClaimsForUserAttributes()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                EmailVerified = true,
                FullName = "Test User",
                Roles = new List<string> { "admin" },
                Attributes = new Dictionary<string, object> { { "custom", "value" } }
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string> { "profile", "email" }).Result;

            // Assert
            claims.Should().Contain(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.UserId);
            claims.Should().Contain(claim => claim.Type == "name" && claim.Value == user.FullName);
            claims.Should().Contain(claim => claim.Type == "email" && claim.Value == user.Email);
            claims.Should().Contain(claim => claim.Type == "email_verified" && claim.Value == user.EmailVerified.ToString().ToLower());
            claims.Should().Contain(claim => claim.Type == "roles" && claim.Value == "admin");
            claims.Should().Contain(claim => claim.Type == "preferred_username" && claim.Value == user.Username);
            claims.Should().Contain(claim => claim.Type == "account_created");
        }

        [Fact]
        public void EnrichUserClaims_DoesNotAddDuplicates()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                EmailVerified = true,
                FullName = "Test User",
                Roles = new List<string> { "admin" },
                Attributes = new Dictionary<string, object> { { "custom", "value" } }
            };

            // Act
            var claims1 = _service.EnrichUserClaimsAsync(user, new List<string> { "profile", "email" }).Result;
            var claims2 = _service.EnrichUserClaimsAsync(user, new List<string> { "profile", "email" }).Result;

            // Assert
            claims1.Should().HaveSameCount(claims2);
        }

        [Fact]
        public void EnrichUserClaims_AddsStandardIdentityClaims()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                FullName = "Test User",
                Roles = new List<string> { "user", "admin" }
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string>()).Result;

            // Assert - standard identity claims should always be present
            claims.Should().Contain(claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.UserId);
            claims.Should().Contain(claim => claim.Type == "sub" && claim.Value == user.UserId);
            claims.Should().HaveCount(2 + (user.Roles.Count * 2) + 2); // 2 identity claims + 2 claims per role (Role + roles) + 2 application claims (preferred_username, account_created)
        }

        [Fact]
        public void EnrichUserClaims_AddsProfileClaims_WhenScopeGranted()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                FullName = "John Doe"
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string> { "profile" }).Result;

            // Assert
            claims.Should().Contain(claim => claim.Type == ClaimTypes.Name && claim.Value == user.FullName);
            claims.Should().Contain(claim => claim.Type == "name" && claim.Value == user.FullName);
            claims.Should().Contain(claim => claim.Type == "given_name" && claim.Value == "John");
            claims.Should().Contain(claim => claim.Type == "family_name" && claim.Value == "Doe");
        }

        [Fact]
        public void EnrichUserClaims_AddsEmailClaims_WhenScopeGranted()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                EmailVerified = true
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string> { "email" }).Result;

            // Assert
            claims.Should().Contain(claim => claim.Type == ClaimTypes.Email && claim.Value == user.Email);
            claims.Should().Contain(claim => claim.Type == "email" && claim.Value == user.Email);
            claims.Should().Contain(claim => claim.Type == "email_verified" && claim.Value == "true");
        }

        [Fact]
        public void EnrichUserClaims_AddsRoleClaims()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Roles = new List<string> { "admin", "user", "moderator" }
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string>()).Result;

            // Assert
            claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "admin");
            claims.Should().Contain(claim => claim.Type == "roles" && claim.Value == "admin");
            claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "user");
            claims.Should().Contain(claim => claim.Type == "roles" && claim.Value == "user");
            claims.Should().Contain(claim => claim.Type == ClaimTypes.Role && claim.Value == "moderator");
            claims.Should().Contain(claim => claim.Type == "roles" && claim.Value == "moderator");
        }

        [Fact]
        public void EnrichUserClaims_AddsApplicationClaims()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                IsActive = true
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string>()).Result;

            // Assert
            claims.Should().Contain(claim => claim.Type == "preferred_username" && claim.Value == user.Username);
        }

        [Fact]
        public void EnrichUserClaims_HandlesNullFullName()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                FullName = null
            };

            // Act
            var claims = _service.EnrichUserClaimsAsync(user, new List<string> { "profile" }).Result;

            // Assert - should not add name claims when full name is null
            claims.Should().NotContain(claim => claim.Type == ClaimTypes.Name);
            claims.Should().NotContain(claim => claim.Type == "name");
            claims.Should().NotContain(claim => claim.Type == "given_name");
            claims.Should().NotContain(claim => claim.Type == "family_name");
        }

        [Fact]
        public void FilterClaimsByScope_RemovesNonGrantedClaims()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                FullName = "Test User",
                Roles = new List<string> { "admin" }
            };

            var claims = _service.EnrichUserClaimsAsync(user, new List<string>()).Result;

            // Act - filter to only profile scope
            var filtered = _service.FilterClaimsByScope(claims, new List<string> { "profile" });

            // Assert
            filtered.Should().Contain(claim => claim.Type == ClaimTypes.NameIdentifier);
            filtered.Should().Contain(claim => claim.Type == "sub");
            filtered.Should().Contain(claim => claim.Type == ClaimTypes.Role); // Roles always included
            filtered.Should().NotContain(claim => claim.Type == ClaimTypes.Email);
            filtered.Should().NotContain(claim => claim.Type == "email_verified");
        }

        [Fact]
        public void FilterClaimsByScope_IncludesAllClaims_WhenNoScopes()
        {
            // Arrange
            var user = new User
            {
                UserId = "user123",
                Username = "testuser",
                Email = "test@example.com",
                FullName = "Test User",
                Roles = new List<string> { "admin" }
            };

            var claims = _service.EnrichUserClaimsAsync(user, new List<string>()).Result;

            // Act - filter with no scopes
            var filtered = _service.FilterClaimsByScope(claims, new List<string>());

            // Assert - should include all claims
            filtered.Should().HaveSameCount(claims);
        }
    }
}