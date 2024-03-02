namespace SteamStorageAPI.Services.RefreshActiveDynamicsService;

public interface IRefreshActiveGroupDynamicsService
{
    Task RefreshActiveDynamicsAsync(
        CancellationToken cancellationToken = default);
}