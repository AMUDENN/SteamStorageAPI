using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Services.SkinService
{
    public interface ISkinService
    {
        BaseSkinResponse GetBaseSkinResponse(Skin skin);
        double GetCurrentPrice(Skin skin);
        IEnumerable<SkinDynamicResponse> GetSkinDynamicsResponse(Skin skin, int days);
    }
}
