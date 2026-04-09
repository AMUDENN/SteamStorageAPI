using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.StatisticsService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class StatisticsController : ControllerBase
{
    #region Fields

    private readonly IStatisticsService _statisticsService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public StatisticsController(
        IStatisticsService statisticsService,
        IContextUserService contextUserService)
    {
        _statisticsService = statisticsService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение суммы инвестиций
    /// </summary>
    /// <response code="200">Возвращает сумму инвестиций</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetInvestmentSum")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InvestmentSumResponse>> GetInvestmentSum(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetInvestmentSumAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение информации о финансовой цели
    /// </summary>
    /// <response code="200">Возвращает информацию о финансовой цели</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetFinancialGoal")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<FinancialGoalResponse>> GetFinancialGoal(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetFinancialGoalAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение информации об активах
    /// </summary>
    /// <response code="200">Возвращает информацию об активах</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveStatisticResponse>> GetActiveStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetActiveStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение информации об архиве
    /// </summary>
    /// <response code="200">Возвращает информацию об архиве</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveStatisticResponse>> GetArchiveStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetArchiveStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение информации об инвентаре
    /// </summary>
    /// <response code="200">Возвращает информацию об инвентаре</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetInventoryStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoryStatisticResponse>> GetInventoryStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetInventoryStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение общего количества предметов
    /// </summary>
    /// <response code="200">Возвращает общее количество предметов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetItemsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ItemsCountResponse>> GetItemsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _statisticsService.GetItemsCountAsync(user, cancellationToken));
    }

    #endregion GET
}