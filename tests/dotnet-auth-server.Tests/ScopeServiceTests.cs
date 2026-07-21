using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Tests for the ScopeService class.
/// </summary>
public sealed class ScopeServiceTests
{
    private readonly Mock<IScopeRepository> _scopeRepositoryMock;
    private readonly ScopeService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScopeServiceTests"/> class.
    /// </summary>
    public ScopeServiceTests()
    {
        _scopeRepositoryMock = new Mock<IScopeRepository>();
        _service = new ScopeService(_scopeRepositoryMock.Object);
    }

    #region CreateScopeAsync Tests

    /// <summary>
    /// Tests that creating a scope with valid parameters succeeds.
    /// </summary>
    [Fact]
    public async Task CreateScopeAsync_ValidParameters_CreatesScope()
    {
        // Arrange
        var scopeId = "read";
        var displayName = "Read Access";
        var description = "Allows read access to resources";
        var expectedScope = new Scope
        {
            ScopeId = scopeId,
            DisplayName = displayName,
            Description = description,
            IsActive = true
        };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scope?)null);
        _scopeRepositoryMock.Setup(r => r.CreateAsync(It.IsAny<Scope>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedScope);

        // Act
        var result = await _service.CreateScopeAsync(scopeId, displayName, description);

        // Assert
        result.Should().NotBeNull();
        result!.ScopeId.Should().Be(scopeId);
        result.DisplayName.Should().Be(displayName);
        result.Description.Should().Be(description);
        result.IsActive.Should().BeTrue();
    }

    /// <summary>
    /// Tests that creating a scope with empty scope ID throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task CreateScopeAsync_EmptyScopeId_ThrowsAuthServerException()
    {
        // Act
        Func<Task> act = async () => await _service.CreateScopeAsync("", "Read Access", "Description");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 400);
    }

    /// <summary>
    /// Tests that creating a scope with whitespace-only scope ID throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task CreateScopeAsync_WhitespaceScopeId_ThrowsAuthServerException()
    {
        // Act
        Func<Task> act = async () => await _service.CreateScopeAsync("   ", "Read Access", "Description");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 400);
    }

    /// <summary>
    /// Tests that creating a scope with null scope ID throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task CreateScopeAsync_NullScopeId_ThrowsAuthServerException()
    {
        // Act
        Func<Task> act = async () => await _service.CreateScopeAsync(null!, "Read Access", "Description");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 400);
    }

    /// <summary>
    /// Tests that creating a scope that already exists throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task CreateScopeAsync_ScopeAlreadyExists_ThrowsAuthServerException()
    {
        // Arrange
        var scopeId = "read";
        var existingScope = new Scope { ScopeId = scopeId };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingScope);

        // Act
        Func<Task> act = async () => await _service.CreateScopeAsync(scopeId, "Read Access", "Description");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 400);
    }

    #endregion

    #region ValidateAndFilterScopesAsync Tests

    /// <summary>
    /// Tests that validating scopes with valid known scopes returns those scopes.
    /// </summary>
    [Fact]
    public async Task ValidateAndFilterScopesAsync_ValidKnownScopes_ReturnsScopes()
    {
        // Arrange
        var requestedScopes = new[] { "read", "write" };
        var userRoles = new[] { "user" };

        var readScope = new Scope { ScopeId = "read", IsActive = true, AllowedRoles = new List<string> { "user" } };
        var writeScope = new Scope { ScopeId = "write", IsActive = true, AllowedRoles = new List<string> { "user" } };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(readScope);
        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeScope);

        // Act
        var result = await _service.ValidateAndFilterScopesAsync(requestedScopes, userRoles);

        // Assert
        result.Should().BeEquivalentTo(new[] { "read", "write" });
    }

    /// <summary>
    /// Tests that validating scopes with unknown scope returns only valid scopes.
    /// </summary>
    [Fact]
    public async Task ValidateAndFilterScopesAsync_UnknownScope_ReturnsOnlyValidScopes()
    {
        // Arrange
        var requestedScopes = new[] { "read", "unknown", "write" };
        var userRoles = new[] { "user" };

        var readScope = new Scope { ScopeId = "read", IsActive = true, AllowedRoles = new List<string> { "user" } };
        var writeScope = new Scope { ScopeId = "write", IsActive = true, AllowedRoles = new List<string> { "user" } };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(readScope);
        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("unknown", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scope?)null);
        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeScope);

        // Act
        var result = await _service.ValidateAndFilterScopesAsync(requestedScopes, userRoles);

        // Assert
        result.Should().BeEquivalentTo(new[] { "read", "write" });
    }

    /// <summary>
    /// Tests that validating scopes with inactive scope returns only active scopes.
    /// </summary>
    [Fact]
    public async Task ValidateAndFilterScopesAsync_InactiveScope_ReturnsOnlyActiveScopes()
    {
        // Arrange
        var requestedScopes = new[] { "read", "write" };
        var userRoles = new[] { "user" };

        var readScope = new Scope { ScopeId = "read", IsActive = true, AllowedRoles = new List<string> { "user" } };
        var writeScope = new Scope { ScopeId = "write", IsActive = false, AllowedRoles = new List<string> { "user" } };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(readScope);
        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("write", It.IsAny<CancellationToken>()))
            .ReturnsAsync(writeScope);

        // Act
        var result = await _service.ValidateAndFilterScopesAsync(requestedScopes, userRoles);

        // Assert
        result.Should().BeEquivalentTo(new[] { "read" });
    }

    /// <summary>
    /// Tests that validating scopes with role restrictions returns only accessible scopes.
    /// </summary>
    [Fact]
    public async Task ValidateAndFilterScopesAsync_RoleRestrictedScope_ReturnsOnlyAccessibleScopes()
    {
        // Arrange
        var requestedScopes = new[] { "admin", "user" };
        var adminRoles = new[] { "admin" };
        var userRoles = new[] { "user" };

        var adminScope = new Scope { ScopeId = "admin", IsActive = true, AllowedRoles = new List<string> { "admin" } };
        var userScope = new Scope { ScopeId = "user", IsActive = true, AllowedRoles = new List<string>() };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("admin", It.IsAny<CancellationToken>()))
            .ReturnsAsync(adminScope);
        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync("user", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userScope);

        // Act - admin can access both
        var adminResult = await _service.ValidateAndFilterScopesAsync(requestedScopes, adminRoles);
        adminResult.Should().BeEquivalentTo(new[] { "admin", "user" });

        // Act - user can only access user scope
        var userResult = await _service.ValidateAndFilterScopesAsync(requestedScopes, userRoles);
        userResult.Should().BeEquivalentTo(new[] { "user" });
    }

    /// <summary>
    /// Tests that validating empty scopes returns empty collection.
    /// </summary>
    [Fact]
    public async Task ValidateAndFilterScopesAsync_EmptyScopes_ReturnsEmptyCollection()
    {
        // Arrange
        var requestedScopes = Array.Empty<string>();
        var userRoles = new[] { "user" };

        // Act
        var result = await _service.ValidateAndFilterScopesAsync(requestedScopes, userRoles);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region AddClaimToScopeAsync Tests

    /// <summary>
    /// Tests that adding a claim to a scope succeeds.
    /// </summary>
    [Fact]
    public async Task AddClaimToScopeAsync_AddIdTokenClaim_Succeeds()
    {
        // Arrange
        var scopeId = "openid";
        var claim = "sub";
        var scope = new Scope { ScopeId = scopeId, IdTokenClaims = new List<string>(), AccessTokenClaims = new List<string>() };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);
        _scopeRepositoryMock.Setup(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);

        // Act
        await _service.AddClaimToScopeAsync(scopeId, claim);

        // Assert
        scope.IdTokenClaims.Should().Contain(claim);
        _scopeRepositoryMock.Verify(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that adding an access token claim succeeds.
    /// </summary>
    [Fact]
    public async Task AddClaimToScopeAsync_AddAccessTokenClaim_Succeeds()
    {
        // Arrange
        var scopeId = "api";
        var claim = "user_id";
        var scope = new Scope { ScopeId = "api", IdTokenClaims = new List<string>(), AccessTokenClaims = new List<string>() };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);
        _scopeRepositoryMock.Setup(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);

        // Act
        await _service.AddClaimToScopeAsync(scopeId, claim, isIdTokenClaim: false);

        // Assert
        scope.AccessTokenClaims.Should().Contain(claim);
        _scopeRepositoryMock.Verify(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that adding a duplicate claim doesn't add it twice.
    /// </summary>
    [Fact]
    public async Task AddClaimToScopeAsync_AddDuplicateClaim_DoesNotAddTwice()
    {
        // Arrange
        var scopeId = "openid";
        var claim = "sub";
        var scope = new Scope { ScopeId = scopeId, IdTokenClaims = new List<string> { claim }, AccessTokenClaims = new List<string>() };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);
        _scopeRepositoryMock.Setup(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);

        // Act
        await _service.AddClaimToScopeAsync(scopeId, claim);

        // Assert
        scope.IdTokenClaims.Should().HaveCount(1);
        scope.IdTokenClaims.Should().Contain(claim);
    }

    /// <summary>
    /// Tests that adding a claim to non-existent scope throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task AddClaimToScopeAsync_NonExistentScope_ThrowsAuthServerException()
    {
        // Arrange
        var scopeId = "nonexistent";
        var claim = "sub";

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scope?)null);

        // Act
        Func<Task> act = async () => await _service.AddClaimToScopeAsync(scopeId, claim);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 404);
    }

    #endregion

    #region AssignRoleAsync Tests

    /// <summary>
    /// Tests that assigning a role to a scope succeeds.
    /// </summary>
    [Fact]
    public async Task AssignRoleAsync_AddRole_Succeeds()
    {
        // Arrange
        var scopeId = "admin";
        var role = "super_admin";
        var scope = new Scope { ScopeId = scopeId, AllowedRoles = new List<string>() };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);
        _scopeRepositoryMock.Setup(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);

        // Act
        await _service.AssignRoleAsync(scopeId, role);

        // Assert
        scope.AllowedRoles.Should().Contain(role);
        _scopeRepositoryMock.Verify(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// Tests that assigning a duplicate role doesn't add it twice.
    /// </summary>
    [Fact]
    public async Task AssignRoleAsync_AddDuplicateRole_DoesNotAddTwice()
    {
        // Arrange
        var scopeId = "admin";
        var role = "admin";
        var scope = new Scope { ScopeId = scopeId, AllowedRoles = new List<string> { role } };

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);
        _scopeRepositoryMock.Setup(r => r.UpdateAsync(scope, It.IsAny<CancellationToken>()))
            .ReturnsAsync(scope);

        // Act
        await _service.AssignRoleAsync(scopeId, role);

        // Assert
        scope.AllowedRoles.Should().HaveCount(1);
        scope.AllowedRoles.Should().Contain(role);
    }

    /// <summary>
    /// Tests that assigning a role to non-existent scope throws AuthServerException.
    /// </summary>
    [Fact]
    public async Task AssignRoleAsync_NonExistentScope_ThrowsAuthServerException()
    {
        // Arrange
        var scopeId = "nonexistent";
        var role = "admin";

        _scopeRepositoryMock.Setup(r => r.GetByScopeIdAsync(scopeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Scope?)null);

        // Act
        Func<Task> act = async () => await _service.AssignRoleAsync(scopeId, role);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.ErrorCode == "invalid_request" && ex.StatusCode == 404);
    }

    #endregion

    #region GetScopesWithClaimsAsync Tests

    /// <summary>
    /// Tests that getting scopes with claims returns active scopes with their claims.
    /// </summary>
    [Fact]
    public async Task GetScopesWithClaimsAsync_ReturnsScopesWithClaims()
    {
        // Arrange
        var scope1 = new Scope
        {
            ScopeId = "read",
            DisplayName = "Read Access",
            Description = "Allows read access",
            IsActive = true,
            IdTokenClaims = new List<string> { "sub", "name" },
            AccessTokenClaims = new List<string> { "scope" }
        };

        var scope2 = new Scope
        {
            ScopeId = "write",
            DisplayName = "Write Access",
            Description = "Allows write access",
            IsActive = true,
            IdTokenClaims = new List<string>(),
            AccessTokenClaims = new List<string> { "user_id" }
        };

        var inactiveScope = new Scope
        {
            ScopeId = "inactive",
            DisplayName = "Inactive Scope",
            Description = "Inactive scope",
            IsActive = false
        };

        _scopeRepositoryMock.Setup(r => r.GetActiveScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { scope1, scope2, inactiveScope });

        // Act
        var result = await _service.GetScopesWithClaimsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().ContainSingle(s => s.ScopeId == "read" && s.Claims.Count() == 3);
        result.Should().ContainSingle(s => s.ScopeId == "write" && s.Claims.Count() == 1);
        result.Should().NotContain(s => s.ScopeId == "inactive");
    }

    /// <summary>
    /// Tests that getting scopes with claims returns empty collection when no active scopes exist.
    /// </summary>
    [Fact]
    public async Task GetScopesWithClaimsAsync_NoActiveScopes_ReturnsEmptyCollection()
    {
        // Arrange
        _scopeRepositoryMock.Setup(r => r.GetActiveScopesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Scope>());

        // Act
        var result = await _service.GetScopesWithClaimsAsync();

        // Assert
        result.Should().BeEmpty();
    }

    #endregion
}
