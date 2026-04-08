using SteamStorageAPI.Application.DTOs.ArchiveGroups;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.ArchiveGroups;

public sealed class ArchiveGroupService
{
    #region Fields

    private readonly IArchiveGroupRepository _archiveGroupRepository;
    private readonly IUserRepository _userRepository;

    #endregion

    #region Constructor

    public ArchiveGroupService(
        IArchiveGroupRepository archiveGroupRepository,
        IUserRepository userRepository)
    {
        _archiveGroupRepository = archiveGroupRepository;
        _userRepository = userRepository;
    }

    #endregion

    #region Methods

    public async Task<IReadOnlyList<ArchiveGroupDto>> GetAllAsync(
        int userId, GetArchiveGroupsFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveGroupRepository.GetAllAsync(userId, filter, ct);
    }

    public async Task<ArchiveGroupsStatisticDto> GetStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveGroupRepository.GetStatisticAsync(userId, ct);
    }

    public async Task<int> GetCountAsync(int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveGroupRepository.GetCountAsync(userId, ct);
    }

    public async Task CreateAsync(CreateArchiveGroupDto dto, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(dto.UserId, ct);

        await _archiveGroupRepository.AddAsync(
            new ArchiveGroup(dto.UserId, dto.Title, dto.Description, dto.Colour), ct);
    }

    public async Task UpdateAsync(UpdateArchiveGroupDto dto, CancellationToken ct = default)
    {
        ArchiveGroup group = await _archiveGroupRepository.GetByIdAsync(dto.GroupId, dto.UserId, ct)
            ?? throw new NotFoundException(
                "Archive group with this Id does not exist or does not belong to you.", dto.GroupId);

        group.Update(dto.Title, dto.Description, dto.Colour);
        await _archiveGroupRepository.UpdateAsync(group, ct);
    }

    public async Task DeleteAsync(int userId, int groupId, CancellationToken ct = default)
    {
        ArchiveGroup group = await _archiveGroupRepository.GetByIdAsync(groupId, userId, ct)
            ?? throw new NotFoundException(
                "Archive group with this Id does not exist or does not belong to you.", groupId);

        await _archiveGroupRepository.DeleteAsync(group, ct);
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
