using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SkinsController : ControllerBase
{
    #region Fields

    private readonly ISkinService _skinService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public SkinsController(ISkinService skinService, IContextUserService contextUserService)
    {
        _skinService = skinService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение информации об одном предмете
    /// </summary>
    /// <response code="200">Возвращает подробную информацию о предмете</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSkinInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinResponse>> GetSkinInfo(
        [FromQuery] GetSkinInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _skinService.GetSkinInfoAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Получение упрощённого списка предметов
    /// </summary>
    /// <response code="200">Возвращает упрощённый список предметов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetBaseSkins")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<BaseSkinsResponse>> GetBaseSkins(
        [FromQuery] GetBaseSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _skinService.GetBaseSkinsAsync(request, cancellationToken));
    }

    /// <summary>
    /// Получение списка предметов
    /// </summary>
    /// <response code="200">Возвращает список предметов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSkins")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinsResponse>> GetSkins(
        [FromQuery] GetSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _skinService.GetSkinsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Получение динамики стоимости предмета
    /// </summary>
    /// <response code="200">Возвращает динамику стоимости предмета</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSkinDynamics")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinDynamicStatsResponse>> GetSkinDynamics(
        [FromQuery] GetSkinDynamicsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _skinService.GetSkinDynamicsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Получение количества страниц предметов
    /// </summary>
    /// <response code="200">Возвращает количество страниц определённого размера</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSkinPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinPagesCountResponse>> GetSkinPagesCount(
        [FromQuery] GetSkinPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _skinService.GetSkinPagesCountAsync(user, request, cancellationToken));
    }


    /// <summary>
    /// Получение общего количества предметов в Steam
    /// </summary>
    /// <response code="200">Возвращает количество предметов в Steam</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Игры с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSteamSkinsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SteamSkinsCountResponse>> GetSteamSkinsCount(
        [FromQuery] GetSteamSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _skinService.GetSteamSkinsCountAsync(request, cancellationToken));
    }

    /// <summary>
    /// Получение количества сохранённых предметов
    /// </summary>
    /// <response code="200">Возвращает количество сохранённых предметов</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetSavedSkinsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SavedSkinsCountResponse>> GetSavedSkinsCount(
        [FromQuery] GetSavedSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _skinService.GetSavedSkinsCountAsync(user, request, cancellationToken));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Занесение одного предмета из Steam
    /// </summary>
    /// <response code="200">Предмет успешно добавлен</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Игры с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPost(Name = "PostSkin")]
    public async Task<ActionResult> PostSkin(
        PostSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        await _skinService.PostSkinAsync(request, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Добавление предмета в отмеченные
    /// </summary>
    /// <response code="200">Предмет отмечен</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPost(Name = "SetMarkedSkin")]
    public async Task<ActionResult> SetMarkedSkin(
        SetMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _skinService.SetMarkedSkinAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion POST

    #region DELETE

    /// <summary>
    /// Удаление отмеченного предмета
    /// </summary>
    /// <response code="200">Отметка предмета снята</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Предмета с таким Id в таблице отмеченных предметов нет или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteMarkedSkin")]
    public async Task<ActionResult> DeleteMarkedSkin(
        DeleteMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _skinService.DeleteMarkedSkinAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}