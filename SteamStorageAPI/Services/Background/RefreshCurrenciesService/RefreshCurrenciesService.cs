using System.Globalization;
using System.Net;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Price;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;

namespace SteamStorageAPI.Services.Background.RefreshCurrenciesService;

public class RefreshCurrenciesService : IRefreshCurrenciesService
{
    #region Constants

    private const int REFRESH_DELAY = 2_000;

    #endregion Constants

    #region Fields

    private readonly ILogger<RefreshCurrenciesService> _logger;
    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkinService _skinService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshCurrenciesService(
        ILogger<RefreshCurrenciesService> logger,
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        ISkinService skinService,
        SteamStorageContext context)
    {
        _logger = logger;
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _skinService = skinService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        List<Currency> currencies = await _context.Currencies.ToListAsync(cancellationToken);

        DateTime todayUtc = DateTime.UtcNow.Date;
        int updatedToday = await _context.CurrencyDynamics
            .CountAsync(x => x.DateUpdate >= todayUtc && x.DateUpdate < todayUtc.AddDays(1), cancellationToken);

        if (updatedToday >= currencies.Count)
        {
            _logger.LogInformation(
                "Currency exchange rates already updated today ({Count} currencies)", currencies.Count);
            return;
        }

        Currency baseCurrency = currencies.FirstOrDefault(x => x.IsBase)
                                ?? throw new InvalidOperationException("Base currency is not set in database");

        Game game = await _context.Games
                        .FirstOrDefaultAsync(x => x.IsBase, cancellationToken)
                    ?? throw new InvalidOperationException("Base game is not set in database");

        using HttpClient client = _httpClientFactory.CreateClient();

        SteamSkinResponse? skinResponse = await client.GetFromJsonAsync<SteamSkinResponse>(
            _steamApiUrlBuilder.GetMostPopularSkinUrl(game.SteamGameId, baseCurrency.SteamCurrencyId), cancellationToken);

        AssetDescription? skinResult = skinResponse?.results?.FirstOrDefault()?.asset_description;
        if (skinResult?.market_hash_name is null)
            throw new InvalidOperationException("Could not retrieve reference skin from Steam");

        Skin? skin = await _context.Skins
            .FirstOrDefaultAsync(x => x.MarketHashName == skinResult.market_hash_name, cancellationToken);

        if (skin is null)
            await _skinService.AddSkinAsync(
                game.Id, skinResult.market_hash_name, skinResult.name!, skinResult.icon_url!, cancellationToken);

        decimal? baseCurrencyPrice = await FetchPriceAsync(
            client, game.SteamGameId, skinResult.market_hash_name, baseCurrency, cancellationToken);

        if (baseCurrencyPrice is null or <= 0)
            throw new InvalidOperationException(
                $"Could not get base currency price for '{skinResult.market_hash_name}'");

        _logger.LogInformation(
            "Reference skin: {Skin}, base price: {Price} {Currency}",
            skinResult.market_hash_name, baseCurrencyPrice, baseCurrency.Title);

        int savedCount = 0;
        foreach (Currency currency in currencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (await _context.CurrencyDynamics.AnyAsync(
                    x => x.CurrencyId == currency.Id
                         && x.DateUpdate >= todayUtc
                         && x.DateUpdate < todayUtc.AddDays(1),
                    cancellationToken))
            {
                _logger.LogDebug("Currency {Title} already updated today, skipping", currency.Title);
                continue;
            }

            decimal? price = await FetchPriceAsync(
                client, game.SteamGameId, skinResult.market_hash_name, currency, cancellationToken);

            if (price is null or <= 0)
            {
                _logger.LogWarning("Could not get price for currency {Title}, skipping", currency.Title);
                continue;
            }

            _context.CurrencyDynamics.Add(new CurrencyDynamic
            {
                CurrencyId = currency.Id,
                DateUpdate = DateTime.UtcNow,
                Price = price.Value / baseCurrencyPrice.Value
            });
            savedCount++;

            _logger.LogInformation(
                "Currency {Title}: rate = {Rate:F6}", currency.Title, price.Value / baseCurrencyPrice.Value);

            await Task.Delay(REFRESH_DELAY, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Updated {Count}/{Total} currency exchange rates", savedCount, currencies.Count);
    }

    private async Task<decimal?> FetchPriceAsync(
        HttpClient client,
        int appId,
        string marketHashName,
        Currency currency,
        CancellationToken cancellationToken)
    {
        string url = _steamApiUrlBuilder.GetPriceOverviewUrl(appId, marketHashName, currency.SteamCurrencyId);

        try
        {
            using HttpResponseMessage httpResponse = await client.GetAsync(url, cancellationToken);

            if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                _logger.LogWarning("Steam rate-limited price request for {Title}", currency.Title);
                return null;
            }

            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Steam returned {StatusCode} for price request ({Title})",
                    (int)httpResponse.StatusCode, currency.Title);
                return null;
            }

            SteamPriceResponse? response =
                await httpResponse.Content.ReadFromJsonAsync<SteamPriceResponse>(cancellationToken);

            if (response?.lowest_price is null)
            {
                _logger.LogWarning("Steam returned no lowest_price for currency {Title}", currency.Title);
                return null;
            }

            decimal? price = ParsePrice(response.lowest_price, currency.Mark, new CultureInfo(currency.CultureInfo));

            if (price is null)
                _logger.LogWarning(
                    "Failed to parse '{RawPrice}' for {Title} (mark='{Mark}', culture='{Culture}')",
                    response.lowest_price, currency.Title, currency.Mark, currency.CultureInfo);

            return price;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Failed to fetch price for currency {Title}: {Message}", currency.Title, ex.Message);
            return null;
        }
    }

    private static decimal? ParsePrice(string priceText, string currencyMark, CultureInfo culture)
    {
        string cleaned = priceText.Replace(currencyMark, string.Empty).Trim();

        if (decimal.TryParse(cleaned, NumberStyles.Any, culture, out decimal result) && result > 0)
            return result;

        // Fallback: keep only digits and the culture's decimal/group separators
        string decSep = culture.NumberFormat.NumberDecimalSeparator;
        string grpSep = culture.NumberFormat.NumberGroupSeparator;
        string numericOnly = new(
            cleaned.Where(c => char.IsDigit(c) || c.ToString() == decSep || c.ToString() == grpSep).ToArray());

        if (decimal.TryParse(numericOnly, NumberStyles.Any, culture, out result) && result > 0)
            return result;

        return null;
    }

    #endregion Methods
}