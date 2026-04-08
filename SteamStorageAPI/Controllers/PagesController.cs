using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Infrastructure.UserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class PagesController : ControllerBase
{
    #region Fields

    private readonly IUserService _userService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public PagesController(
        IUserService userService,
        SteamStorageContext context)
    {
        _userService = userService;
        _context = context;
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
    public async Task<ActionResult<PagesResponse>> GetPages(CancellationToken cancellationToken = default)
    {
        List<Page> pages = await _context.Pages.AsNoTracking().ToListAsync(cancellationToken);

        return Ok(new PagesResponse(pages.Count, pages.Select(x => new PageResponse(x.Id, x.Title))));
    }

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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        Page page = await _context.Pages.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == user.StartPageId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Страницы с таким Id не существует");

        return Ok(new PageResponse(page.Id, page.Title));
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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        if (!await _context.Pages.AnyAsync(x => x.Id == request.PageId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "Страницы с таким Id не существует");

        user.StartPageId = request.PageId;
        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    #endregion PUT
}