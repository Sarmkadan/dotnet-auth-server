// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Attribute-Based Access Control (ABAC) Example
/// Demonstrates fine-grained authorization beyond simple RBAC
/// </summary>
public interface IPolicyEngine
{
    Task<bool> EvaluateAsync(string policy, AccessContext context);
}

/// <summary>
/// Access context containing user, resource, and environment information
/// </summary>
public class AccessContext
{
    public UserAttributes User { get; set; } = new();
    public ResourceAttributes Resource { get; set; } = new();
    public EnvironmentAttributes Environment { get; set; } = new();
}

/// <summary>
/// User-related attributes for ABAC decisions
/// </summary>
public class UserAttributes
{
    public string UserId { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public string Department { get; set; } = string.Empty;
    public string CostCenter { get; set; } = string.Empty;
    public int TenureMonths { get; set; }
    public bool IsManager { get; set; }
    public List<string> Clearances { get; set; } = new();
}

/// <summary>
/// Resource-related attributes for ABAC decisions
/// </summary>
public class ResourceAttributes
{
    public string ResourceId { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty; // file, database, api, etc.
    public string Classification { get; set; } = string.Empty; // public, internal, confidential, secret
    public string Owner { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Environment attributes for context-aware policies
/// </summary>
public class EnvironmentAttributes
{
    public DateTime RequestTime { get; set; }
    public string SourceIp { get; set; } = string.Empty;
    public string DeviceType { get; set; } = string.Empty; // mobile, desktop, tablet
    public bool IsVpnConnected { get; set; }
    public string Location { get; set; } = string.Empty; // country code
}

/// <summary>
/// Attribute-Based Policy Engine Implementation
/// </summary>
public class AbacPolicyEngine : IPolicyEngine
{
    /// <summary>
    /// Evaluate access based on attributes
    /// </summary>
    public async Task<bool> EvaluateAsync(string policy, AccessContext context)
    {
        return await Task.FromResult(policy switch
        {
            "can_read_file" => CanReadFile(context),
            "can_write_file" => CanWriteFile(context),
            "can_access_during_business_hours" => IsBusinessHours(context),
            "can_access_from_office" => IsOfficeLocation(context),
            "can_approve_budget" => CanApproveBudget(context),
            "can_delete_resource" => CanDeleteResource(context),
            "requires_mfa" => RequiresMfa(context),
            _ => false
        });
    }

    /// <summary>
    /// Can user read file?
    /// Rules:
    /// - Owner can always read
    /// - Managers in same department can read
    /// - Users with explicit read tags
    /// </summary>
    private bool CanReadFile(AccessContext context)
    {
        var user = context.User;
        var resource = context.Resource;

        // Owner always can
        if (user.UserId == resource.Owner)
            return true;

        // Manager of department
        if (user.IsManager && user.Department == ExtractDepartmentFromResource(resource))
            return true;

        // User with explicit read access tag
        if (resource.Tags.Contains($"can_read:{user.UserId}"))
            return true;

        // Group-based access
        if (user.Roles.Any(role => resource.Tags.Contains($"can_read:{role}")))
            return true;

        return false;
    }

    /// <summary>
    /// Can user write file?
    /// Rules:
    /// - Owner can write
    /// - Users with 1+ year tenure and admin role
    /// - Explicit write permission tag
    /// - Classification must be lower than user clearance
    /// </summary>
    private bool CanWriteFile(AccessContext context)
    {
        var user = context.User;
        var resource = context.Resource;

        // Owner can write
        if (user.UserId == resource.Owner)
            return true;

        // Minimum tenure requirement
        if (user.TenureMonths < 12)
            return false;

        // Admin with tenure
        if (user.Roles.Contains("admin") && user.TenureMonths >= 12)
            return CheckClearanceLevel(user, resource);

        // Explicit write permission
        if (resource.Tags.Contains($"can_write:{user.UserId}"))
            return CheckClearanceLevel(user, resource);

        return false;
    }

    /// <summary>
    /// Can user access during business hours only?
    /// </summary>
    private bool IsBusinessHours(AccessContext context)
    {
        var time = context.Environment.RequestTime;
        var hour = time.Hour;
        var dayOfWeek = time.DayOfWeek;

        // Monday-Friday, 9am-5pm
        return dayOfWeek >= DayOfWeek.Monday && dayOfWeek <= DayOfWeek.Friday &&
               hour >= 9 && hour < 17;
    }

    /// <summary>
    /// Can user access from office location only?
    /// </summary>
    private bool IsOfficeLocation(AccessContext context)
    {
        var location = context.Environment.Location;
        var isVpn = context.Environment.IsVpnConnected;
        var deviceType = context.Environment.DeviceType;

        // Allow if on VPN or approved location
        // Require VPN for remote access, unless within approved locations
        return isVpn || location == "US" || location == "CA";
    }

    /// <summary>
    /// Can user approve budget above certain amount?
    /// Rules:
    /// - Manager role required
    /// - Approval limit based on tenure
    /// - During business hours
    /// </summary>
    private bool CanApproveBudget(AccessContext context)
    {
        var user = context.User;

        // Must be manager
        if (!user.IsManager)
            return false;

        // Must be business hours
        if (!IsBusinessHours(context))
            return false;

        // Must have sufficient tenure
        if (user.TenureMonths < 24)
            return false;

        return true;
    }

    /// <summary>
    /// Can user delete resource?
    /// Rules:
    /// - Owner can delete if created > 30 days ago
    /// - Admin can delete (with audit logging)
    /// - Classification must be approved
    /// </summary>
    private bool CanDeleteResource(AccessContext context)
    {
        var user = context.User;
        var resource = context.Resource;
        var resourceAge = DateTime.UtcNow - resource.CreatedAt;

        // Owner deletion with grace period
        if (user.UserId == resource.Owner && resourceAge.TotalDays > 30)
            return true;

        // Admin deletion always allowed (will be audited)
        if (user.Roles.Contains("admin"))
            return true;

        return false;
    }

    /// <summary>
    /// Does user need MFA to access this resource?
    /// Rules:
    /// - Confidential/Secret resources always require MFA
    /// - Non-office access requires MFA
    /// </summary>
    private bool RequiresMfa(AccessContext context)
    {
        var resource = context.Resource;
        var env = context.Environment;

        // Classification-based MFA
        if (resource.Classification is "confidential" or "secret")
            return true;

        // Location-based MFA
        if (!env.IsVpnConnected && env.Location != "US")
            return true;

        return false;
    }

    /// <summary>
    /// Check if user clearance is sufficient for resource
    /// </summary>
    private bool CheckClearanceLevel(UserAttributes user, ResourceAttributes resource)
    {
        var clearanceLevels = new Dictionary<string, int>
        {
            { "public", 1 },
            { "internal", 2 },
            { "confidential", 3 },
            { "secret", 4 }
        };

        if (!clearanceLevels.TryGetValue(resource.Classification, out var requiredLevel))
            return false;

        // User must have clearance at or above required level
        var userClearances = user.Clearances
            .Where(c => clearanceLevels.ContainsKey(c))
            .Select(c => clearanceLevels[c]);

        return userClearances.Any(level => level >= requiredLevel);
    }

    private string ExtractDepartmentFromResource(ResourceAttributes resource)
    {
        // Extract department from tags like "dept:engineering"
        return resource.Tags
            .FirstOrDefault(t => t.StartsWith("dept:"))
            ?.Replace("dept:", "") ?? string.Empty;
    }
}

/// <summary>
/// Example usage of ABAC policy engine
/// </summary>
public class AbacPolicyExample
{
    private readonly IPolicyEngine _policyEngine;

    public AbacPolicyExample()
    {
        _policyEngine = new AbacPolicyEngine();
    }

    /// <summary>
    /// Example 1: Manager accessing own department file during business hours
    /// </summary>
    public async Task ManagerAccessFileAsync()
    {
        Console.WriteLine("=== Example 1: Manager Accessing Department File ===\n");

        var context = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "user123",
                Roles = new List<string> { "manager" },
                Department = "engineering",
                IsManager = true,
                TenureMonths = 36,
                Clearances = new List<string> { "internal", "confidential" }
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "file456",
                ResourceType = "file",
                Classification = "internal",
                Owner = "user456",
                Tags = new List<string> { "dept:engineering", "team:backend" }
            },
            Environment = new EnvironmentAttributes
            {
                RequestTime = DateTime.Parse("2024-01-15 10:00:00"),
                SourceIp = "203.0.113.1",
                IsVpnConnected = false,
                Location = "US",
                DeviceType = "desktop"
            }
        };

        var canRead = await _policyEngine.EvaluateAsync("can_read_file", context);
        Console.WriteLine($"Can read file: {canRead}");

        var requiresMfa = await _policyEngine.EvaluateAsync("requires_mfa", context);
        Console.WriteLine($"Requires MFA: {requiresMfa}");

        var businessHours = await _policyEngine.EvaluateAsync("can_access_during_business_hours", context);
        Console.WriteLine($"Within business hours: {businessHours}\n");
    }

    /// <summary>
    /// Example 2: Junior developer with clearance restrictions
    /// </summary>
    public async Task JuniorDeveloperAccessAsync()
    {
        Console.WriteLine("=== Example 2: Junior Developer with Restrictions ===\n");

        var context = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "user789",
                Roles = new List<string> { "developer" },
                Department = "engineering",
                IsManager = false,
                TenureMonths = 3, // Only 3 months tenure
                Clearances = new List<string> { "internal" } // No secret clearance
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "file789",
                ResourceType = "file",
                Classification = "secret", // Requires high clearance
                Owner = "user123",
                Tags = new List<string> { "dept:engineering", "secret" }
            },
            Environment = new EnvironmentAttributes
            {
                RequestTime = DateTime.Parse("2024-01-15 14:00:00"),
                SourceIp = "203.0.113.1",
                IsVpnConnected = true,
                Location = "US"
            }
        };

        var canWrite = await _policyEngine.EvaluateAsync("can_write_file", context);
        Console.WriteLine($"Can write file: {canWrite} (insufficient tenure)");

        var requiresMfa = await _policyEngine.EvaluateAsync("requires_mfa", context);
        Console.WriteLine($"Requires MFA: {requiresMfa} (secret classification)\n");
    }

    /// <summary>
    /// Example 3: Off-hours access from remote location
    /// </summary>
    public async Task RemoteOffHoursAccessAsync()
    {
        Console.WriteLine("=== Example 3: Remote Off-Hours Access ===\n");

        var context = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "user999",
                Roles = new List<string> { "admin" },
                Department = "operations",
                IsManager = true,
                TenureMonths = 60,
                Clearances = new List<string> { "secret" }
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "file999",
                ResourceType = "api",
                Classification = "confidential",
                Owner = "user999",
                Tags = new List<string> { "critical", "production" }
            },
            Environment = new EnvironmentAttributes
            {
                RequestTime = DateTime.Parse("2024-01-15 22:00:00"), // 10 PM
                SourceIp = "198.51.100.1", // Remote IP
                IsVpnConnected = false, // No VPN!
                Location = "JP", // Japan
                DeviceType = "mobile"
            }
        };

        var businessHours = await _policyEngine.EvaluateAsync("can_access_during_business_hours", context);
        Console.WriteLine($"Within business hours: {businessHours}");

        var officeLocation = await _policyEngine.EvaluateAsync("can_access_from_office", context);
        Console.WriteLine($"From approved location: {officeLocation}");

        var requiresMfa = await _policyEngine.EvaluateAsync("requires_mfa", context);
        Console.WriteLine($"Requires MFA: {requiresMfa} (remote + off-hours)\n");
    }

    /// <summary>
    /// Example 4: Budget approval limits by tenure
    /// </summary>
    public async Task BudgetApprovalAsync()
    {
        Console.WriteLine("=== Example 4: Budget Approval Authority ===\n");

        // Senior manager - can approve
        var seniorContext = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "mgr001",
                Roles = new List<string> { "manager" },
                IsManager = true,
                TenureMonths = 48 // 4 years
            },
            Environment = new EnvironmentAttributes
            {
                RequestTime = DateTime.Parse("2024-01-15 14:00:00") // Business hours
            }
        };

        var canApprove = await _policyEngine.EvaluateAsync("can_approve_budget", seniorContext);
        Console.WriteLine($"Senior Manager (4yr tenure): Can approve budget = {canApprove}");

        // Junior manager - cannot approve (insufficient tenure)
        var juniorContext = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "mgr002",
                Roles = new List<string> { "manager" },
                IsManager = true,
                TenureMonths = 6 // 6 months
            },
            Environment = new EnvironmentAttributes
            {
                RequestTime = DateTime.Parse("2024-01-15 14:00:00")
            }
        };

        var canApproveJunior = await _policyEngine.EvaluateAsync("can_approve_budget", juniorContext);
        Console.WriteLine($"Junior Manager (6mo tenure): Can approve budget = {canApproveJunior}\n");
    }

    /// <summary>
    /// Example 5: Resource deletion with grace period
    /// </summary>
    public async Task DeleteResourceAsync()
    {
        Console.WriteLine("=== Example 5: Resource Deletion Policies ===\n");

        // Owner trying to delete old resource
        var oldResourceContext = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "user111",
                Roles = new List<string> { "user" }
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "old_file",
                Owner = "user111",
                CreatedAt = DateTime.UtcNow.AddDays(-60) // Created 60 days ago
            }
        };

        var canDeleteOld = await _policyEngine.EvaluateAsync("can_delete_resource", oldResourceContext);
        Console.WriteLine($"Owner deleting old file (60 days): {canDeleteOld}");

        // Owner trying to delete new resource
        var newResourceContext = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "user111",
                Roles = new List<string> { "user" }
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "new_file",
                Owner = "user111",
                CreatedAt = DateTime.UtcNow.AddDays(-5) // Created 5 days ago
            }
        };

        var canDeleteNew = await _policyEngine.EvaluateAsync("can_delete_resource", newResourceContext);
        Console.WriteLine($"Owner deleting new file (5 days): {canDeleteNew}");

        // Admin can always delete
        var adminContext = new AccessContext
        {
            User = new UserAttributes
            {
                UserId = "admin",
                Roles = new List<string> { "admin" }
            },
            Resource = new ResourceAttributes
            {
                ResourceId = "any_file",
                Owner = "other_user",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        var canDeleteAdmin = await _policyEngine.EvaluateAsync("can_delete_resource", adminContext);
        Console.WriteLine($"Admin deleting any file: {canDeleteAdmin}\n");
    }
}

/// <summary>
/// Main example execution
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== ABAC (Attribute-Based Access Control) Example ===\n");

        var example = new AbacPolicyExample();

        await example.ManagerAccessFileAsync();
        await example.JuniorDeveloperAccessAsync();
        await example.RemoteOffHoursAccessAsync();
        await example.BudgetApprovalAsync();
        await example.DeleteResourceAsync();

        Console.WriteLine("✓ ABAC Examples completed");
    }
}
