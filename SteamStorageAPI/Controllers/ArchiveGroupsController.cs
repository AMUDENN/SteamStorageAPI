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
    /// Получение информации об одной группе архива
    /// </summary>
    /// <response code="200">Возвращает подробную информацию о группе архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы архива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupResponse>> GetArchiveGroupInfo(
        [FromQuery] GetArchiveGroupInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        ArchiveGroup group = await _archiveGroupService.GetArchiveGroupsQuery(user)
                                 .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                             ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                 "Группы архива с таким Id не существует");

        return Ok(_archiveGroupService.GetArchiveGroupResponse(group));
    }

    /// <summary>
    /// Получение списка групп архива
    /// </summary>
    /// <response code="200">Возвращает список групп архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroups")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsResponse>> GetArchiveGroups(
        [FromQuery] GetArchiveGroupsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<ArchiveGroup> groups = _archiveGroupService.GetArchiveGroupsQuery(user);

        IEnumerable<ArchiveGroupResponse> groupsResponse =
            await _archiveGroupService.GetArchiveGroupsResponseAsync(groups, cancellationToken);

        groupsResponse = _archiveGroupService.ApplyOrder(groupsResponse, request.OrderName, request.IsAscending);

        return Ok(new ArchiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
    }

    /// <summary>
    /// Получение статистики групп архива
    /// </summary>
    /// <response code="200">Возвращает статистику групп архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupsStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsStatisticResponse>> GetArchiveGroupsStatistic(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _archiveGroupService.GetArchiveGroupsStatisticAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение количества групп архива
    /// </summary>
    /// <response code="200">Возвращает количество групп архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveGroupsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveGroupsCountResponse>> GetArchiveGroupsCount(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(new ArchiveGroupsCountResponse(
            await _archiveGroupService.GetArchiveGroupsQuery(user).CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Добавление новой группы архива
    /// </summary>
    /// <response code="200">Группа успешно добавлена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "PostArchiveGroup")]
    public async Task<ActionResult> PostArchiveGroup(
        PostArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveGroupService.PostArchiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Изменение группы архива
    /// </summary>
    /// <response code="200">Группа успешно изменена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "PutArchiveGroup")]
    public async Task<ActionResult> PutArchiveGroup(
        PutArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveGroupService.PutArchiveGroupAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Удаление группы архива
    /// </summary>
    /// <response code="200">Группа успешно удалена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteArchiveGroup")]
    public async Task<ActionResult> DeleteArchiveGroup(
        DeleteArchiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveGroupService.DeleteArchiveGroupAsync(user, request.GroupId, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}