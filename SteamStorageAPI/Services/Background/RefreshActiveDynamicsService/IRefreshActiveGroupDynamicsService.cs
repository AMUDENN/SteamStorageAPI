namespace SteamStorageAPI.Services.Background.RefreshActiveDynamicsService;

public interface IRefreshActiveGroupDynamicsService
{
    Task RefreshActiveDynamicsAsync(
        CancellationToken cancellationToken = default);
}