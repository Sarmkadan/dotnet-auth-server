#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Controllers;

using Microsoft.AspNetCore.Mvc;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;

/// <summary>
/// RFC 7591 Dynamic Client Registration endpoint.
/// Allows OAuth2/OIDC clients to register themselves programmatically.
/// </summary>
[ApiController]
[Route("register")]
public sealed class ClientRegistrationController : ControllerBase
{
    private readonly DynamicClientRegistrationService _registrationService;
    private readonly ILogger<ClientRegistrationController> _logger;

    public ClientRegistrationController(
        DynamicClientRegistrationService registrationService,
        ILogger<ClientRegistrationController> logger)
    {
        _registrationService = registrationService;
        _logger = logger;
    }

    /// <summary>
    /// Register a new OAuth2 client (RFC 7591).
    /// </summary>
    /// <remarks>
    /// POST /register with a JSON body containing client metadata.
    /// Returns 201 Created with the registered metadata and, for confidential
    /// clients, a generated client_secret.
    ///
    /// Supported grant_types: authorization_code, client_credentials, implicit,
    /// refresh_token.
    ///
    /// Set token_endpoint_auth_method to "none" for public clients (SPAs, native
    /// apps).  Omit it or use "client_secret_basic" / "client_secret_post" for
    /// confidential clients.
    /// </remarks>
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    public async Task<IActionResult> RegisterClientAsync(
        [FromBody] ClientRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _registrationService.RegisterAsync(request, cancellationToken);

            // RFC 7591 §3.2.1 mandates HTTP 201 Created with Location header
            return CreatedAtAction(
                actionName: null,
                value: response);
        }
        catch (AuthServerException ex)
        {
            _logger.LogWarning(
                "Dynamic client registration failed: {ErrorCode} - {Message}",
                ex.ErrorCode, ex.Message);

            return StatusCode(ex.StatusCode, ex.ToErrorResponse());
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Collision on generated client_id (extremely unlikely)
            _logger.LogError(ex, "Client ID collision during dynamic registration");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "Failed to generate unique client identifier; please retry."
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled error in client registration endpoint");
            return StatusCode(500, new
            {
                error = Constants.ErrorCodes.ServerError,
                error_description = "An unexpected error occurred."
            });
        }
    }
}
