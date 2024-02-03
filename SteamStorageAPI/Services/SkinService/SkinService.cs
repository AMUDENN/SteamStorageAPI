﻿using SteamStorageAPI.DBEntities;
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

        public BaseSkinResponse GetBaseSkinResponse(Skin skin)
        {
            _context.Entry(skin).Reference(x => x.Game).Load();
            return new(
                skin.Id,
                SteamApi.GetSkinIconUrl(skin.SkinIconUrl),
                skin.Title,
                skin.MarketHashName,
                SteamApi.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));
        }

        public decimal GetCurrentPrice(Skin skin)
        {
            List<SkinsDynamic> dynamics = _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .OrderBy(x => x.DateUpdate)
                .ToList();

            return dynamics.Count == 0 ? 0 : dynamics.Last().Price;
        }

        public List<SkinDynamicResponse> GetSkinDynamicsResponse(Skin skin, DateTime startDate, DateTime endDate)
        {
            return _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
                .OrderBy(x => x.DateUpdate)
                .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, x.Price))
                .ToList();
        }

        #endregion Methods
    }
}
