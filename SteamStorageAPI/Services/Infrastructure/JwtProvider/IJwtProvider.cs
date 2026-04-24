using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.JwtProvider;

public interface IJwtProvider
{
    public string Generate(User tokenOwner);
}