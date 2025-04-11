#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Claims;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Integration;

/// <summary>
/// Service for enforcing RBAC (Role-Based Access Control) and ABAC (Attribute-Based Access Control).
/// Evaluates policies to determine if a user is allowed to perform an action or access a resource.
/// When OPA integration is enabled (<see cref="OpaOptions.Enabled"/> = true) policy decisions are
/// delegated to the Open Policy Agent REST API; otherwise the built-in evaluator is used.
/// </summary>
public sealed class PolicyEnforcementService
{
    private readonly ILogger<PolicyEnforcementService> _logger;
    private readonly OpaClient? _opaClient;
    private readonly OpaOptions? _opaOptions;
    private readonly Dictionary<string, Policy> _policies = new();

    public PolicyEnforcementService(ILogger<PolicyEnforcementService> logger)
    {
        _logger = logger;
        InitializeDefaultPolicies();
    }

    /// <summary>
    /// Constructor used when OPA integration is enabled.
    /// </summary>
    public PolicyEnforcementService(
        ILogger<PolicyEnforcementService> logger,
        OpaClient opaClient,
        OpaOptions opaOptions)
    {
        _logger = logger;
        _opaClient = opaClient;
        _opaOptions = opaOptions;
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
    /// When OPA is enabled, the decision is delegated to OPA first.
    /// If OPA is unreachable and <see cref="OpaOptions.FailClosedOnError"/> is false,
    /// the built-in evaluator is used as a fallback.
    /// </summary>
    public bool EvaluatePolicy(string policyName, ClaimsPrincipal principal)
    {
        if (_opaClient is not null && _opaOptions?.Enabled == true)
        {
            // Run synchronously; callers that need async should use EvaluatePolicyAsync.
            var opaResult = _opaClient.EvaluatePolicyAsync(policyName, principal)
                .GetAwaiter().GetResult();

            if (opaResult.HasValue)
                return opaResult.Value;

            // opaResult is null → OPA unreachable + FailClosedOnError == false → fall through
        }

        if (!_policies.TryGetValue(policyName, out var policy))
        {
            _logger.LogWarning("Policy not found: {PolicyName}", policyName);
            return false;
        }

        return EvaluatePolicy(policy, principal);
    }

    /// <summary>
    /// Async version that avoids blocking the thread when OPA is used.
    /// </summary>
    public async Task<bool> EvaluatePolicyAsync(string policyName, ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (_opaClient is not null && _opaOptions?.Enabled == true)
        {
            var opaResult = await _opaClient.EvaluatePolicyAsync(policyName, principal, cancellationToken);
            if (opaResult.HasValue)
                return opaResult.Value;
        }

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
        if (policy is null || principal is null)
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
        if (claim is null)
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
public sealed class Policy
{
    public List<PolicyRule> Rules { get; set; } = new();
    public PolicyCombineMode CombineWith { get; set; } = PolicyCombineMode.All;
}

/// <summary>
/// Single policy rule that checks a specific condition.
/// </summary>
public sealed class PolicyRule
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
