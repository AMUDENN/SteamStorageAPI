using SteamStorageAPI.Application.DTOs.Archives;
using SteamStorageAPI.Application.DTOs.Common;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Archives;

public sealed class ArchiveService
{
    #region Fields

    private readonly IArchiveRepository _archiveRepository;
    private readonly IArchiveGroupRepository _archiveGroupRepository;
    private readonly ISkinRepository _skinRepository;
    private readonly IUserRepository _userRepository;

    #endregion

    #region Constructor

    public ArchiveService(
        IArchiveRepository archiveRepository,
        IArchiveGroupRepository archiveGroupRepository,
        ISkinRepository skinRepository,
        IUserRepository userRepository)
    {
        _archiveRepository = archiveRepository;
        _archiveGroupRepository = archiveGroupRepository;
        _skinRepository = skinRepository;
        _userRepository = userRepository;
    }

    #endregion

    #region Methods

    public async Task<Archive> GetByIdAsync(
        int userId, int archiveId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);

        return await _archiveRepository.GetByIdAsync(archiveId, userId, ct)
            ?? throw new NotFoundException("Archive item", archiveId);
    }

    public async Task<PagedResult<ArchiveDto>> GetPagedAsync(
        int userId,
        ArchivesFilterDto filter,
        PaginationDto pagination,
        CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveRepository.GetPagedAsync(userId, filter, pagination, ct);
    }

    public async Task<ArchiveStatisticDto> GetStatisticAsync(
        int userId, ArchivesFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveRepository.GetStatisticAsync(userId, filter, ct);
    }

    public async Task<int> GetCountAsync(
        int userId, ArchivesFilterDto filter, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _archiveRepository.GetCountAsync(userId, filter, ct);
    }

    public async Task CreateAsync(CreateArchiveDto dto, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(dto.UserId, ct);
        await EnsureArchiveGroupBelongsToUserAsync(dto.GroupId, dto.UserId, ct);
        await EnsureSkinExistsAsync(dto.SkinId, ct);

        await _archiveRepository.AddAsync(new Archive(
            dto.GroupId, dto.SkinId, dto.Count,
            dto.BuyPrice, dto.SoldPrice,
            dto.BuyDate, dto.SoldDate, dto.Description), ct);
    }

    public async Task UpdateAsync(UpdateArchiveDto dto, CancellationToken ct = default)
    {
        Archive archive = await _archiveRepository.GetByIdAsync(dto.ArchiveId, dto.UserId, ct)
            ?? throw new NotFoundException(
                "Archive item with this Id does not exist or does not belong to you.", dto.ArchiveId);

        await EnsureArchiveGroupBelongsToUserAsync(dto.GroupId, dto.UserId, ct);
        await EnsureSkinExistsAsync(dto.SkinId, ct);

        archive.Update(
            dto.GroupId, dto.SkinId, dto.Count,
            dto.BuyPrice, dto.SoldPrice,
            dto.BuyDate, dto.SoldDate, dto.Description);

        await _archiveRepository.UpdateAsync(archive, ct);
    }

    public async Task DeleteAsync(int userId, int archiveId, CancellationToken ct = default)
    {
        Archive archive = await _archiveRepository.GetByIdAsync(archiveId, userId, ct)
            ?? throw new NotFoundException(
                "Archive item with this Id does not exist or does not belong to you.", archiveId);

        await _archiveRepository.DeleteAsync(archive, ct);
    }

    #endregion

    #region Private helpers

    private async Task EnsureUserExistsAsync(int userId, CancellationToken ct)
    {
        if (await _userRepository.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);
    }

    private async Task EnsureArchiveGroupBelongsToUserAsync(int groupId, int userId, CancellationToken ct)
    {
        if (await _archiveGroupRepository.GetByIdAsync(groupId, userId, ct) is null)
            throw new NotFoundException(
                "Archive group with this Id does not exist or does not belong to you.", groupId);
    }

    private async Task EnsureSkinExistsAsync(int skinId, CancellationToken ct)
    {
        if (await _skinRepository.GetByIdAsync(skinId, ct) is null)
            throw new NotFoundException("Skin", skinId);
    }

    #endregion
}
