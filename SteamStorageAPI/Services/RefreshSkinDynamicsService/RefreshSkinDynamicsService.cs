using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Services.RefreshSkinDynamicsService;

public class RefreshSkinDynamicsService : IRefreshSkinDynamicsService
{
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
        Currency dollar =
            await _context.Currencies.Include(x => x.CurrencyDynamics)
                .FirstOrDefaultAsync(x => x.SteamCurrencyId == 1, cancellationToken) ??
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "В базе данных отсутствует базовая валюта (американский доллар)");

        List<Game> games = await _context.Games.ToListAsync(cancellationToken);

        foreach (Game game in games)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            int count = 200;
            int start = 0;

            int answerCount = 200;

            SteamSkinResponse? response =
                await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetSkinsUrl(game.SteamGameId, 1, 0),
                    cancellationToken);

            if (response is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "При получении данных с сервера Steam произошла ошибка");

            int totalCount = response.total_count;

            Random rnd = new();

            while (count == answerCount || start < totalCount)
            {
                try
                {
                    _logger.LogInformation(
                        $"Процесс выполнения загрузки скинов:\nЗагружено: {start} / {totalCount}");

                    response = await client.GetFromJsonAsync<SteamSkinResponse>(
                        SteamApi.GetSkinsUrl(game.SteamGameId, count, start), cancellationToken);

                    if (response is null)
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "При получении данных с сервера Steam произошла ошибка");

                    List<Skin> skins = [];

                    List<SkinsDynamic> skinsDynamics = [];

                    foreach (SkinResult item in response.results)
                    {
                        if (await _context.Skins.AnyAsync(x => x.MarketHashName == item.hash_name,
                                cancellationToken))
                            continue;
                        skins.Add(new()
                        {
                            GameId = game.Id,
                            MarketHashName = item.hash_name,
                            Title = item.name,
                            SkinIconUrl = item.asset_description.icon_url
                        });
                    }

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
                            Price = Convert.ToDecimal(item.sell_price_text.Replace(dollar.Mark, string.Empty)
                                .Replace('.', ',')),
                            SkinId = skin.Id
                        });
                    }

                    await _context.SkinsDynamics.AddRangeAsync(skinsDynamics, cancellationToken);

                    await _context.SaveChangesAsync(cancellationToken);

                    answerCount = response.results.Length;
                    start += response.results.Length;

                    count = 200;

                    await Task.Delay(rnd.Next(10000, 15000), cancellationToken);
                }
                catch (Exception ex)
                {
                    count = rnd.Next(100, 199);
                    start -= 1;
                    _logger.LogError(ex.Message);
                    await Task.Delay(rnd.Next(100000, 150000), cancellationToken);
                }
            }
        }
    }

    #endregion Methods
}
