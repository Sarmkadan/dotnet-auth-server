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
/// Administrative REST API for managing user accounts.
/// Provides full CRUD, search, role assignment and account lock/unlock operations.
/// All routes are under <c>/api/users</c>.
/// </summary>
[ApiController]
[Route("api/users")]
[Produces("application/json")]
public sealed class UserManagementController : ControllerBase
{
    private readonly UserManagementService _userManagementService;
    private readonly ILogger<UserManagementController> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="UserManagementController"/>.
    /// </summary>
    public UserManagementController(
        UserManagementService userManagementService,
        ILogger<UserManagementController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    // -------------------------------------------------------------------------
    // List / Search
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns all registered users. Pass an optional <c>q</c> query parameter to
    /// filter by username, email or full name.
    /// </summary>
    /// <param name="q">Optional search term (substring match on username, email, full name).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserResponse>>), 200)]
    public async Task<IActionResult> GetUsersAsync(
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        try
        {
            var users = string.IsNullOrWhiteSpace(q)
                ? await _userManagementService.GetAllUsersAsync(cancellationToken)
                : await _userManagementService.SearchUsersAsync(q, cancellationToken);

            return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(users));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing users");
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Returns the user with the specified ID.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUserAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManagementService.GetUserByIdAsync(userId, cancellationToken);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(user));
        }
        catch (AuthServerException ex) when (ex.StatusCode == 404)
        {
            return NotFound(ApiResponse.ErrorResponse("user_not_found", ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Create
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="request">User creation parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> CreateUserAsync(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userManagementService.CreateUserAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetUserAsync), new { userId = user.UserId },
                ApiResponse<UserResponse>.SuccessResponse(user, "User created successfully"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Update
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates mutable fields on an existing user account.
    /// Only non-null properties in the request body are applied.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="request">Fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPut("{userId}")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ApiResponse<UserResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateUserAsync(
        string userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var user = await _userManagementService.UpdateUserAsync(userId, request, cancellationToken);
            return Ok(ApiResponse<UserResponse>.SuccessResponse(user, "User updated successfully"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Delete
    // -------------------------------------------------------------------------

    /// <summary>
    /// Permanently deletes a user account, revoking all tokens and sessions.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{userId}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> DeleteUserAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.DeleteUserAsync(userId, cancellationToken);
            return NoContent();
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Roles
    // -------------------------------------------------------------------------

    /// <summary>
    /// Assigns a role to a user. Idempotent.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="request">Role to assign.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId}/roles")]
    [Consumes("application/json")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> AssignRoleAsync(
        string userId,
        [FromBody] AssignRoleRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            await _userManagementService.AssignRoleAsync(userId, request.Role, cancellationToken);
            return Ok(ApiResponse.SuccessResponse($"Role '{request.Role}' assigned to user '{userId}'"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Removes a role from a user. Idempotent.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="role">Role name to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpDelete("{userId}/roles/{role}")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> RemoveRoleAsync(
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.RemoveRoleAsync(userId, role, cancellationToken);
            return Ok(ApiResponse.SuccessResponse($"Role '{role}' removed from user '{userId}'"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    // -------------------------------------------------------------------------
    // Lock / Unlock
    // -------------------------------------------------------------------------

    /// <summary>
    /// Locks a user account. Defaults to a 1-hour lockout when no duration is specified.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="minutes">Lockout duration in minutes (default: 60).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId}/lock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> LockUserAsync(
        string userId,
        [FromQuery] int minutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (minutes <= 0) minutes = 60;
            await _userManagementService.LockUserAsync(userId, TimeSpan.FromMinutes(minutes), cancellationToken);
            return Ok(ApiResponse.SuccessResponse($"User '{userId}' locked for {minutes} minute(s)"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }

    /// <summary>
    /// Unlocks a user account immediately, clearing any active lockout.
    /// </summary>
    /// <param name="userId">Target user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("{userId}/unlock")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UnlockUserAsync(string userId, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.UnlockUserAsync(userId, cancellationToken);
            return Ok(ApiResponse.SuccessResponse($"User '{userId}' unlocked successfully"));
        }
        catch (AuthServerException ex)
        {
            return StatusCode(ex.StatusCode, ApiResponse.ErrorResponse(ex.ErrorCode, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", userId);
            return StatusCode(500, ApiResponse.ErrorResponse(Constants.ErrorCodes.ServerError, ex.Message));
        }
    }
}
