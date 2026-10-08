#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/// <summary>
/// Registration helpers for <see cref="MfaOptions"/>.
/// </summary>
public static class MfaOptionsServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="MfaOptions"/> from the <c>DotnetAuthServer:Mfa</c> section, validates it
    /// with <see cref="MfaOptionsValidator"/> when the host starts, and registers the plain
    /// <see cref="MfaOptions"/> singleton for injection.
    /// </summary>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The application configuration containing the section.</param>
    /// <returns>The same service collection, for chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> or <paramref name="configuration"/> is null.</exception>
    public static IServiceCollection AddMfaOptions(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<MfaOptions>()
            .Bind(configuration.GetSection(MfaOptions.SectionKey))
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<MfaOptions>, MfaOptionsValidator>();
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<MfaOptions>>().Value);

        return services;
    }
}
