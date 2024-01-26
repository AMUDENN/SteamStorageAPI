using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Services.JwtProvider
{
    public interface IJwtProvider
    {
        public string Generate(User tokenOwner);
    }
}
