using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.DTOs.Skins;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Skins;

public sealed class SkinService
{
    #region Fields

    private readonly ISkinRepository _skinRepository;
    private readonly IUserRepository _userRepository;

    #endregion

    #region Constructor

    public SkinService(ISkinRepository skinRepository, IUserRepository userRepository)
    {
        _skinRepository = skinRepository;
        _userRepository = userRepository;
    }

    #endregion

    #region Methods

    public async Task<PagedResult<SkinDetailDto>> GetPagedAsync(
        int userId,
        SkinsFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _skinRepository.GetPagedAsync(userId, filter, pagination, ct);
    }

    public async Task<SkinDynamicStatsDto> GetDynamicsAsync(
        GetSkinDynamicsDto dto, CancellationToken ct = default)
    {
        if (await _skinRepository.GetByIdAsync(dto.SkinId, ct) is null)
            throw new NotFoundException("Skin", dto.SkinId);

        return await _skinRepository.GetDynamicsAsync(dto, ct);
    }

    public async Task<int> GetSteamSkinsCountAsync(int gameId, CancellationToken ct = default) =>
        await _skinRepository.GetSteamSkinsCountAsync(gameId, ct);

    public async Task<int> GetSavedSkinsCountAsync(CancellationToken ct = default) =>
        await _skinRepository.GetSavedSkinsCountAsync(ct);

    public async Task SetMarkedAsync(int skinId, int userId, CancellationToken ct = default)
    {
        if (await _skinRepository.GetByIdAsync(skinId, ct) is null)
            throw new NotFoundException("Skin", skinId);

        await _skinRepository.SetMarkedAsync(skinId, userId, ct);
    }

    public async Task RemoveMarkedAsync(int skinId, int userId, CancellationToken ct = default)
    {
        if (await _skinRepository.GetByIdAsync(skinId, ct) is null)
            throw new NotFoundException("Skin", skinId);

        await _skinRepository.RemoveMarkedAsync(skinId, userId, ct);
    }

    #endregion

    #region Private helpers

    private async Task EnsureUserExistsAsync(int userId, CancellationToken ct)
    {
        if (await _userRepository.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);
    }

    #endregion
}
