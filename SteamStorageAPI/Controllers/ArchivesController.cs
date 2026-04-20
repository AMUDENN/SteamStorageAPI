using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.ArchiveService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ArchivesController : ControllerBase
{
    #region Fields

    private readonly IArchiveService _archiveService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public ArchivesController(
        IArchiveService archiveService,
        IContextUserService contextUserService)
    {
        _archiveService = archiveService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get information about an archive item
    /// </summary>
    /// <response code="200">Returns detailed information about the archive item</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No archive item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveResponse>> GetArchiveInfo(
        [FromQuery] GetArchiveInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        Archive archive = await _archiveService.GetArchivesQuery(user, null, null, null)
                              .Include(x => x.Skin).ThenInclude(x => x.Game)
                              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                          ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                              "No archive item with the given Id exists");

        return Ok(_archiveService.GetArchiveResponse(archive));
    }

    /// <summary>
    /// Get the list of archive items
    /// </summary>
    /// <response code="200">Returns the list of archive items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchives")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesResponse>> GetArchives(
        [FromQuery] GetArchivesRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<Archive> archives = _archiveService.GetArchivesQuery(
            user, request.GroupId, request.GameId, request.Filter);

        archives = _archiveService.ApplyOrder(archives, request.OrderName, request.IsAscending);

        return Ok(await _archiveService.GetArchivesResponseAsync(
            archives, request.PageNumber, request.PageSize, cancellationToken));
    }

    /// <summary>
    /// Get statistics for the archive items selection
    /// </summary>
    /// <response code="200">Returns statistics for the archive items selection</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesStatisticResponse>> GetArchivesStatistic(
        [FromQuery] GetArchivesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<Archive> archives = _archiveService.GetArchivesQuery(
            user, request.GroupId, request.GameId, request.Filter);

        return Ok(new ArchivesStatisticResponse(
            await archives.SumAsync(x => x.Count, cancellationToken),
            await archives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken),
            await archives.SumAsync(x => x.SoldPrice * x.Count, cancellationToken)));
    }

    /// <summary>
    /// Get the number of archive pages
    /// </summary>
    /// <response code="200">Returns the number of archive pages</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesPagesCountResponse>> GetArchivesPagesCount(
        [FromQuery] GetArchivesPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        int count = await _archiveService
            .GetArchivesQuery(user, request.GroupId, request.GameId, request.Filter)
            .CountAsync(cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

        return Ok(new ArchivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
    }

    /// <summary>
    /// Get the number of archive items
    /// </summary>
    /// <response code="200">Returns the number of archive items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesCountResponse>> GetArchivesCount(
        [FromQuery] GetArchivesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(new ArchivesCountResponse(await _archiveService
            .GetArchivesQuery(user, request.GroupId, request.GameId, request.Filter)
            .CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add an archive item
    /// </summary>
    /// <response code="201">The archive item was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, no item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "PostArchive")]
    public async Task<ActionResult> PostArchive(
        PostArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveService.PostArchiveAsync(user, request, cancellationToken);

        return Created();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Update an archive item
    /// </summary>
    /// <response code="200">The archive item was successfully updated</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No archive item with the given Id exists, no group with the given Id exists, no item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "PutArchive")]
    public async Task<ActionResult> PutArchive(
        PutArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveService.PutArchiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete an archive item
    /// </summary>
    /// <response code="200">The archive item was successfully deleted</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No archive item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteArchive")]
    public async Task<ActionResult> DeleteArchive(
        DeleteArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _archiveService.DeleteArchiveAsync(user, request.Id, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}