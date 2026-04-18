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
    /// Get the total investment amount
    /// </summary>
    /// <response code="200">Returns the total investment amount</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetInvestmentSum")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InvestmentSumResponse>> GetInvestmentSum(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetInvestmentSumAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get information about the financial goal
    /// </summary>
    /// <response code="200">Returns information about the financial goal</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetFinancialGoal")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<FinancialGoalResponse>> GetFinancialGoal(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetFinancialGoalAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get information about active items
    /// </summary>
    /// <response code="200">Returns information about active items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetActiveStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveStatisticResponse>> GetActiveStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetActiveStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get information about the archive
    /// </summary>
    /// <response code="200">Returns information about the archive</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveStatisticResponse>> GetArchiveStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetArchiveStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get information about the inventory
    /// </summary>
    /// <response code="200">Returns information about the inventory</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetInventoryStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<InventoryStatisticResponse>> GetInventoryStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetInventoryStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get the total number of items
    /// </summary>
    /// <response code="200">Returns the total number of items</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetItemsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ItemsCountResponse>> GetItemsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _statisticsService.GetItemsCountAsync(user, cancellationToken));
    }

    /// <summary>
    /// Get the number of users per currency (admin)
    /// </summary>
    /// <response code="200">Returns the number of users per currency</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUsersCountByCurrency")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UsersCountByCurrencyResponse>> GetUsersCountByCurrency(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _statisticsService.GetUsersCountByCurrencyAsync(cancellationToken));
    }

    /// <summary>
    /// Get the total number of items for a game across all users (admin)
    /// </summary>
    /// <response code="200">Returns the total number of items for the game</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetItemsCountByGame")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ItemsCountResponse>> GetItemsCountByGame(
        [FromQuery] GetItemsCountByGameRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _statisticsService.GetItemsCountByGameAsync(request, cancellationToken));
    }

    #endregion GET
}