using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.GameService;

public interface IGameService
{
    Task<GamesResponse> GetGamesAsync(
        CancellationToken cancellationToken = default);

    Task PostGameAsync(
        PostGameRequest request,
        CancellationToken cancellationToken = default);

    Task PutGameAsync(
        PutGameRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteGameAsync(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default);
}