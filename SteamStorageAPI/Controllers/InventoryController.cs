using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.InventoryService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class InventoryController : ControllerBase
{
    #region Fields

    private readonly IInventoryService _inventoryService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public InventoryController(
        IInventoryService inventoryService,
        IContextUserService contextUserService)
    {
        _inventoryService = inventoryService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get the inventory list
    /// </summary>
    /// <response code="200">Returns the list of items in the inventory</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetInventory")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoriesResponse>> GetInventory(
        [FromQuery] GetInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        IQueryable<Inventory> inventories = _inventoryService.GetInventoryQuery(
            user, request.GameId, request.Filter);

        inventories = _inventoryService.ApplyOrder(inventories, request.OrderName, request.IsAscending);

        return Ok(await _inventoryService.GetInventoriesResponseAsync(
            inventories, request.PageNumber, request.PageSize, user, cancellationToken));
    }

    /// <summary>
    /// Get statistics for the selected inventory items
    /// </summary>
    /// <response code="200">Returns statistics for the selection</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetInventoriesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoriesStatisticResponse>> GetInventoriesStatistic(
        [FromQuery] GetInventoriesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _inventoryService.GetInventoriesStatisticAsync(
            user, request.GameId, request.Filter, cancellationToken));
    }

    /// <summary>
    /// Get the number of inventory pages
    /// </summary>
    /// <response code="200">Returns the number of inventory pages</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetInventoryPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoryPagesCountResponse>> GetInventoryPagesCount(
        [FromQuery] GetInventoryPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        InventoryPagesCountResponse response =
            await _inventoryService.GetInventoryPagesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Get the number of items in the inventory
    /// </summary>
    /// <response code="200">Returns the number of items in the inventory</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSavedInventoriesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SavedInventoriesCountResponse>> GetSavedInventoriesCount(
        [FromQuery] GetSavedInventoriesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(new SavedInventoriesCountResponse(await _inventoryService
            .GetInventoryQuery(user, request.GameId, request.Filter)
            .CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Refresh the inventory
    /// </summary>
    /// <response code="200">The inventory was successfully refreshed</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "RefreshInventory")]
    public async Task<ActionResult> RefreshInventory(
        RefreshInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _inventoryService.RefreshInventoryAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST
}