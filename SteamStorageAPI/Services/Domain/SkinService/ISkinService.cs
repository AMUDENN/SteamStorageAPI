using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.SkinService;

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

    Task<SkinResponse> GetSkinInfoAsync(
        User user,
        GetSkinInfoRequest request,
        CancellationToken cancellationToken = default);

    Task<BaseSkinsResponse> GetBaseSkinsAsync(
        GetBaseSkinsRequest request,
        CancellationToken cancellationToken = default);

    Task<SkinsResponse> GetSkinsAsync(
        User user,
        GetSkinsRequest request,
        CancellationToken cancellationToken = default);

    Task<SkinDynamicStatsResponse> GetSkinDynamicsAsync(
        User user,
        GetSkinDynamicsRequest request,
        CancellationToken cancellationToken = default);

    Task<SkinPagesCountResponse> GetSkinPagesCountAsync(
        User user,
        GetSkinPagesCountRequest request,
        CancellationToken cancellationToken = default);

    Task<SteamSkinsCountResponse> GetSteamSkinsCountAsync(
        GetSteamSkinsCountRequest request,
        CancellationToken cancellationToken = default);

    Task<SavedSkinsCountResponse> GetSavedSkinsCountAsync(
        User user,
        GetSavedSkinsCountRequest request,
        CancellationToken cancellationToken = default);

    Task PostSkinAsync(
        PostSkinRequest request,
        CancellationToken cancellationToken = default);

    Task SetMarkedSkinAsync(
        User user,
        SetMarkedSkinRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteMarkedSkinAsync(
        User user,
        DeleteMarkedSkinRequest request,
        CancellationToken cancellationToken = default);
}