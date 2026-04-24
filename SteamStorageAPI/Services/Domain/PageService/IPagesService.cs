using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.PageService;

public interface IPageService
{
    Task<PagesResponse> GetPagesAsync(
        CancellationToken cancellationToken = default);

    Task<PageResponse> GetCurrentStartPageAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task SetStartPageAsync(
        User user,
        SetPageRequest request,
        CancellationToken cancellationToken = default);
}