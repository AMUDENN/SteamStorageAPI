using SteamStorageAPI.Domain.Entities;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IGameRepository
{
    Task<Game?> GetByIdAsync(int gameId, CancellationToken ct = default);

    Task<Game> GetBaseAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(Game game, CancellationToken ct = default);

    Task UpdateAsync(Game game, CancellationToken ct = default);

    Task DeleteAsync(Game game, CancellationToken ct = default);
}
