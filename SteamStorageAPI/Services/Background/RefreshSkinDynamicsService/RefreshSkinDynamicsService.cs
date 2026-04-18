using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Background.RefreshSkinDynamicsService;

public class RefreshSkinDynamicsService : IRefreshSkinDynamicsService
{
    #region Constants

    private const int REFRESH_DELAY_MIN = 10000; // 10 sec
    private const int REFRESH_DELAY_MAX = 15000; // 15 sec

    private const int REFRESH_DELAY_ERROR_MIN = 100000; // 100 sec
    private const int REFRESH_DELAY_ERROR_MAX = 150000; // 150 sec

    private const int BASE_RESPONSE_COUNT = 200;
    private const int MIN_RESPONSE_COUNT = 100;
    private const int START_RESPONSE = 0;

    #endregion Constants

    #region Fields

    private readonly ILogger<RefreshSkinDynamicsService> _logger;
    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshSkinDynamicsService(
        ILogger<RefreshSkinDynamicsService> logger,
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _logger = logger;
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshSkinDynamicsAsync(
        CancellationToken cancellationToken = default)
    {
        Currency baseCurrency =
            await _context.Currencies
                .FirstOrDefaultAsync(x => x.Id == Currency.BASE_CURRENCY_ID, cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                "The base currency is missing from the database");

        List<Game> games = await _context.Games.ToListAsync(cancellationToken);

        foreach (Game game in games)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            int count = BASE_RESPONSE_COUNT;
            int start = START_RESPONSE;

            int answerCount = BASE_RESPONSE_COUNT;

            SteamSkinResponse? response =
                await client.GetFromJsonAsync<SteamSkinResponse>(
                    _steamApiUrlBuilder.GetSkinsUrl(game.SteamGameId, baseCurrency.SteamCurrencyId, 1, 0),
                    cancellationToken);

            if (response is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "An error occurred while retrieving data from the Steam server");

            int totalCount = int.MaxValue;

            // Load once per game, update incrementally when new skins are added
            HashSet<string> existingHashNames =
                [.. await _context.Skins.Select(x => x.MarketHashName).ToListAsync(cancellationToken)];

            Random rnd = new();

            Stopwatch stopwatch = new();

            while (count == answerCount || start < totalCount)
                try
                {
                    stopwatch.Reset();
                    stopwatch.Start();

                    response = await client.GetFromJsonAsync<SteamSkinResponse>(
                        _steamApiUrlBuilder.GetSkinsUrl(game.SteamGameId, baseCurrency.SteamCurrencyId, count, start),
                        cancellationToken);

                    if (response is null)
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "An error occurred while retrieving data from the Steam server");

                    totalCount = response.total_count;

                    SkinResult[] results = response.results ?? [];

                    List<Skin> skins = results
                        .Where(x => x.hash_name is not null && !existingHashNames.Contains(x.hash_name))
                        .Select(x => new Skin
                        {
                            GameId = game.Id,
                            MarketHashName = x.hash_name!,
                            Title = x.name ?? string.Empty,
                            SkinIconUrl = x.asset_description?.icon_url ?? string.Empty
                        })
                        .ToList();

                    if (skins.Count > 0)
                    {
                        _logger.LogInformation(
                            $"Skins added:\n {string.Join("\n", skins.Select(x => x.MarketHashName))}");

                        await _context.Skins.AddRangeAsync(skins, cancellationToken);
                        await _context.SaveChangesAsync(cancellationToken);

                        foreach (Skin s in skins)
                            existingHashNames.Add(s.MarketHashName);
                    }

                    // Batch load skins by hash_name instead of N separate queries
                    List<string> resultHashNames = results
                        .Where(x => x.hash_name is not null)
                        .Select(x => x.hash_name!)
                        .ToList();
                    Dictionary<string, int> skinIds = await _context.Skins
                        .Where(x => resultHashNames.Contains(x.MarketHashName))
                        .Select(x => new { x.MarketHashName, x.Id })
                        .ToDictionaryAsync(x => x.MarketHashName, x => x.Id, cancellationToken);

                    List<SkinsDynamic> skinsDynamics = results
                        .Where(x => x.hash_name is not null && skinIds.ContainsKey(x.hash_name))
                        .Select(x => new SkinsDynamic
                        {
                            DateUpdate = DateTime.Now,
                            Price = Convert.ToDecimal(
                                x.sell_price_text!.Replace(baseCurrency.Mark, string.Empty),
                                new CultureInfo(baseCurrency.CultureInfo)),
                            SkinId = skinIds[x.hash_name!]
                        })
                        .ToList();

                    await _context.SkinsDynamics.AddRangeAsync(skinsDynamics, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);

                    answerCount = results.Length;
                    start += results.Length;

                    count = BASE_RESPONSE_COUNT;

                    stopwatch.Stop();
                    _logger.LogInformation(
                        $"\n\tSkin loading progress:\n\t\tLoaded: {start} / {totalCount};\n\t\tCurrent iteration execution time: {stopwatch.ElapsedMilliseconds} ms;\n");

                    await Task.Delay(rnd.Next(REFRESH_DELAY_MIN, REFRESH_DELAY_MAX), cancellationToken);
                }
                catch (Exception ex)
                {
                    count = rnd.Next(MIN_RESPONSE_COUNT, BASE_RESPONSE_COUNT);
                    start -= 1;
                    _logger.LogError($"An error occurred while updating item prices: {ex.Message}");
                    await Task.Delay(rnd.Next(REFRESH_DELAY_ERROR_MIN, REFRESH_DELAY_ERROR_MAX), cancellationToken);
                }
        }
    }

    #endregion Methods
}