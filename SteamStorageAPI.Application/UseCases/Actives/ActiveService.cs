using SteamStorageAPI.Application.DTOs.Actives;
using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Actives;

public sealed class ActiveService
{
    #region Fields

    private readonly IActiveRepository _activeRepository;
    private readonly IActiveGroupRepository _activeGroupRepository;
    private readonly IArchiveRepository _archiveRepository;
    private readonly IArchiveGroupRepository _archiveGroupRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IUserRepository _userRepository;

    #endregion

    #region Constructor

    public ActiveService(
        IActiveRepository activeRepository,
        IActiveGroupRepository activeGroupRepository,
        IArchiveRepository archiveRepository,
        IArchiveGroupRepository archiveGroupRepository,
        ISkinRepository skinRepository,
        IUserRepository userRepository)
    {
        _activeRepository = activeRepository;
        _activeGroupRepository = activeGroupRepository;
        _archiveRepository = archiveRepository;
        _archiveGroupRepository = archiveGroupRepository;
        _skinRepository = skinRepository;
        _userRepository = userRepository;
    }

    #endregion

    #region Methods

    public async Task<Active> GetByIdAsync(
        int userId, int activeId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);

        return await _activeRepository.GetByIdAsync(activeId, userId, ct)
            ?? throw new NotFoundException("Active", activeId);
    }

    public async Task<PagedResult<ActiveDto>> GetPagedAsync(
        int userId,
        ActivesFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeRepository.GetPagedAsync(userId, filter, pagination, ct);
    }

    public async Task<ActiveStatisticDto> GetStatisticAsync(
        int userId, ActivesFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeRepository.GetStatisticAsync(userId, filter, ct);
    }

    public async Task<int> GetCountAsync(
        int userId, ActivesFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeRepository.GetCountAsync(userId, filter, ct);
    }

    public async Task CreateAsync(CreateActiveDto dto, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(dto.UserId, ct);
        await EnsureActiveGroupBelongsToUserAsync(dto.GroupId, dto.UserId, ct);
        await EnsureSkinExistsAsync(dto.SkinId, ct);

        await _activeRepository.AddAsync(new Active(
            dto.GroupId, dto.SkinId, dto.Count,
            dto.BuyPrice, dto.GoalPrice,
            dto.BuyDate, dto.Description), ct);
    }

    public async Task UpdateAsync(UpdateActiveDto dto, CancellationToken ct = default)
    {
        Active active = await _activeRepository.GetByIdAsync(dto.ActiveId, dto.UserId, ct)
            ?? throw new NotFoundException(
                "Active with this Id does not exist or does not belong to you.", dto.ActiveId);

        await EnsureActiveGroupBelongsToUserAsync(dto.GroupId, dto.UserId, ct);
        await EnsureSkinExistsAsync(dto.SkinId, ct);

        active.Update(
            dto.GroupId, dto.SkinId, dto.Count,
            dto.BuyPrice, dto.GoalPrice,
            dto.BuyDate, dto.Description);

        await _activeRepository.UpdateAsync(active, ct);
    }

    public async Task SellAsync(SoldActiveDto dto, CancellationToken ct = default)
    {
        Active active = await _activeRepository.GetByIdAsync(dto.ActiveId, dto.UserId, ct)
            ?? throw new NotFoundException(
                "Active with this Id does not exist or does not belong to you.", dto.ActiveId);

        if (await _archiveGroupRepository.GetByIdAsync(dto.ArchiveGroupId, dto.UserId, ct) is null)
            throw new NotFoundException(
                "Archive group with this Id does not exist or does not belong to you.", dto.ArchiveGroupId);

        await _archiveRepository.AddAsync(new Archive(
            dto.ArchiveGroupId, active.SkinId, active.Count,
            active.BuyPrice, dto.SoldPrice,
            active.BuyDate, dto.SoldDate, active.Description), ct);

        await _activeRepository.DeleteAsync(active, ct);
    }

    public async Task DeleteAsync(int userId, int activeId, CancellationToken ct = default)
    {
        Active active = await _activeRepository.GetByIdAsync(activeId, userId, ct)
            ?? throw new NotFoundException(
                "Active with this Id does not exist or does not belong to you.", activeId);

        await _activeRepository.DeleteAsync(active, ct);
    }

    #endregion

    #region Private helpers

    private async Task EnsureUserExistsAsync(int userId, CancellationToken ct)
    {
        if (await _userRepository.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);
    }

    private async Task EnsureActiveGroupBelongsToUserAsync(int groupId, int userId, CancellationToken ct)
    {
        if (await _activeGroupRepository.GetByIdAsync(groupId, userId, ct) is null)
            throw new NotFoundException(
                "Active group with this Id does not exist or does not belong to you.", groupId);
    }

    private async Task EnsureSkinExistsAsync(int skinId, CancellationToken ct)
    {
        if (await _skinRepository.GetByIdAsync(skinId, ct) is null)
            throw new NotFoundException("Skin", skinId);
    }

    #endregion
}
