namespace SteamStorageAPI.Services.RefreshActiveDynamicsService;

public interface IRefreshActiveDynamicsService
{
    Task RefreshActiveDynamicsAsync(
        CancellationToken cancellationToken = default);
}