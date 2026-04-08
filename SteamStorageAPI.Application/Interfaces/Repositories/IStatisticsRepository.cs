using SteamStorageAPI.Application.DTOs.Statistics;

namespace SteamStorageAPI.Application.Interfaces.Repositories;

public interface IStatisticsRepository
{
    Task<InvestmentSumDto> GetInvestmentSumAsync(int userId, double currencyRate, CancellationToken ct = default);

    Task<ActivesStatisticDto> GetActivesStatisticAsync(int userId, double currencyRate, CancellationToken ct = default);

    Task<ArchivesStatisticDto> GetArchivesStatisticAsync(int userId, CancellationToken ct = default);

    Task<InventoryStatisticDto> GetInventoryStatisticAsync(int userId, double currencyRate, CancellationToken ct = default);

    Task<ItemsCountDto> GetItemsCountAsync(int userId, CancellationToken ct = default);
}
