using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Domain.ActiveService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ActivesController : ControllerBase
{
    #region Fields

    private readonly IActiveService _activeService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public ActivesController(
        IActiveService activeService,
        IContextUserService contextUserService)
    {
        _activeService = activeService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get information about an active item
    /// </summary>
    /// <response code="200">Returns detailed information about the active item</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No active item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveResponse>> GetActiveInfo(
        [FromQuery] GetActiveInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ActiveResponse response = await _activeService.GetActiveInfoAsync(user, request.Id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get the list of active items
    /// </summary>
    /// <response code="200">Returns the list of active items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActives")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesResponse>> GetActives(
        [FromQuery] GetActivesRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<Active> actives =
            _activeService.GetActivesQuery(user, request.GroupId, request.GameId, request.Filter);
        actives = _activeService.ApplyOrder(actives, request.OrderName, request.IsAscending);

        return Ok(await _activeService.GetActivesResponseAsync(
            actives, request.PageNumber, request.PageSize, user, cancellationToken));
    }

    /// <summary>
    /// Get statistics for the active items selection
    /// </summary>
    /// <response code="200">Returns statistics for the active items selection</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActivesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesStatisticResponse>> GetActivesStatistic(
        [FromQuery] GetActivesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ActivesStatisticResponse response =
            await _activeService.GetActivesStatisticAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get the number of pages of active items
    /// </summary>
    /// <response code="200">Returns the number of pages of active items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActivesPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesPagesCountResponse>> GetActivesPagesCount(
        [FromQuery] GetActivesPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ActivesPagesCountResponse response =
            await _activeService.GetActivesPagesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get the number of active items
    /// </summary>
    /// <response code="200">Returns the number of active items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActivesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesCountResponse>> GetActivesCount(
        [FromQuery] GetActivesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        ActivesCountResponse response =
            await _activeService.GetActivesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add an active item
    /// </summary>
    /// <response code="201">The active item was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No group with the given Id exists, no item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "PostActive")]
    public async Task<ActionResult> PostActive(
        PostActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeService.PostActiveAsync(user, request, cancellationToken);

        return Created();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Update an active item
    /// </summary>
    /// <response code="200">The active item was successfully updated</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No active item with the given Id exists, no group with the given Id exists, no item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "PutActive")]
    public async Task<ActionResult> PutActive(
        PutActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeService.PutActiveAsync(user, request, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Sell an active item
    /// </summary>
    /// <response code="200">The active item was successfully sold</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No active item with the given Id exists, no archive group with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "SoldActive")]
    public async Task<ActionResult> SoldActive(
        SoldActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeService.SoldActiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete an active item
    /// </summary>
    /// <response code="200">The active item was successfully deleted</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No active item with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteActive")]
    public async Task<ActionResult> DeleteActive(
        DeleteActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _activeService.DeleteActiveAsync(user, request.Id, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}