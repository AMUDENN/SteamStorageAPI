using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.ArchiveGroupService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ArchiveGroupsController : ControllerBase
{
    #region Fields

    private readonly IArchiveGroupService _archiveGroupService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public ArchiveGroupsController(
        IArchiveGroupService archiveGroupService,
        IContextUserService contextUserService)
    {
        _archiveGroupService = archiveGroupService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get information about a single archive group
    /// </summary>
    /// <response code="200">Returns detailed information about the archive group</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No archive group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupResponse>> GetArchiveGroupInfo(
        [FromQuery] GetArchiveGroupInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ArchiveGroup group = await _archiveGroupService.GetArchiveGroupsQuery(user)
                                 .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                             ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                 "No archive group with the given Id exists");

        return Ok(_archiveGroupService.GetArchiveGroupResponse(group));
    }

    /// <summary>
    /// Get the list of archive groups
    /// </summary>
    /// <response code="200">Returns the list of archive groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroups")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsResponse>> GetArchiveGroups(
        [FromQuery] GetArchiveGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<ArchiveGroup> groups = _archiveGroupService.GetArchiveGroupsQuery(user);

        IEnumerable<ArchiveGroupResponse> groupsResponse =
            await _archiveGroupService.GetArchiveGroupsResponseAsync(groups, cancellationToken);

        groupsResponse = _archiveGroupService.ApplyOrder(groupsResponse, request.OrderName, request.IsAscending);

        return Ok(new ArchiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
    }

    /// <summary>
    /// Get statistics for archive groups
    /// </summary>
    /// <response code="200">Returns statistics for archive groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupsStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsStatisticResponse>> GetArchiveGroupsStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _archiveGroupService.GetArchiveGroupsStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get the number of archive groups
    /// </summary>
    /// <response code="200">Returns the number of archive groups</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsCountResponse>> GetArchiveGroupsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(new ArchiveGroupsCountResponse(
            await _archiveGroupService.GetArchiveGroupsQuery(user).CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add a new archive group
    /// </summary>
    /// <response code="201">The group was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "PostArchiveGroup")]
    public async Task<ActionResult> PostArchiveGroup(
        PostArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveGroupService.PostArchiveGroupAsync(user, request, cancellationToken);

        return Created();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Update an archive group
    /// </summary>
    /// <response code="200">The group was successfully updated</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "PutArchiveGroup")]
    public async Task<ActionResult> PutArchiveGroup(
        PutArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveGroupService.PutArchiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete an archive group
    /// </summary>
    /// <response code="200">The group was successfully deleted</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteArchiveGroup")]
    public async Task<ActionResult> DeleteArchiveGroup(
        DeleteArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveGroupService.DeleteArchiveGroupAsync(user, request.GroupId, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}