using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.ArchiveService;

public interface IArchiveService
{
    ArchiveResponse GetArchiveResponse(
        Archive archive);

    Task<ArchivesResponse> GetArchivesResponseAsync(
        IQueryable<Archive> archives,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    IQueryable<Archive> GetArchivesQuery(
        User user,
        int? groupId,
        int? gameId,
        string? filter);

    IQueryable<Archive> ApplyOrder(
        IQueryable<Archive> archives,
        Models.DTOs.Enums.ArchiveOrderName? orderName,
        bool? isAscending);

    Task PostArchiveAsync(
        User user,
        PostArchiveRequest request,
        CancellationToken cancellationToken = default);

    Task PutArchiveAsync(
        User user,
        PutArchiveRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteArchiveAsync(
        User user,
        int archiveId,
        CancellationToken cancellationToken = default);
}