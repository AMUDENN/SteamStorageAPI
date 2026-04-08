// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace SteamStorageAPI.Application.DTOs.Common;

public sealed class PagedResult<T>
{
    public int TotalCount { get; init; }
    public int PagesCount { get; init; }
    public IReadOnlyList<T> Items { get; init; }

    public PagedResult(int totalCount, int pageSize, IReadOnlyList<T> items)
    {
        TotalCount = totalCount;
        PagesCount = pageSize > 0
            ? (int)Math.Ceiling((double)totalCount / pageSize)
            : 0;
        Items = items;
    }
}
