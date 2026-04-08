using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;

namespace SteamStorageAPI.Services.Domain.ArchiveGroupService;

public interface IArchiveGroupService
{
    ArchiveGroupResponse GetArchiveGroupResponse(ArchiveGroup group);

    Task<IEnumerable<ArchiveGroupResponse>> GetArchiveGroupsResponseAsync(
        IQueryable<ArchiveGroup> groups,
        CancellationToken cancellationToken = default);

    IQueryable<ArchiveGroup> GetArchiveGroupsQuery(User user);

    IEnumerable<ArchiveGroupResponse> ApplyOrder(
        IEnumerable<ArchiveGroupResponse> groups,
        ArchiveGroupOrderName? orderName,
        bool? isAscending);

    Task<ArchiveGroupsStatisticResponse> GetArchiveGroupsStatisticAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task PostArchiveGroupAsync(
        User user,
        PostArchiveGroupRequest request,
        CancellationToken cancellationToken = default);

    Task PutArchiveGroupAsync(
        User user,
        PutArchiveGroupRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteArchiveGroupAsync(
        User user,
        int groupId,
        CancellationToken cancellationToken = default);
}