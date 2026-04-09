using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.PageService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class PagesController : ControllerBase
{
    #region Fields

    private readonly IPageService _pageService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public PagesController(IPageService pageService, IContextUserService contextUserService)
    {
        _pageService = pageService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение списка страниц
    /// </summary>
    /// <response code="200">Возвращает список страниц</response>
    /// <response code="499">Операция отменена</response>
    [HttpGet(Name = "GetPages")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<PagesResponse>> GetPages(
        CancellationToken cancellationToken = default) =>
        Ok(await _pageService.GetPagesAsync(cancellationToken));

    /// <summary>
    /// Получение текущей стартовой страницы пользователя
    /// </summary>
    /// <response code="200">Возвращает текущую стартовую страницу пользователя</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Страницы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentStartPage")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<PageResponse>> GetCurrentStartPage(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _pageService.GetCurrentStartPageAsync(user, cancellationToken));
    }

    #endregion GET

    #region PUT

    /// <summary>
    /// Установка стартовой страницы
    /// </summary>
    /// <response code="200">Стартовая страница успешно установлена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Страницы с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "SetStartPage")]
    public async Task<ActionResult> SetStartPage(
        SetPageRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _pageService.SetStartPageAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT
}