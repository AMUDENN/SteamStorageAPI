using SteamStorageAPI.Application.DTOs.ActiveGroups;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.ActiveGroups;

public sealed class ActiveGroupService
{
    #region Fields

    private readonly IActiveGroupRepository _activeGroupRepository;
    private readonly IUserRepository _userRepository;

    #endregion

    #region Constructor

    public ActiveGroupService(
        IActiveGroupRepository activeGroupRepository,
        IUserRepository userRepository)
    {
        _activeGroupRepository = activeGroupRepository;
        _userRepository = userRepository;
    }

    #endregion

    #region Methods

    public async Task<IReadOnlyList<ActiveGroupDto>> GetAllAsync(
        int userId, GetActiveGroupsFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeGroupRepository.GetAllAsync(userId, filter, ct);
    }

    public async Task<ActiveGroupDynamicStatsDto> GetDynamicAsync(
        GetActiveGroupDynamicDto dto, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(dto.UserId, ct);

        if (await _activeGroupRepository.GetByIdAsync(dto.GroupId, dto.UserId, ct) is null)
            throw new NotFoundException(
                "Active group with this Id does not exist or does not belong to you.", dto.GroupId);

        return await _activeGroupRepository.GetDynamicAsync(dto, ct);
    }

    public async Task<ActiveGroupsStatisticDto> GetStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeGroupRepository.GetStatisticAsync(userId, ct);
    }

    public async Task<int> GetCountAsync(int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _activeGroupRepository.GetCountAsync(userId, ct);
    }

    public async Task CreateAsync(CreateActiveGroupDto dto, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(dto.UserId, ct);

        await _activeGroupRepository.AddAsync(
            new ActiveGroup(dto.UserId, dto.Title, dto.Description, dto.Colour, dto.GoalSum), ct);
    }

    public async Task UpdateAsync(UpdateActiveGroupDto dto, CancellationToken ct = default)
    {
        ActiveGroup group = await _activeGroupRepository.GetByIdAsync(dto.GroupId, dto.UserId, ct)
            ?? throw new NotFoundException(
                "Active group with this Id does not exist or does not belong to you.", dto.GroupId);

        group.Update(dto.Title, dto.Description, dto.Colour, dto.GoalSum);
        await _activeGroupRepository.UpdateAsync(group, ct);
    }

    public async Task DeleteAsync(int userId, int groupId, CancellationToken ct = default)
    {
        ActiveGroup group = await _activeGroupRepository.GetByIdAsync(groupId, userId, ct)
            ?? throw new NotFoundException(
                "Active group with this Id does not exist or does not belong to you.", groupId);

        await _activeGroupRepository.DeleteAsync(group, ct);
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
