using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Services;

public interface IJwtProvider
{
    string Generate(User user);
}
