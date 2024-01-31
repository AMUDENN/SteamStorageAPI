using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Services.SkinService
{
    public interface ISkinService
    {
        BaseSkinResponse GetBaseSkinResponse(Skin skin);
        decimal GetCurrentPrice(Skin skin);
        List<SkinDynamicResponse> GetSkinDynamicsResponse(Skin skin, DateTime startDate, DateTime endDate);
    }
}
