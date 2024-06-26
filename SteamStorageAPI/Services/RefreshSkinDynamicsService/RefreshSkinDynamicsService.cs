﻿using System.Diagnostics;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Utilities.Comparers;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.RefreshSkinDynamicsService;

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
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshSkinDynamicsService(
        ILogger<RefreshSkinDynamicsService> logger,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshSkinDynamicsAsync(
        CancellationToken cancellationToken = default)
    {
        //TODO: Performance Troubles
        Currency baseCurrency =
            await _context.Currencies.Include(x => x.CurrencyDynamics)
                .FirstOrDefaultAsync(x => x.Id == Currency.BASE_CURRENCY_ID, cancellationToken) ??
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "В базе данных отсутствует базовая валюта");

        List<Game> games = await _context.Games.ToListAsync(cancellationToken);

        foreach (Game game in games)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            int count = BASE_RESPONSE_COUNT;
            int start = START_RESPONSE;

            int answerCount = BASE_RESPONSE_COUNT;

            SteamSkinResponse? response =
                await client.GetFromJsonAsync<SteamSkinResponse>(
                    SteamApi.GetSkinsUrl(game.SteamGameId, baseCurrency.SteamCurrencyId, 1, 0),
                    cancellationToken);

            if (response is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "При получении данных с сервера Steam произошла ошибка");

            int totalCount = int.MaxValue;

            Random rnd = new();

            Stopwatch stopwatch = new();

            while (count == answerCount || start < totalCount)
            {
                try
                {
                    stopwatch.Reset();
                    stopwatch.Start();

                    response = await client.GetFromJsonAsync<SteamSkinResponse>(
                        SteamApi.GetSkinsUrl(game.SteamGameId, baseCurrency.SteamCurrencyId, count, start),
                        cancellationToken);

                    if (response is null)
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "При получении данных с сервера Steam произошла ошибка");

                    totalCount = response.total_count;

                    List<SkinsDynamic> skinsDynamics = [];

                    List<string> marketHashNames =
                        await _context.Skins.Select(x => x.MarketHashName).ToListAsync(cancellationToken);

                    List<Skin> skins = response.results
                        .Where(x => !marketHashNames.Contains(x.hash_name, new InvariantCaseStringComparer()))
                        .Select(x =>
                            new Skin
                            {
                                GameId = game.Id,
                                MarketHashName = x.hash_name,
                                Title = x.name,
                                SkinIconUrl = x.asset_description.icon_url
                            })
                        .ToList();
                    
                    if (skins.Count > 0)
                        _logger.LogInformation(
                            $"Добавлены скины:\n {string.Join("\n", skins.Select(x => x.MarketHashName))}");

                    await _context.Skins.AddRangeAsync(skins, cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);

                    foreach (SkinResult item in response.results)
                    {
                        Skin? skin = await _context.Skins.FirstOrDefaultAsync(x => x.MarketHashName == item.hash_name,
                            cancellationToken);

                        if (skin is null)
                            continue;

                        skinsDynamics.Add(new()
                        {
                            DateUpdate = DateTime.Now,
                            Price = Convert.ToDecimal(item.sell_price_text.Replace(baseCurrency.Mark, string.Empty),
                                new CultureInfo(baseCurrency.CultureInfo)),
                            SkinId = skin.Id
                        });
                    }

                    await _context.SkinsDynamics.AddRangeAsync(skinsDynamics, cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);

                    answerCount = response.results.Length;
                    start += response.results.Length;

                    count = BASE_RESPONSE_COUNT;

                    stopwatch.Stop();
                    _logger.LogInformation(
                        $"\n\tПроцесс выполнения загрузки скинов:\n\t\tЗагружено: {start} / {totalCount};\n\t\tВремя выполнения текущей итерации: {stopwatch.ElapsedMilliseconds} мс;\n");

                    await Task.Delay(rnd.Next(REFRESH_DELAY_MIN, REFRESH_DELAY_MAX), cancellationToken);
                }
                catch (Exception ex)
                {
                    count = rnd.Next(MIN_RESPONSE_COUNT, BASE_RESPONSE_COUNT);
                    start -= 1;
                    _logger.LogError($"Произошла ошибка во время обновления стоимости предметов: {ex.Message}");
                    await Task.Delay(rnd.Next(REFRESH_DELAY_ERROR_MIN, REFRESH_DELAY_ERROR_MAX), cancellationToken);
                }
            }
        }
    }

    #endregion Methods
}
