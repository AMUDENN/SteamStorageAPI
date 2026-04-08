using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Inventory;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Application.Interfaces.Services;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Inventory;

public sealed class InventoryService
{
    #region Fields

    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IGameRepository _gameRepository;
    private readonly ISteamApiClient _steamApiClient;

    #endregion

    #region Constructor

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IUserRepository userRepository,
        ISkinRepository skinRepository,
        IGameRepository gameRepository,
        ISteamApiClient steamApiClient)
    {
        _inventoryRepository = inventoryRepository;
        _userRepository = userRepository;
        _skinRepository = skinRepository;
        _gameRepository = gameRepository;
        _steamApiClient = steamApiClient;
    }

    #endregion

    #region Methods

    public async Task<PagedResult<InventoryItemDto>> GetPagedAsync(
        int userId,
        InventoryFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _inventoryRepository.GetPagedAsync(userId, filter, pagination, ct);
    }

    public async Task<InventoryStatisticDto> GetStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _inventoryRepository.GetStatisticAsync(userId, ct);
    }

    public async Task<int> GetSavedCountAsync(int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _inventoryRepository.GetSavedCountAsync(userId, ct);
    }

    public async Task RefreshAsync(int userId, int gameId, CancellationToken ct = default)
    {
        Domain.Entities.User user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        Game game = await _gameRepository.GetByIdAsync(gameId, ct)
            ?? throw new NotFoundException("Game", gameId);

        IReadOnlyList<SteamInventoryItemDto> steamItems =
            await _steamApiClient.GetInventoryAsync(user.SteamId, game.SteamGameId, 2000, ct);

        List<Domain.Entities.Inventory> fresh = [];

        foreach (SteamInventoryItemDto item in steamItems)
        {
            Skin skin = await _skinRepository.GetByMarketHashNameAsync(item.MarketHashName, ct)
                        ?? await CreateSkinAsync(game.Id, item, ct);

            fresh.Add(new Domain.Entities.Inventory(userId, skin.Id, item.Count));
        }

        await _inventoryRepository.RefreshAsync(userId, fresh, ct);
    }

    #endregion

    #region Private helpers

    private async Task<Skin> CreateSkinAsync(int gameId, SteamInventoryItemDto item, CancellationToken ct)
    {
        Skin skin = new(gameId, item.MarketHashName, item.Name, item.IconUrl);
        await _skinRepository.AddAsync(skin, ct);
        return skin;
    }

    private async Task EnsureUserExistsAsync(int userId, CancellationToken ct)
    {
        if (await _userRepository.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);
    }

    #endregion
}
