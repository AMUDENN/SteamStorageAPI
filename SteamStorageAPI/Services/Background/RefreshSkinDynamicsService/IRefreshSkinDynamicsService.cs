namespace SteamStorageAPI.Services.Background.RefreshSkinDynamicsService;

public interface IRefreshSkinDynamicsService
{
    Task RefreshSkinDynamicsAsync(
        CancellationToken cancellationToken = default);
}