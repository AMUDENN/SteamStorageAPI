using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Price;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.RefreshCurrenciesService;

public class RefreshCurrenciesService : IRefreshCurrenciesService
{
    #region Constants

    private const int REFRESH_DELAY = 2000; // 2 sec

    #endregion Constants

    #region Fields

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkinService _skinService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshCurrenciesService(
        IHttpClientFactory httpClientFactory,
        ISkinService skinService,
        SteamStorageContext context)
    {
        _httpClientFactory = httpClientFactory;
        _skinService = skinService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        if (await _context.CurrencyDynamics.CountAsync(x => x.DateUpdate.Date == DateTime.Today, cancellationToken) ==
            await _context.Currencies.CountAsync(cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "Сегодня уже было выполнено обновление курса валют!");

        IQueryable<Currency> currencies = _context.Currencies.AsQueryable();

        Currency baseCurrency =
            await currencies.FirstOrDefaultAsync(x => x.Id == Currency.BASE_CURRENCY_ID, cancellationToken) ??
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "В базе данных отсутствует базовая валюта");

        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == Game.BASE_GAME_ID, cancellationToken) ??
                    throw new HttpResponseException(StatusCodes.Status400BadRequest, "В базе данных нет ни одной игры");

        HttpClient client = _httpClientFactory.CreateClient();

        SteamSkinResponse? skinResponse =
            await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetMostPopularSkinUrl(game.SteamGameId),
                cancellationToken);

        AssetDescription? skinResult = skinResponse?.results.FirstOrDefault()?.asset_description;

        if (skinResult is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        Skin skin =
            await _context.Skins.Include(skin => skin.Game)
                .FirstOrDefaultAsync(x => x.MarketHashName == skinResult.market_hash_name, cancellationToken) ??
            await _skinService.AddSkinAsync(game.Id, 
                skinResult.market_hash_name, 
                skinResult.name,
                skinResult.icon_url,
                cancellationToken);

        SteamPriceResponse? response = await client.GetFromJsonAsync<SteamPriceResponse>(
            SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId,
                skin.MarketHashName, 
                baseCurrency.SteamCurrencyId), 
            cancellationToken);
        if (response?.lowest_price is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        double baseCurrencyPrice =
            Convert.ToDouble(response.lowest_price.Replace(baseCurrency.Mark, string.Empty).Replace('.', ','));

        foreach (Currency currency in currencies)
        {
            if (await _context.CurrencyDynamics.AnyAsync(
                    x => x.CurrencyId == currency.Id && x.DateUpdate.Date == DateTime.Today, cancellationToken))
                continue;

            response = await client.GetFromJsonAsync<SteamPriceResponse>(
                SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId, 
                    skin.MarketHashName,
                    currency.SteamCurrencyId),
                cancellationToken);

            if (response is null)
                continue;

            double price = Convert.ToDouble(response.lowest_price.Replace(currency.Mark, string.Empty)
                .Replace('.', ','));

            _context.CurrencyDynamics.Add(new()
            {
                CurrencyId = currency.Id,
                DateUpdate = DateTime.Now,
                Price = price / baseCurrencyPrice
            });

            await Task.Delay(REFRESH_DELAY, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}
