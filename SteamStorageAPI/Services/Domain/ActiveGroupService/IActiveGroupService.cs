using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;

namespace SteamStorageAPI.Services.Domain.ActiveGroupService;

public interface IActiveGroupService
{
    Task<ActiveGroupResponse> GetActiveGroupResponseAsync(
        ActiveGroup group,
        User user,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ActiveGroupResponse>> GetActiveGroupsResponseAsync(
        IQueryable<ActiveGroup> groups,
        User user,
        CancellationToken cancellationToken = default);

    IQueryable<ActiveGroup> GetActiveGroupsQuery(User user);

    IEnumerable<ActiveGroupResponse> ApplyOrder(
        IEnumerable<ActiveGroupResponse> groups,
        ActiveGroupOrderName? orderName,
        bool? isAscending);

    Task<ActiveGroupsStatisticResponse> GetActiveGroupsStatisticAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ActiveGroupDynamicStatsResponse> GetActiveGroupDynamicsAsync(
        User user,
        GetActiveGroupDynamicRequest request,
        CancellationToken cancellationToken = default);

    Task PostActiveGroupAsync(
        User user,
        PostActiveGroupRequest request,
        CancellationToken cancellationToken = default);

    Task PutActiveGroupAsync(
        User user,
        PutActiveGroupRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteActiveGroupAsync(
        User user,
        int groupId,
        CancellationToken cancellationToken = default);
}