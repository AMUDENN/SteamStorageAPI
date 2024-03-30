using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Services.SkinService
{
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
            CancellationToken cancellationToken = default)
        {
            return new(
                skin.Id,
                SteamApi.GetSkinIconUrl(skin.SkinIconUrl),
                skin.Title,
                skin.MarketHashName,
                SteamApi.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));
        }

        public async Task<decimal> GetCurrentPriceAsync(
            Skin skin,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            IOrderedQueryable<SkinsDynamic> dynamics = _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .AsNoTracking()
                .OrderBy(x => x.DateUpdate);

            return dynamics.Any() ? (decimal)((double)dynamics.Last().Price * currencyExchangeRate) : 0;
        }

        public async Task<List<SkinDynamicResponse>> GetSkinDynamicsResponseAsync(
            Skin skin,
            User user,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            startDate = startDate.Date;

            endDate = endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            return await _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .AsNoTracking()
                .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
                .OrderBy(x => x.DateUpdate)
                .Select(x =>
                    new SkinDynamicResponse(x.Id, x.DateUpdate, (decimal)((double)x.Price * currencyExchangeRate)))
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
}
