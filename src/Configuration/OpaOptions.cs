#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Options for the optional Open Policy Agent (OPA) integration.
/// When <see cref="Enabled"/> is true the <see cref="PolicyEnforcementService"/>
/// delegates policy decisions to the OPA REST API instead of evaluating them
/// locally, allowing teams to manage and version policies externally using Rego.
/// </summary>
public sealed class OpaOptions sealed
{
    /// <summary>
    /// Enables OPA-backed policy evaluation.
    /// When false (default) the built-in evaluator is used.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Base URL of the OPA server, e.g. "http://opa:8181".
    /// </summary>
    public string BaseUrl { get; set; } = "http://localhost:8181";

    /// <summary>
    /// OPA policy path prefix, e.g. "authz".
    /// The full query URL becomes {BaseUrl}/v1/data/{PolicyPath}/{policyName}.
    /// </summary>
    public string PolicyPath { get; set; } = "authz";

    /// <summary>
    /// HTTP request timeout in seconds when calling OPA.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 5;

    /// <summary>
    /// When true, a failed or unreachable OPA call causes the policy evaluation
    /// to return <c>false</c> (deny).  When false, a failure falls back to the
    /// built-in evaluator so normal operation continues if OPA is temporarily
    /// unavailable.
    /// </summary>
    public bool FailClosedOnError { get; set; } = false;
}
