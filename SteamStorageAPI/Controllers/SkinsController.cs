using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.Infrastructure.SkinService;
using SteamStorageAPI.Services.Infrastructure.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Extensions;
using SteamStorageAPI.Utilities.Steam;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SkinsController : ControllerBase
{
    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkinService _skinService;
    private readonly IUserService _userService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public SkinsController(
        IHttpClientFactory httpClientFactory,
        ISkinService skinService,
        IUserService userService,
        SteamStorageContext context)
    {
        _httpClientFactory = httpClientFactory;
        _skinService = skinService;
        _userService = userService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    private async Task<List<int>> GetMarkedSkinsIdsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        return await _context.Entry(user)
            .Collection(x => x.MarkedSkins)
            .Query()
            .AsNoTracking()
            .Select(x => x.SkinId)
            .ToListAsync(cancellationToken);
    }

    private IQueryable<Skin> ApplySkinOrder(
        IQueryable<Skin> skins,
        SkinOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return skins.OrderBy(x => x.Id);

        return orderName switch
        {
            SkinOrderName.Title => isAscending.Value
                ? skins.OrderBy(x => x.Title)
                : skins.OrderByDescending(x => x.Title),
            SkinOrderName.Price => isAscending.Value
                ? skins.OrderBy(x => x.CurrentPrice)
                : skins.OrderByDescending(x => x.CurrentPrice),
            SkinOrderName.Change7D => isAscending.Value
                ? skins.OrderBy(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                    ? (double)((x.CurrentPrice - x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7)).OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7)).OrderBy(y => y.DateUpdate).First().Price)
                    : 0)
                : skins.OrderByDescending(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                    ? (double)((x.CurrentPrice - x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7)).OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7)).OrderBy(y => y.DateUpdate).First().Price)
                    : 0),
            SkinOrderName.Change30D => isAscending.Value
                ? skins.OrderBy(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                    ? (double)((x.CurrentPrice - x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30)).OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30)).OrderBy(y => y.DateUpdate).First().Price)
                    : 0)
                : skins.OrderByDescending(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                    ? (double)((x.CurrentPrice - x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30)).OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30)).OrderBy(y => y.DateUpdate).First().Price)
                    : 0),
            _ => skins.OrderBy(x => x.Id)
        };
    }

    #endregion Methods

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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        Skin skin = await _context.Skins.AsNoTracking()
                        .Include(x => x.Game)
                        .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        List<int> markedSkinsIds = await GetMarkedSkinsIdsAsync(user, cancellationToken);

        return Ok(await _skinService.GetSkinResponseAsync(skin, user, markedSkinsIds, cancellationToken));
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
        IQueryable<Skin> skins = _context.Skins
            .AsNoTracking()
            .WhereMatchFilter(x => x.Title, request.Filter)
            .Take(20)
            .Include(x => x.Game);

        return Ok(new BaseSkinsResponse(
            await skins.CountAsync(cancellationToken),
            await Task.WhenAll(skins.AsEnumerable()
                    .Select(async x => await _skinService.GetBaseSkinResponseAsync(x, cancellationToken)))
                .WaitAsync(cancellationToken)));
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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        List<int> markedSkinsIds = await GetMarkedSkinsIdsAsync(user, cancellationToken);

        IQueryable<Skin> skins = _context.Skins
            .AsNoTracking()
            .Include(x => x.Game)
            .Where(x => (request.GameId == null || x.GameId == request.GameId)
                        && (request.IsMarked == null || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)))
            .WhereMatchFilter(x => x.Title, request.Filter);

        skins = ApplySkinOrder(skins, request.OrderName, request.IsAscending);

        int skinsCount = await skins.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)skinsCount / request.PageSize);

        skins = skins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

        return Ok(new SkinsResponse(
            skinsCount,
            pagesCount == 0 ? 1 : pagesCount,
            await _skinService.GetSkinsResponseAsync(skins, user, markedSkinsIds, cancellationToken)));
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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        Skin skin = await _context.Skins.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        List<SkinDynamicResponse> dynamic = await _skinService
            .GetSkinDynamicsResponseAsync(skin, user, request.StartDate, request.EndDate, cancellationToken);

        double changePeriod = dynamic.Count == 0
            ? 0
            : (double)((dynamic.Last().Price - dynamic.First().Price) / dynamic.First().Price);

        return Ok(new SkinDynamicStatsResponse(changePeriod, dynamic));
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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        List<int> markedSkinsIds = request.IsMarked is not null
            ? await GetMarkedSkinsIdsAsync(user, cancellationToken)
            : [];

        int count = await _context.Skins.AsNoTracking()
            .WhereMatchFilter(x => x.Title, request.Filter)
            .CountAsync(x => (request.GameId == null || x.GameId == request.GameId)
                             && (request.IsMarked == null
                                 || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)),
                cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

        return Ok(new SkinPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
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
        Game game = await _context.Games.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        HttpClient client = _httpClientFactory.CreateClient();
        SteamSkinResponse response =
            await client.GetFromJsonAsync<SteamSkinResponse>(
                SteamApi.GetMostPopularSkinUrl(game.SteamGameId), cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        return Ok(new SteamSkinsCountResponse(response.total_count));
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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        List<int> markedSkinsIds = request.IsMarked is not null
            ? await GetMarkedSkinsIdsAsync(user, cancellationToken)
            : [];

        int count = await _context.Skins.AsNoTracking()
            .WhereMatchFilter(x => x.Title, request.Filter)
            .CountAsync(x => (request.GameId == null || x.GameId == request.GameId)
                             && (request.IsMarked == null
                                 || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)),
                cancellationToken);

        return Ok(new SavedSkinsCountResponse(count));
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
        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        HttpClient client = _httpClientFactory.CreateClient();
        SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(
            SteamApi.GetSkinInfoUrl(request.MarketHashName), cancellationToken);

        if (response is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        SkinResult result = response.results.First();

        if (await _context.Skins.AnyAsync(x => x.MarketHashName == result.asset_description.market_hash_name,
                cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "Скин с таким MarketHashName уже присутствует в базе");

        await _skinService.AddSkinAsync(
            game.Id,
            result.asset_description.market_hash_name,
            result.name,
            result.asset_description.icon_url,
            cancellationToken);

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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        Skin skin = await _context.Skins.FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        MarkedSkin? markedSkin = await _context.MarkedSkins.Where(x => x.UserId == user.Id)
            .FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

        if (markedSkin is not null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Скин с таким Id уже добавлен в избранное");

        await _context.MarkedSkins.AddAsync(new() { SkinId = skin.Id, UserId = user.Id }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

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
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        MarkedSkin? markedSkin = await _context.MarkedSkins.Where(x => x.UserId == user.Id)
            .FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

        if (markedSkin is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Скина с таким Id в таблице отмеченных скинов нет");

        _context.MarkedSkins.Remove(markedSkin);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    #endregion DELETE
}