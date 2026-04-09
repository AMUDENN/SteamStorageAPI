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
    /// Получение списка инвентаря
    /// </summary>
    /// <response code="200">Возвращает список предметов в инвентаре</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetInventory")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoriesResponse>> GetInventory(
        [FromQuery] GetInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<Inventory> inventories = _inventoryService.GetInventoryQuery(
            user, request.GameId, request.Filter);

        inventories = _inventoryService.ApplyOrder(inventories, request.OrderName, request.IsAscending);

        return Ok(await _inventoryService.GetInventoriesResponseAsync(
            inventories, request.PageNumber, request.PageSize, user, cancellationToken));
    }

    /// <summary>
    /// Получение статистики по выборке предметов из инвентаря
    /// </summary>
    /// <response code="200">Возвращает статистику по выборке</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetInventoriesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoriesStatisticResponse>> GetInventoriesStatistic(
        [FromQuery] GetInventoriesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _inventoryService.GetInventoriesStatisticAsync(
            user, request.GameId, request.Filter, cancellationToken));
    }

    /// <summary>
    /// Получение количества страниц инвентаря
    /// </summary>
    /// <response code="200">Возвращает количество страниц инвентаря</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetInventoryPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoryPagesCountResponse>> GetInventoryPagesCount(
        [FromQuery] GetInventoryPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        InventoryPagesCountResponse response =
            await _inventoryService.GetInventoryPagesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Получение количества элементов в инвентаре
    /// </summary>
    /// <response code="200">Возвращает количество элементов в инвентаре</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSavedInventoriesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SavedInventoriesCountResponse>> GetSavedInventoriesCount(
        [FromQuery] GetSavedInventoriesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(new SavedInventoriesCountResponse(await _inventoryService
            .GetInventoryQuery(user, request.GameId, request.Filter)
            .CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Обновление инвентаря
    /// </summary>
    /// <response code="200">Инвентарь успешно обновлён</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Игры с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "RefreshInventory")]
    public async Task<ActionResult> RefreshInventory(
        RefreshInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _inventoryService.RefreshInventoryAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST
}