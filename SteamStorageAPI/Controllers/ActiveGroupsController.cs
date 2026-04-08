using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.ActiveGroupService;
using SteamStorageAPI.Services.Infrastructure.UserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ActiveGroupsController : ControllerBase
{
    #region Fields

    private readonly IActiveGroupService _activeGroupService;
    private readonly IUserService _userService;

    #endregion Fields

    #region Constructor

    public ActiveGroupsController(
        IActiveGroupService activeGroupService,
        IUserService userService)
    {
        _activeGroupService = activeGroupService;
        _userService = userService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение информации об одной группе активов
    /// </summary>
    /// <response code="200">Возвращает подробную информацию о группе активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы активов с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupResponse>> GetActiveGroupInfo(
        [FromQuery] GetActiveGroupInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ActiveGroup group = await _activeGroupService.GetActiveGroupsQuery(user)
                                .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "Группы активов с таким Id не существует");

        return Ok(await _activeGroupService.GetActiveGroupResponseAsync(group, user, cancellationToken));
    }

    /// <summary>
    /// Получение списка групп активов
    /// </summary>
    /// <response code="200">Возвращает список групп активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroups")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsResponse>> GetActiveGroups(
        [FromQuery] GetActiveGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<ActiveGroup> groups = _activeGroupService.GetActiveGroupsQuery(user);

        IEnumerable<ActiveGroupResponse> groupsResponse =
            await _activeGroupService.GetActiveGroupsResponseAsync(groups, user, cancellationToken);

        groupsResponse = _activeGroupService.ApplyOrder(groupsResponse, request.OrderName, request.IsAscending);

        return Ok(new ActiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
    }

    /// <summary>
    /// Получение статистики групп активов
    /// </summary>
    /// <response code="200">Возвращает статистику групп активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupsStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsStatisticResponse>> GetActiveGroupsStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _activeGroupService.GetActiveGroupsStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение динамики стоимости группы активов
    /// </summary>
    /// <response code="200">Возвращает динамику группы активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupDynamics")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupDynamicStatsResponse>> GetActiveGroupDynamics(
        [FromQuery] GetActiveGroupDynamicRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _activeGroupService.GetActiveGroupDynamicsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Получение количества групп активов
    /// </summary>
    /// <response code="200">Возвращает количество групп активов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetActiveGroupsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ActiveGroupsCountResponse>> GetActiveGroupsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(new ActiveGroupsCountResponse(
            await _activeGroupService.GetActiveGroupsQuery(user).CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Добавление новой группы активов
    /// </summary>
    /// <response code="200">Группа успешно добавлена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "PostActiveGroup")]
    public async Task<ActionResult> PostActiveGroup(
        PostActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeGroupService.PostActiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Изменение группы активов
    /// </summary>
    /// <response code="200">Группа успешно изменена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "PutActiveGroup")]
    public async Task<ActionResult> PutActiveGroup(
        PutActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeGroupService.PutActiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Удаление группы активов
    /// </summary>
    /// <response code="200">Группа успешно удалена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteActiveGroup")]
    public async Task<ActionResult> DeleteActiveGroup(
        DeleteActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _activeGroupService.DeleteActiveGroupAsync(user, request.GroupId, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}