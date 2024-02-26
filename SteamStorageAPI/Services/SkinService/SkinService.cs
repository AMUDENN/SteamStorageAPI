using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Services.SkinService
{
    public class SkinService : ISkinService
    {
        #region Fields

        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public SkinService(SteamStorageContext context)
        {
            _context = context;
        }

        #endregion Constructor

        #region Methods

        public async Task<BaseSkinResponse> GetBaseSkinResponseAsync(
            Skin skin,
            CancellationToken cancellationToken = default)
        {
            await _context.Entry(skin).Reference(x => x.Game).LoadAsync(cancellationToken);
            return new(
                skin.Id,
                SteamApi.GetSkinIconUrl(skin.SkinIconUrl),
                skin.Title,
                skin.MarketHashName,
                SteamApi.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));
        }

        public async Task<decimal> GetCurrentPriceAsync(
            Skin skin,
            CancellationToken cancellationToken = default)
        {
            List<SkinsDynamic> dynamics = await _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .OrderBy(x => x.DateUpdate)
                .ToListAsync(cancellationToken);

            return dynamics.Count == 0 ? 0 : dynamics.Last().Price;
        }

        public async Task<List<SkinDynamicResponse>> GetSkinDynamicsResponseAsync(
            Skin skin,
            DateTime startDate,
            DateTime endDate,
            CancellationToken cancellationToken = default)
        {
            startDate = startDate.Date;

            endDate = endDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            return await _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
                .OrderBy(x => x.DateUpdate)
                .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, x.Price))
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
