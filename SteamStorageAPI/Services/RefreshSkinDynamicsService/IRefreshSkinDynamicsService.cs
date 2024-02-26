namespace SteamStorageAPI.Services.RefreshSkinDynamicsService;

public interface IRefreshSkinDynamicsService
{
    Task RefreshSkinDynamicsAsync(
        CancellationToken cancellationToken = default);
}