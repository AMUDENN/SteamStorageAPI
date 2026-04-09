using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Extensions;

namespace SteamStorageAPI.Services.Domain.SkinService;

public class SkinService : ISkinService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public SkinService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    private async Task<List<int>> GetMarkedSkinsIdsAsync(
        User user,
        CancellationToken cancellationToken = default) =>
        await _context.Entry(user)
            .Collection(x => x.MarkedSkins)
            .Query()
            .AsNoTracking()
            .Select(x => x.SkinId)
            .ToListAsync(cancellationToken);

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

    public async Task<BaseSkinResponse> GetBaseSkinResponseAsync(
        Skin skin,
        CancellationToken cancellationToken = default) =>
        new(
            skin.Id,
            _steamApiUrlBuilder.GetSkinIconUrl(skin.SkinIconUrl),
            skin.Title,
            skin.MarketHashName,
            _steamApiUrlBuilder.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));

    public async Task<SkinResponse> GetSkinResponseAsync(
        Skin skin,
        User user,
        IEnumerable<int> markedSkinsIds,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        List<SkinsDynamic> dynamic30 = await _context.Entry(skin)
            .Collection(x => x.SkinsDynamics)
            .Query()
            .AsNoTracking()
            .Where(x => x.DateUpdate > DateTime.Now.AddDays(-30).Date)
            .OrderBy(x => x.DateUpdate)
            .ToListAsync(cancellationToken);

        List<SkinsDynamic> dynamic7 = dynamic30
            .Where(x => x.DateUpdate > DateTime.Now.AddDays(-7).Date)
            .ToList();

        double change7D = (double)(dynamic7.Count == 0
            ? 0
            : (skin.CurrentPrice - dynamic7.First().Price) / dynamic7.First().Price);

        double change30D = (double)(dynamic30.Count == 0
            ? 0
            : (skin.CurrentPrice - dynamic30.First().Price) / dynamic30.First().Price);

        return new(
            await GetBaseSkinResponseAsync(skin, cancellationToken),
            (decimal)((double)skin.CurrentPrice * rate),
            change7D,
            change30D,
            markedSkinsIds.Any(x => x == skin.Id));
    }

    public async Task<IEnumerable<SkinResponse>> GetSkinsResponseAsync(
        IQueryable<Skin> skins,
        User user,
        IEnumerable<int> markedSkinsIds,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        return await Task.WhenAll(skins
            .Include(x => x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30).Date))
            .AsEnumerable()
            .Select(async x => new SkinResponse(
                await GetBaseSkinResponseAsync(x, cancellationToken),
                (decimal)((double)x.CurrentPrice * rate),
                x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7).Date)
                    ? (double)((x.CurrentPrice
                                - x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7).Date)
                                    .OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7).Date)
                                   .OrderBy(y => y.DateUpdate).First().Price)
                    : 0,
                x.SkinsDynamics.Count != 0
                    ? (double)((x.CurrentPrice - x.SkinsDynamics.OrderBy(y => y.DateUpdate).First().Price)
                               / x.SkinsDynamics.OrderBy(y => y.DateUpdate).First().Price)
                    : 0,
                markedSkinsIds.Any(y => y == x.Id)))
        ).WaitAsync(cancellationToken);
    }

    public async Task<List<SkinDynamicResponse>> GetSkinDynamicsResponseAsync(
        Skin skin,
        User user,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        startDate = startDate.Date;
        endDate = endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        return await _context.Entry(skin)
            .Collection(x => x.SkinsDynamics)
            .Query()
            .AsNoTracking()
            .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
            .OrderBy(x => x.DateUpdate)
            .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, (decimal)((double)x.Price * rate)))
            .ToListAsync(cancellationToken);
    }

    public async Task<Skin> AddSkinAsync(
        int gameId,
        string marketHashName,
        string title,
        string skinIconUrl,
        CancellationToken cancellationToken = default)
    {
        Skin skin = new()
        {
            GameId = gameId,
            MarketHashName = marketHashName,
            Title = title,
            SkinIconUrl = skinIconUrl
        };

        await _context.Skins.AddAsync(skin, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return skin;
    }

    public async Task<SkinResponse> GetSkinInfoAsync(
        User user,
        GetSkinInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        Skin skin = await _context.Skins.AsNoTracking()
                        .Include(x => x.Game)
                        .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        List<int> markedSkinsIds = await GetMarkedSkinsIdsAsync(user, cancellationToken);

        return await GetSkinResponseAsync(skin, user, markedSkinsIds, cancellationToken);
    }

    public async Task<BaseSkinsResponse> GetBaseSkinsAsync(
        GetBaseSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Skin> skins = _context.Skins
            .AsNoTracking()
            .WhereMatchFilter(x => x.Title, request.Filter)
            .Take(20)
            .Include(x => x.Game);

        return new(
            await skins.CountAsync(cancellationToken),
            await Task.WhenAll(skins.AsEnumerable()
                    .Select(async x => await GetBaseSkinResponseAsync(x, cancellationToken)))
                .WaitAsync(cancellationToken));
    }

    public async Task<SkinsResponse> GetSkinsAsync(
        User user,
        GetSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
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

        return new(
            skinsCount,
            pagesCount == 0 ? 1 : pagesCount,
            await GetSkinsResponseAsync(skins, user, markedSkinsIds, cancellationToken));
    }

    public async Task<SkinDynamicStatsResponse> GetSkinDynamicsAsync(
        User user,
        GetSkinDynamicsRequest request,
        CancellationToken cancellationToken = default)
    {
        Skin skin = await _context.Skins.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        List<SkinDynamicResponse> dynamic =
            await GetSkinDynamicsResponseAsync(skin, user, request.StartDate, request.EndDate, cancellationToken);

        double changePeriod = dynamic.Count == 0
            ? 0
            : (double)((dynamic.Last().Price - dynamic.First().Price) / dynamic.First().Price);

        return new(changePeriod, dynamic);
    }

    public async Task<SkinPagesCountResponse> GetSkinPagesCountAsync(
        User user,
        GetSkinPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
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

        return new(pagesCount == 0 ? 1 : pagesCount);
    }

    public async Task<SteamSkinsCountResponse> GetSteamSkinsCountAsync(
        GetSteamSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        Game game = await _context.Games.AsNoTracking()
                        .FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        HttpClient client = _httpClientFactory.CreateClient();
        SteamSkinResponse response =
            await client.GetFromJsonAsync<SteamSkinResponse>(
                _steamApiUrlBuilder.GetMostPopularSkinUrl(game.SteamGameId), cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        return new(response.total_count);
    }

    public async Task<SavedSkinsCountResponse> GetSavedSkinsCountAsync(
        User user,
        GetSavedSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        List<int> markedSkinsIds = request.IsMarked is not null
            ? await GetMarkedSkinsIdsAsync(user, cancellationToken)
            : [];

        int count = await _context.Skins.AsNoTracking()
            .WhereMatchFilter(x => x.Title, request.Filter)
            .CountAsync(x => (request.GameId == null || x.GameId == request.GameId)
                             && (request.IsMarked == null
                                 || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)),
                cancellationToken);

        return new(count);
    }

    public async Task PostSkinAsync(
        PostSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        HttpClient client = _httpClientFactory.CreateClient();
        SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(
            _steamApiUrlBuilder.GetSkinInfoUrl(request.MarketHashName), cancellationToken);

        if (response is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        SkinResult result = response.results.First();

        if (await _context.Skins.AnyAsync(x => x.MarketHashName == result.asset_description.market_hash_name,
                cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "Скин с таким MarketHashName уже присутствует в базе");

        await AddSkinAsync(
            game.Id,
            result.asset_description.market_hash_name,
            result.name,
            result.asset_description.icon_url,
            cancellationToken);
    }

    public async Task SetMarkedSkinAsync(
        User user,
        SetMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        Skin skin = await _context.Skins.FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Предмета с таким Id не существует");

        MarkedSkin? markedSkin = await _context.MarkedSkins
            .Where(x => x.UserId == user.Id)
            .FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

        if (markedSkin is not null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Скин с таким Id уже добавлен в избранное");

        await _context.MarkedSkins.AddAsync(new() { SkinId = skin.Id, UserId = user.Id }, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteMarkedSkinAsync(
        User user,
        DeleteMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        MarkedSkin? markedSkin = await _context.MarkedSkins
            .Where(x => x.UserId == user.Id)
            .FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

        if (markedSkin is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "Скина с таким Id в таблице отмеченных скинов нет");

        _context.MarkedSkins.Remove(markedSkin);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}