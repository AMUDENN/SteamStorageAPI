using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.UserService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UsersController : ControllerBase
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public UsersController(IUserService userService, IContextUserService contextUserService)
    {
        _userService = userService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get the list of users
    /// </summary>
    /// <response code="200">Returns the list of users</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUsers")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UsersResponse>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _userService.GetUsersAsync(request, cancellationToken));
    }

    /// <summary>
    /// Get the number of users
    /// </summary>
    /// <response code="200">Returns the number of users</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUsersCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UsersCountResponse>> GetUsersCount(
        CancellationToken cancellationToken = default)
    {
        return Ok(new UsersCountResponse(await _userService.GetUsersCountAsync(cancellationToken)));
    }

    /// <summary>
    /// Get information about a user
    /// </summary>
    /// <response code="200">Returns information about the user</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUserInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UserResponse>> GetUserInfo(
        [FromQuery] GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _userService.GetUserInfoAsync(request, cancellationToken));
    }

    /// <summary>
    /// Get information about the current user
    /// </summary>
    /// <response code="200">Returns information about the current user</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentUserInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UserResponse>> GetCurrentUserInfo(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _userService.GetCurrentUserInfoAsync(user, cancellationToken));
    }


    /// <summary>
    /// Get the financial goal of the current user
    /// </summary>
    /// <response code="200">Returns the financial goal of the current user</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentUserGoalSum")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<GoalSumResponse>> GetCurrentUserGoalSum(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(_userService.GetCurrentUserGoalSum(user));
    }


    /// <summary>
    /// Get information about access to the admin panel
    /// </summary>
    /// <response code="200">Returns information about access to the admin panel</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetHasAccessToAdminPanel")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<HasAccessToAdminPanelResponse>> GetHasAccessToAdminPanel(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _userService.GetHasAccessToAdminPanelAsync(user, cancellationToken));
    }

    #endregion GET

    #region PUT

    /// <summary>
    /// Set the financial goal
    /// </summary>
    /// <response code="200">The financial goal was successfully set</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "PutGoalSum")]
    public async Task<ActionResult> PutGoalSum(
        PutGoalSumRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _userService.PutGoalSumAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete the current user
    /// </summary>
    /// <response code="200">The user was successfully deleted</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteUser")]
    public async Task<ActionResult> DeleteUser(CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _userService.DeleteUserAsync(user, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}