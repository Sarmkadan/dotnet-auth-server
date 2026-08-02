using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace DotnetAuthServer.Tests.Handlers
{
    public class UserinfoHandlerTests
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<ILogger<UserinfoHandler>> _loggerMock;
        private readonly UserinfoHandler _handler;

        public UserinfoHandlerTests()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _loggerMock = new Mock<ILogger<UserinfoHandler>>();
            _handler = new UserinfoHandler(_userRepositoryMock.Object, _loggerMock.Object);
        }

        private ClaimsPrincipal CreatePrincipal(string? sub, string? scope)
        {
            var claims = new List<Claim>();
            if (sub != null) claims.Add(new Claim("sub", sub));
            if (scope != null) claims.Add(new Claim("scope", scope));
            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }

        [Fact]
        public async Task GetUserinfoAsync_ReturnsNull_WhenSubjectClaimMissing()
        {
            // Arrange
            var principal = CreatePrincipal(null, "openid profile email");

            // Act
            var result = await _handler.GetUserinfoAsync(principal);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserinfoAsync_ReturnsNull_WhenUserNotFound()
        {
            // Arrange
            var userId = "user-123";
            var principal = CreatePrincipal(userId, "openid profile email");
            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _handler.GetUserinfoAsync(principal);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetUserinfoAsync_ReturnsFullProfile_WhenProfileAndEmailScopesGranted()
        {
            // Arrange
            var userId = "user-123";
            var now = DateTime.UtcNow;
            var user = new User
            {
                UserId = userId,
                Username = "jdoe",
                FullName = "John Doe",
                Email = "john.doe@example.com",
                EmailVerified = true,
                UpdatedAt = now
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var principal = CreatePrincipal(userId, "openid profile email");

            // Act
            var result = await _handler.GetUserinfoAsync(principal);

            // Assert
            result.Should().NotBeNull();
            result!.Sub.Should().Be(userId);
            result.Name.Should().Be(user.FullName);
            result.GivenName.Should().Be("John");
            result.FamilyName.Should().Be("Doe");
            result.Email.Should().Be(user.Email);
            result.EmailVerified.Should().BeTrue();
            result.UpdatedAt.Should().HaveValue();
        }

        [Fact]
        public async Task GetUserinfoAsync_ReturnsOnlySub_WhenNoAdditionalScopes()
        {
            // Arrange
            var userId = "user-456";
            var user = new User
            {
                UserId = userId,
                Username = "alice",
                FullName = "Alice Wonderland"
            };

            _userRepositoryMock
                .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(user);

            var principal = CreatePrincipal(userId, "openid");

            // Act
            var result = await _handler.GetUserinfoAsync(principal);

            // Assert
            result.Should().NotBeNull();
            result!.Sub.Should().Be(userId);
            result.Name.Should().BeNull();
            result.GivenName.Should().BeNull();
            result.FamilyName.Should().BeNull();
            result.Email.Should().BeNull();
            result.EmailVerified.Should().BeNull();
        }
    }
}
