using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Infrastructure.CurrencyService;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.Infrastructure.SkinService;

public class SkinService : ISkinService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public SkinService(
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<BaseSkinResponse> GetBaseSkinResponseAsync(
        Skin skin,
        CancellationToken cancellationToken = default) =>
        new(
            skin.Id,
            SteamApi.GetSkinIconUrl(skin.SkinIconUrl),
            skin.Title,
            skin.MarketHashName,
            SteamApi.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));

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

    #endregion Methods
}