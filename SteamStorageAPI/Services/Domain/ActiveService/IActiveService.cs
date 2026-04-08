using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;

namespace SteamStorageAPI.Services.Domain.ActiveService;

public interface IActiveService
{
    Task<ActiveResponse> GetActiveResponseAsync(
        Active active,
        User user,
        CancellationToken cancellationToken = default);

    Task<ActivesResponse> GetActivesResponseAsync(
        IQueryable<Active> actives,
        int pageNumber,
        int pageSize,
        User user,
        CancellationToken cancellationToken = default);

    IQueryable<Active> GetActivesQuery(
        User user,
        int? groupId,
        int? gameId,
        string? filter);

    IQueryable<Active> ApplyOrder(
        IQueryable<Active> actives,
        ActiveOrderName? orderName,
        bool? isAscending);

    Task PostActiveAsync(
        User user,
        PostActiveRequest request,
        CancellationToken cancellationToken = default);

    Task PutActiveAsync(
        User user,
        PutActiveRequest request,
        CancellationToken cancellationToken = default);

    Task SoldActiveAsync(
        User user,
        SoldActiveRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteActiveAsync(
        User user,
        int activeId,
        CancellationToken cancellationToken = default);
}