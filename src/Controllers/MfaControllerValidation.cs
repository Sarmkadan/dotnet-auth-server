#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using DotnetAuthServer.Services;

namespace DotnetAuthServer.Controllers
{
    /// <summary>
    /// Validation helpers for <see cref="MfaController"/>.
    /// </summary>
    public static class MfaControllerValidation
    {
        /// <summary>
        /// Returns a read‑only list of human‑readable validation problems for the supplied
        /// <see cref="MfaController"/> instance.
        /// </summary>
        /// <param name="value">The controller instance to validate.</param>
        /// <returns>A list of validation error messages. Empty if the instance is valid.</returns>
        public static IReadOnlyList<string> Validate(this MfaController? value)
        {
            var problems = new List<string>();

            if (value is null)
            {
                problems.Add("MfaController instance is null.");
                return problems;
            }

            // Validate required injected services via reflection (they are private fields).
            // _totpService
            var totpField = typeof(MfaController).GetField("_totpService", BindingFlags.Instance | BindingFlags.NonPublic);
            var totpService = totpField?.GetValue(value) as TotpService;
            if (totpService is null)
                problems.Add("TotpService dependency is null.");

            // _logger
            var loggerField = typeof(MfaController).GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
            var logger = loggerField?.GetValue(value) as ILogger<MfaController>;
            if (logger is null)
                problems.Add("ILogger<MfaController> dependency is null.");

            // _userRepository is optional; no validation needed.

            return problems;
        }

        /// <summary>
        /// Determines whether the supplied <see cref="MfaController"/> instance is valid.
        /// </summary>
        /// <param name="value">The controller instance to check.</param>
        /// <returns>True if no validation problems were found; otherwise false.</returns>
        public static bool IsValid(this MfaController? value) => !value.Validate().Any();

        /// <summary>
        /// Ensures that the supplied <see cref="MfaController"/> instance is valid.
        /// Throws an <see cref="ArgumentException"/> if validation fails.
        /// </summary>
        /// <param name="value">The controller instance to validate.</param>
        public static void EnsureValid(this MfaController? value)
        {
            var problems = value.Validate();
            if (problems.Any())
            {
                var message = $"MfaController validation failed: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}
