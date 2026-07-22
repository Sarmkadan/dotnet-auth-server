using System.Security.Claims;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Tests for the PolicyEnforcementService class.
/// Tests allow/deny decisions, missing policy, and attribute matching.
/// </summary>
public sealed class PolicyEnforcementServiceTests
{
    private readonly ILogger<PolicyEnforcementService> _logger;
    private readonly PolicyEnforcementService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="PolicyEnforcementServiceTests"/> class.
    /// </summary>
    public PolicyEnforcementServiceTests()
    {
        _logger = new MockLogger<PolicyEnforcementService>();
        _service = new PolicyEnforcementService(_logger);
    }

    #region Constructor and Initial State

    /// <summary>
    /// Tests that the PolicyEnforcementService is initialized with default policies.
    /// </summary>
    [Fact]
    public void Constructor_InitializesWithDefaultPolicies()
    {
        // Arrange & Act
        var service = new PolicyEnforcementService(_logger);

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region Allow/Deny Decisions - Role-Based Rules

    /// <summary>
    /// Tests that a user with admin role is allowed by AdminOnly policy.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_AdminRole_Allowed()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Name, "adminuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.EvaluatePolicy("AdminOnly", principal);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a user without admin role is denied by AdminOnly policy.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_NonAdminRole_Denied()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.Name, "regularuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.EvaluatePolicy("AdminOnly", principal);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a user with any required role is allowed when using Any match mode.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_RoleAnyMatch_AllowedWithAnyRole()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "editor"),
            new Claim(ClaimTypes.Role, "contributor"),
            new Claim(ClaimTypes.Name, "contentuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy with Any match mode
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "admin", "editor", "supervisor" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("ContentEditor", policy);

        // Act
        var result = _service.EvaluatePolicy("ContentEditor", principal);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a user missing all required roles is denied when using Any match mode.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_RoleAnyMatch_DeniedWithNoMatchingRoles()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "user"),
            new Claim(ClaimTypes.Name, "basicuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy requiring admin or editor
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "admin", "editor" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("AdminOrEditor", policy);

        // Act
        var result = _service.EvaluatePolicy("AdminOrEditor", principal);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Allow/Deny Decisions - Scope-Based Rules

    /// <summary>
    /// Tests that a user with required scope is allowed by ConsentRequired policy.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_RequiredScope_Allowed()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("scope", "consent read:data"),
            new Claim(ClaimTypes.Name, "userwithscope")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.EvaluatePolicy("ConsentRequired", principal);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a user without required scope is denied.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_MissingRequiredScope_Denied()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("scope", "read:data"),
            new Claim(ClaimTypes.Name, "userwithoutscope")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.EvaluatePolicy("ConsentRequired", principal);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that a user with all required scopes is allowed when using All match mode.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_ScopeAllMatch_AllowedWithAllScopes()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("scope", "read:data write:data delete:data"),
            new Claim(ClaimTypes.Name, "adminuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy requiring all three scopes
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Scope,
                    Values = new List<string> { "read:data", "write:data", "delete:data" },
                    Match = PolicyMatchMode.All
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("FullDataAccess", policy);

        // Act
        var result = _service.EvaluatePolicy("FullDataAccess", principal);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Missing Policy Tests

    /// <summary>
    /// Tests that evaluating a non-existent policy returns false.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_MissingPolicy_ReturnsFalse()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim(ClaimTypes.Name, "adminuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = _service.EvaluatePolicy("NonExistentPolicy", principal);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Attribute Matching Tests

    /// <summary>
    /// Tests that attribute-based rule matches when user has the required attribute value.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_AttributeMatch_Allowed()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("department", "engineering"),
            new Claim(ClaimTypes.Name, "enguser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy for department attribute
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering", "finance" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("EngineeringOrFinance", policy);

        // Act
        var result = _service.EvaluatePolicy("EngineeringOrFinance", principal);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that attribute-based rule denies when user lacks the required attribute.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_AttributeNoMatch_Denied()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("department", "marketing"),
            new Claim(ClaimTypes.Name, "mktuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy for department attribute
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering", "finance" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("EngineeringOrFinance", policy);

        // Act
        var result = _service.EvaluatePolicy("EngineeringOrFinance", principal);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Tests that attribute-based rule with None match mode allows when user has none of the values.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_AttributeNoneMatch_AllowedWhenNoValuesMatch()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("department", "marketing"),
            new Claim(ClaimTypes.Name, "mktuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a policy that should match when user has NONE of the values
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering", "finance" },
                    Match = PolicyMatchMode.None
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("NotEngineeringOrFinance", policy);

        // Act
        var result = _service.EvaluatePolicy("NotEngineeringOrFinance", principal);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Claim-Based Rule Tests

    /// <summary>
    /// Tests that claim-based rule matches when user has the required claim value.
    /// </summary>
    [Fact]
    public void EvaluatePolicy_ClaimMatch_Allowed()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim("user_type", "premium"),
            new Claim(ClaimTypes.Name, "premiumuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a custom policy for user_type claim
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Claim,
                    Attribute = "user_type",
                    Values = new List<string> { "premium", "enterprise" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("PremiumOrEnterprise", policy);

        // Act
        var result = _service.EvaluatePolicy("PremiumOrEnterprise", principal);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Policy Evaluation with Multiple Rules

    /// <summary>
    /// Tests that a policy with multiple rules requires all rules to pass (All combine mode).
    /// </summary>
    [Fact]
    public void EvaluatePolicy_MultipleRulesAllMode_RequiresAllRules()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("department", "engineering"),
            new Claim(ClaimTypes.Name, "adminuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a policy with two rules that both must pass
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "admin" },
                    Match = PolicyMatchMode.Any
                },
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("AdminAndEngineering", policy);

        // Act
        var result = _service.EvaluatePolicy("AdminAndEngineering", principal);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Tests that a policy with multiple rules denies when any rule fails (All combine mode).
    /// </summary>
    [Fact]
    public void EvaluatePolicy_MultipleRulesAllMode_DeniesWhenAnyRuleFails()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Role, "admin"),
            new Claim("department", "marketing"), // Wrong department
            new Claim(ClaimTypes.Name, "adminuser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        // Create a policy with two rules that both must pass
        var policy = new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "admin" },
                    Match = PolicyMatchMode.Any
                },
                new PolicyRule
                {
                    Type = PolicyRuleType.Attribute,
                    Attribute = "department",
                    Values = new List<string> { "engineering" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        };
        _service.RegisterPolicy("AdminAndEngineering", policy);

        // Act
        var result = _service.EvaluatePolicy("AdminAndEngineering", principal);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}

/// <summary>
/// Mock logger implementation for testing.
/// </summary>
/// <typeparam name="T">The type being logged.</typeparam>
internal sealed class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) => null;

    public bool IsEnabled(LogLevel logLevel) => false;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // No-op for testing
    }
}