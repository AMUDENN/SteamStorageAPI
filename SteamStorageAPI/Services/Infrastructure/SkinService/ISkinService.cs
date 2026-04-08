using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Infrastructure.SkinService;

public interface ISkinService
{
    Task<BaseSkinResponse> GetBaseSkinResponseAsync(
        Skin skin,
        CancellationToken cancellationToken = default);

    Task<SkinResponse> GetSkinResponseAsync(
        Skin skin,
        User user,
        IEnumerable<int> markedSkinsIds,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<SkinResponse>> GetSkinsResponseAsync(
        IQueryable<Skin> skins,
        User user,
        IEnumerable<int> markedSkinsIds,
        CancellationToken cancellationToken = default);

    Task<List<SkinDynamicResponse>> GetSkinDynamicsResponseAsync(
        Skin skin,
        User user,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<Skin> AddSkinAsync(
        int gameId,
        string marketHashName,
        string title,
        string skinIconUrl,
        CancellationToken cancellationToken = default);
}