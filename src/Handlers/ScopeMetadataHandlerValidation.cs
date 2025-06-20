#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

/// <summary>
/// Provides validation helpers for <see cref="ScopeMetadata"/> instances.
/// Validates all public members including name, display name, description, and related scopes.
/// </summary>
public static class ScopeMetadataHandlerValidation
{
    /// <summary>
    /// Validates the specified <see cref="ScopeMetadata"/> instance.
    /// </summary>
    /// <param name="value">The scope metadata instance to validate.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ScopeMetadata? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate Name property
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Name cannot be null or whitespace.");
        }
        else if (value.Name.Length > 128)
        {
            problems.Add("Name cannot exceed 128 characters.");
        }

        // Validate DisplayName property
        if (string.IsNullOrWhiteSpace(value.DisplayName))
        {
            problems.Add("DisplayName cannot be null or whitespace.");
        }
        else if (value.DisplayName.Length > 256)
        {
            problems.Add("DisplayName cannot exceed 256 characters.");
        }

        // Validate Description property
        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("Description cannot be null or whitespace.");
        }
        else if (value.Description.Length > 2048)
        {
            problems.Add("Description cannot exceed 2048 characters.");
        }

        // Validate Icon property
        if (value.Icon is not null && value.Icon.Length > 512)
        {
            problems.Add("Icon cannot exceed 512 characters when set.");
        }

        // Validate RelatedScopes collection
        if (value.RelatedScopes is null)
        {
            problems.Add("RelatedScopes collection cannot be null.");
        }
        else
        {
            if (value.RelatedScopes.Count > 100)
            {
                problems.Add("RelatedScopes collection cannot contain more than 100 items.");
            }

            for (int i = 0; i < value.RelatedScopes.Count; i++)
            {
                var relatedScope = value.RelatedScopes[i];
                if (string.IsNullOrWhiteSpace(relatedScope))
                {
                    problems.Add($"RelatedScopes[{i}] cannot be null or whitespace.");
                }
                else if (relatedScope.Length > 128)
                {
                    problems.Add($"RelatedScopes[{i}] cannot exceed 128 characters.");
                }
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="ScopeMetadata"/> instance is valid.
    /// </summary>
    /// <param name="value">The scope metadata instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this ScopeMetadata? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="ScopeMetadata"/> instance is valid.
    /// </summary>
    /// <param name="value">The scope metadata instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this ScopeMetadata? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ScopeMetadata validation failed:{Environment.NewLine}" +
                string.Join(Environment.NewLine, problems.Select(p => $"  - {p}")));
        }
    }
}