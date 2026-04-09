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
    /// Получение информации об активе
    /// </summary>
    /// <response code="200">Возвращает подробную информацию об активе</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Актива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveResponse>> GetActiveInfo(
        [FromQuery] GetActiveInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ActiveResponse response = await _activeService.GetActiveInfoAsync(user, request.Id, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Получение списка активов
    /// </summary>
    /// <response code="200">Возвращает список активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActives")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesResponse>> GetActives(
        [FromQuery] GetActivesRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<Active> actives = _activeService.GetActivesQuery(user, request.GroupId, request.GameId, request.Filter);
        actives = _activeService.ApplyOrder(actives, request.OrderName, request.IsAscending);

        return Ok(await _activeService.GetActivesResponseAsync(
            actives, request.PageNumber, request.PageSize, user, cancellationToken));
    }

    /// <summary>
    /// Получение статистики по выборке активов
    /// </summary>
    /// <response code="200">Возвращает статистику по выборке активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActivesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesStatisticResponse>> GetActivesStatistic(
        [FromQuery] GetActivesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ActivesStatisticResponse response =
            await _activeService.GetActivesStatisticAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Получение количества страниц активов
    /// </summary>
    /// <response code="200">Возвращает количество страниц активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActivesPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesPagesCountResponse>> GetActivesPagesCount(
        [FromQuery] GetActivesPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ActivesPagesCountResponse response =
            await _activeService.GetActivesPagesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Получение количества активов
    /// </summary>
    /// <response code="200">Возвращает количество активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActivesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActivesCountResponse>> GetActivesCount(
        [FromQuery] GetActivesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ActivesCountResponse response =
            await _activeService.GetActivesCountAsync(user, request, cancellationToken);

        return Ok(response);
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Добавление актива
    /// </summary>
    /// <response code="200">Актив успешно добавлен</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "PostActive")]
    public async Task<ActionResult> PostActive(
        PostActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeService.PostActiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Изменение актива
    /// </summary>
    /// <response code="200">Актив успешно изменён</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Актива с таким Id не существует, группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "PutActive")]
    public async Task<ActionResult> PutActive(
        PutActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeService.PutActiveAsync(user, request, cancellationToken);

        return Ok();
    }

    /// <summary>
    /// Продажа актива
    /// </summary>
    /// <response code="200">Актив успешно продан</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Актива с таким Id не существует, группы архива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "SoldActive")]
    public async Task<ActionResult> SoldActive(
        SoldActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeService.SoldActiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Удаление актива
    /// </summary>
    /// <response code="200">Актив успешно удалён</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Актива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteActive")]
    public async Task<ActionResult> DeleteActive(
        DeleteActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeService.DeleteActiveAsync(user, request.Id, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}