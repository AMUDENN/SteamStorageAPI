using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.ArchiveService;
using SteamStorageAPI.Services.Infrastructure.UserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class ArchivesController : ControllerBase
{
    #region Fields

    private readonly IArchiveService _archiveService;
    private readonly IUserService _userService;

    #endregion Fields

    #region Constructor

    public ArchivesController(
        IArchiveService archiveService,
        IUserService userService)
    {
        _archiveService = archiveService;
        _userService = userService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение информации об элементе архива
    /// </summary>
    /// <response code="200">Возвращает подробную информацию об элементе архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Элемента архива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchiveInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchiveResponse>> GetArchiveInfo(
        [FromQuery] GetArchiveInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        Archive archive = await _archiveService.GetArchivesQuery(user, null, null, null)
                              .Include(x => x.Skin).ThenInclude(x => x.Game)
                              .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                          ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                              "Элемента архива с таким Id не существует");

        return Ok(await _archiveService.GetArchiveResponseAsync(archive, cancellationToken));
    }

    /// <summary>
    /// Получение списка элементов архива
    /// </summary>
    /// <response code="200">Возвращает список элементов архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchives")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesResponse>> GetArchives(
        [FromQuery] GetArchivesRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<Archive> archives = _archiveService.GetArchivesQuery(
            user, request.GroupId, request.GameId, request.Filter);

        archives = _archiveService.ApplyOrder(archives, request.OrderName, request.IsAscending);

        return Ok(await _archiveService.GetArchivesResponseAsync(
            archives, request.PageNumber, request.PageSize, cancellationToken));
    }

    /// <summary>
    /// Получение статистики по выборке элементов архива
    /// </summary>
    /// <response code="200">Возвращает статистику по выборке элементов архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesStatistic")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesStatisticResponse>> GetArchivesStatistic(
        [FromQuery] GetArchivesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        IQueryable<Archive> archives = _archiveService.GetArchivesQuery(
            user, request.GroupId, request.GameId, request.Filter);

        return Ok(new ArchivesStatisticResponse(
            archives.Sum(x => x.Count),
            archives.Sum(x => x.BuyPrice * x.Count),
            archives.Sum(x => x.SoldPrice * x.Count)));
    }

    /// <summary>
    /// Получение количества страниц архива
    /// </summary>
    /// <response code="200">Возвращает количество страниц архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesPagesCountResponse>> GetArchivesPagesCount(
        [FromQuery] GetArchivesPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        int count = await _archiveService
            .GetArchivesQuery(user, request.GroupId, request.GameId, request.Filter)
            .CountAsync(cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

        return Ok(new ArchivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
    }

    /// <summary>
    /// Получение количества элементов архива
    /// </summary>
    /// <response code="200">Возвращает количество элементов архива</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetArchivesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<ArchivesCountResponse>> GetArchivesCount(
        [FromQuery] GetArchivesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(new ArchivesCountResponse(await _archiveService
            .GetArchivesQuery(user, request.GroupId, request.GameId, request.Filter)
            .CountAsync(cancellationToken)));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Добавление элемента архива
    /// </summary>
    /// <response code="200">Элемент архива успешно добавлен</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "PostArchive")]
    public async Task<ActionResult> PostArchive(
        PostArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveService.PostArchiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Изменение элемента архива
    /// </summary>
    /// <response code="200">Элемент архива успешно изменён</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Элемента архива с таким Id не существует, группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "PutArchive")]
    public async Task<ActionResult> PutArchive(
        PutArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveService.PutArchiveAsync(user, request, cancellationToken);

        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Удаление элемента архива
    /// </summary>
    /// <response code="200">Элемент архива успешно удалён</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Элемента архива с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteArchive")]
    public async Task<ActionResult> DeleteArchive(
        DeleteArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _archiveService.DeleteArchiveAsync(user, request.Id, cancellationToken);

        return Ok();
    }

    #endregion DELETE
}