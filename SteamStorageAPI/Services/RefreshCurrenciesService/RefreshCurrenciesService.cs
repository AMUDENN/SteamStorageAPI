﻿using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Price;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.RefreshCurrenciesService;

public class RefreshCurrenciesService : IRefreshCurrenciesService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkinService _skinService;
    private readonly SteamStorageContext _context;

    public RefreshCurrenciesService(
        IHttpClientFactory httpClientFactory,
        ISkinService skinService,
        SteamStorageContext context)
    {
        _httpClientFactory = httpClientFactory;
        _skinService = skinService;
        _context = context;
    }


    public async Task RefreshCurrencies(
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Currency> currencies = await _context.Currencies.ToListAsync(cancellationToken);

        Currency dollar =
            await _context.Currencies.FirstOrDefaultAsync(x => x.SteamCurrencyId == 1, cancellationToken) ??
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "В базе данных отсутствует базовая валюта (американский доллар)");

        Game game = await _context.Games.FirstOrDefaultAsync(cancellationToken) ?? throw new HttpResponseException(
            StatusCodes.Status400BadRequest,
            "В базе данных нет ни одной игры");

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
            await _skinService.AddSkin(game.Id, skinResult.market_hash_name, skinResult.name,
                skinResult.icon_url,
                cancellationToken);


        SteamPriceResponse? response = await client.GetFromJsonAsync<SteamPriceResponse>(
            SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName, dollar.SteamCurrencyId),
            cancellationToken);
        if (response?.lowest_price is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        double dollarPrice =
            Convert.ToDouble(response.lowest_price.Replace(dollar.Mark, string.Empty).Replace('.', ','));

        foreach (Currency currency in currencies)
        {
            response = await client.GetFromJsonAsync<SteamPriceResponse>(
                SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName,
                    currency.SteamCurrencyId), cancellationToken);

            if (response is null)
                continue;

            double price = Convert.ToDouble(response.lowest_price.Replace(currency.Mark, string.Empty)
                .Replace('.', ','));

            _context.CurrencyDynamics.Add(new()
            {
                CurrencyId = currency.Id,
                DateUpdate = DateTime.Now,
                Price = price / dollarPrice
            });

            await Task.Delay(2000, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}