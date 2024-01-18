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

        public BaseSkinResponse GetBaseSkinResponse(Skin skin)
        {
            _context.Entry(skin).Reference(x => x.Game).Load();
            return new(
                skin.Id,
                SteamUrls.GetSkinIconUrl(skin.SkinIconUrl),
                skin.Title,
                skin.MarketHashName,
                SteamUrls.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName));
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

        public IEnumerable<SkinDynamicResponse> GetSkinDynamicsResponse(Skin skin, int days)
        {
            return _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .OrderBy(x => x.DateUpdate)
                .Where(x => x.DateUpdate > DateTime.Now.AddDays(-days))
                .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, x.Price));
        }

        #endregion Methods
    }
}
