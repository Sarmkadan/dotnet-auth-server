// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Claims;

/// <summary>
/// Service for enforcing RBAC (Role-Based Access Control) and ABAC (Attribute-Based Access Control).
/// Evaluates policies to determine if a user is allowed to perform an action or access a resource.
/// Essential for fine-grained authorization beyond simple token validation.
/// </summary>
public class PolicyEnforcementService
{
    private readonly ILogger<PolicyEnforcementService> _logger;
    private readonly Dictionary<string, Policy> _policies = new();

    public PolicyEnforcementService(ILogger<PolicyEnforcementService> logger)
    {
        _logger = logger;
        InitializeDefaultPolicies();
    }

    /// <summary>
    /// Registers a new policy that can be evaluated against principals.
    /// </summary>
    public void RegisterPolicy(string policyName, Policy policy)
    {
        _policies[policyName] = policy;
        _logger.LogInformation("Policy registered: {PolicyName}", policyName);
    }

    /// <summary>
    /// Evaluates whether a principal satisfies a named policy.
    /// </summary>
    public bool EvaluatePolicy(string policyName, ClaimsPrincipal principal)
    {
        if (!_policies.TryGetValue(policyName, out var policy))
        {
            _logger.LogWarning("Policy not found: {PolicyName}", policyName);
            return false;
        }

        return EvaluatePolicy(policy, principal);
    }

    /// <summary>
    /// Evaluates whether a principal satisfies a policy object directly.
    /// Supports combining multiple rules with AND/OR logic.
    /// </summary>
    public bool EvaluatePolicy(Policy policy, ClaimsPrincipal principal)
    {
        if (policy == null || principal == null)
            return false;

        var results = policy.Rules.Select(rule => EvaluateRule(rule, principal)).ToList();

        if (results.Count == 0)
            return false;

        return policy.CombineWith switch
        {
            PolicyCombineMode.All => results.All(r => r),
            PolicyCombineMode.Any => results.Any(r => r),
            _ => false
        };
    }

    /// <summary>
    /// Evaluates a single policy rule against a principal.
    /// Supports role-based, attribute-based, and scope-based rules.
    /// </summary>
    private bool EvaluateRule(PolicyRule rule, ClaimsPrincipal principal)
    {
        return rule.Type switch
        {
            PolicyRuleType.Role => EvaluateRoleRule(rule, principal),
            PolicyRuleType.Attribute => EvaluateAttributeRule(rule, principal),
            PolicyRuleType.Scope => EvaluateScopeRule(rule, principal),
            PolicyRuleType.Claim => EvaluateClaimRule(rule, principal),
            _ => false
        };
    }

    private bool EvaluateRoleRule(PolicyRule rule, ClaimsPrincipal principal)
    {
        var userRoles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var requiredRoles = rule.Values;

        return rule.Match switch
        {
            PolicyMatchMode.Any => requiredRoles.Any(r => userRoles.Contains(r)),
            PolicyMatchMode.All => requiredRoles.All(r => userRoles.Contains(r)),
            PolicyMatchMode.None => !requiredRoles.Any(r => userRoles.Contains(r)),
            _ => false
        };
    }

    private bool EvaluateAttributeRule(PolicyRule rule, ClaimsPrincipal principal)
    {
        var attributeValue = principal.FindFirst(rule.Attribute)?.Value;
        if (string.IsNullOrEmpty(attributeValue))
            return rule.Match == PolicyMatchMode.None;

        return rule.Match switch
        {
            PolicyMatchMode.Any => rule.Values.Contains(attributeValue),
            PolicyMatchMode.All => true, // Single attribute can't match all
            PolicyMatchMode.None => !rule.Values.Contains(attributeValue),
            _ => false
        };
    }

    private bool EvaluateScopeRule(PolicyRule rule, ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst("scope")?.Value ?? "";
        var userScopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var requiredScopes = rule.Values;

        return rule.Match switch
        {
            PolicyMatchMode.Any => requiredScopes.Any(s => userScopes.Contains(s)),
            PolicyMatchMode.All => requiredScopes.All(s => userScopes.Contains(s)),
            PolicyMatchMode.None => !requiredScopes.Any(s => userScopes.Contains(s)),
            _ => false
        };
    }

    private bool EvaluateClaimRule(PolicyRule rule, ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(rule.Attribute);
        if (claim == null)
            return rule.Match == PolicyMatchMode.None;

        return rule.Match switch
        {
            PolicyMatchMode.Any => rule.Values.Contains(claim.Value),
            PolicyMatchMode.All => true,
            PolicyMatchMode.None => !rule.Values.Contains(claim.Value),
            _ => false
        };
    }

    private void InitializeDefaultPolicies()
    {
        // Policy requiring admin role
        RegisterPolicy("AdminOnly", new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Role,
                    Values = new List<string> { "admin" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        });

        // Policy requiring consent scope
        RegisterPolicy("ConsentRequired", new Policy
        {
            Rules = new List<PolicyRule>
            {
                new PolicyRule
                {
                    Type = PolicyRuleType.Scope,
                    Values = new List<string> { "consent" },
                    Match = PolicyMatchMode.Any
                }
            },
            CombineWith = PolicyCombineMode.All
        });
    }
}

/// <summary>
/// Represents a policy that can be evaluated against a principal.
/// Contains one or more rules combined with AND/OR logic.
/// </summary>
public class Policy
{
    public List<PolicyRule> Rules { get; set; } = new();
    public PolicyCombineMode CombineWith { get; set; } = PolicyCombineMode.All;
}

/// <summary>
/// Single policy rule that checks a specific condition.
/// </summary>
public class PolicyRule
{
    public PolicyRuleType Type { get; set; }
    public string? Attribute { get; set; } // For attribute/claim rules
    public List<string> Values { get; set; } = new();
    public PolicyMatchMode Match { get; set; } = PolicyMatchMode.Any;
}

/// <summary>
/// How multiple rules are combined.
/// </summary>
public enum PolicyCombineMode
{
    All,  // All rules must be satisfied
    Any   // At least one rule must be satisfied
}

/// <summary>
/// Type of policy rule.
/// </summary>
public enum PolicyRuleType
{
    Role,       // Role-based access control
    Attribute,  // Attribute value matching
    Scope,      // OAuth2 scope checking
    Claim       // Generic claim matching
}

/// <summary>
/// How rule values are matched against user values.
/// </summary>
public enum PolicyMatchMode
{
    Any,   // User has at least one of the values
    All,   // User has all of the values
    None   // User has none of the values
}
