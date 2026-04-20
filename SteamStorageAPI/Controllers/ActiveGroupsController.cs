using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.ActiveGroupService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ActiveGroupsController : ControllerBase
{
    #region Fields

    private readonly IActiveGroupService _activeGroupService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public ActiveGroupsController(
        IActiveGroupService activeGroupService,
        IContextUserService contextUserService)
    {
        _activeGroupService = activeGroupService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get information about a single active group
    /// </summary>
    /// <response code="200">Returns detailed information about the active group</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No active group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupResponse>> GetActiveGroupInfo(
        [FromQuery] GetActiveGroupInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ActiveGroup group = await _activeGroupService.GetActiveGroupsQuery(user)
                                .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "No active group with the given Id exists");

        return Ok(await _activeGroupService.GetActiveGroupResponseAsync(group, user, cancellationToken));
    }

    /// <summary>
    /// Get the list of active groups
    /// </summary>
    /// <response code="200">Returns the list of active groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroups")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsResponse>> GetActiveGroups(
        [FromQuery] GetActiveGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<ActiveGroup> groups = _activeGroupService.GetActiveGroupsQuery(user);

        IEnumerable<ActiveGroupResponse> groupsResponse =
            await _activeGroupService.GetActiveGroupsResponseAsync(groups, user, cancellationToken);

        groupsResponse = _activeGroupService.ApplyOrder(groupsResponse, request.OrderName, request.IsAscending);

        return Ok(new ActiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
    }

    /// <summary>
    /// Get statistics for active groups
    /// </summary>
    /// <response code="200">Returns statistics for active groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupsStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsStatisticResponse>> GetActiveGroupsStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _activeGroupService.GetActiveGroupsStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get the value dynamics of an active group
    /// </summary>
    /// <response code="200">Returns the dynamics of the active group</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupDynamics")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupDynamicStatsResponse>> GetActiveGroupDynamics(
        [FromQuery] GetActiveGroupDynamicRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _activeGroupService.GetActiveGroupDynamicsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Get the number of active groups
    /// </summary>
    /// <response code="200">Returns the number of active groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsCountResponse>> GetActiveGroupsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(new ActiveGroupsCountResponse(
            await _activeGroupService.GetActiveGroupsQuery(user).CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add a new active group
    /// </summary>
    /// <response code="201">The group was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "PostActiveGroup")]
    public async Task<ActionResult> PostActiveGroup(
        PostActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeGroupService.PostActiveGroupAsync(user, request, cancellationToken);

        return Created();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Update an active group
    /// </summary>
    /// <response code="200">The group was successfully updated</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "PutActiveGroup")]
    public async Task<ActionResult> PutActiveGroup(
        PutActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeGroupService.PutActiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete an active group
    /// </summary>
    /// <response code="200">The group was successfully deleted</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteActiveGroup")]
    public async Task<ActionResult> DeleteActiveGroup(
        DeleteActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeGroupService.DeleteActiveGroupAsync(user, request.GroupId, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}