using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Games;

public sealed class GameService
{
    #region Fields

    private readonly IGameRepository _gameRepository;

    #endregion

    #region Constructor

    public GameService(IGameRepository gameRepository)
    {
        _gameRepository = gameRepository;
    }

    #endregion

    #region Methods

    public async Task<IReadOnlyList<Game>> GetAllAsync(CancellationToken ct = default) =>
        await _gameRepository.GetAllAsync(ct);

    public async Task<Game> GetByIdAsync(int gameId, CancellationToken ct = default) =>
        await _gameRepository.GetByIdAsync(gameId, ct)
        ?? throw new NotFoundException("Game", gameId);

    public async Task CreateAsync(
        int steamGameId, string title, string gameIconUrl,
        CancellationToken ct = default)
    {
        await _gameRepository.AddAsync(new Game(steamGameId, title, gameIconUrl), ct);
    }

    public async Task UpdateAsync(
        int gameId, string title, string gameIconUrl,
        CancellationToken ct = default)
    {
        Game game = await _gameRepository.GetByIdAsync(gameId, ct)
            ?? throw new NotFoundException("Game", gameId);

        game.Update(title, gameIconUrl);
        await _gameRepository.UpdateAsync(game, ct);
    }

    public async Task DeleteAsync(int gameId, CancellationToken ct = default)
    {
        Game game = await _gameRepository.GetByIdAsync(gameId, ct)
            ?? throw new NotFoundException("Game", gameId);

        await _gameRepository.DeleteAsync(game, ct);
    }

    #endregion
}
