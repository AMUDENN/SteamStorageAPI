using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Services.SkinService
{
    public interface ISkinService
    {
        Task<BaseSkinResponse> GetBaseSkinResponseAsync(
            Skin skin, 
            CancellationToken cancellationToken = default);

        Task<decimal> GetCurrentPriceAsync(
            Skin skin, 
            CancellationToken cancellationToken = default);

        Task<List<SkinDynamicResponse>> GetSkinDynamicsResponseAsync(
            Skin skin,
            DateTime startDate, 
            DateTime endDate,
            CancellationToken cancellationToken = default);

        Task<Skin> AddSkin(
            int gameId, 
            string marketHashName, 
            string title, 
            string skinIconUrl,
            CancellationToken cancellationToken = default);
    }
}
