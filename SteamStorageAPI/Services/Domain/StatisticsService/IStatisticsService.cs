using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;

namespace SteamStorageAPI.Services.Domain.StatisticsService;

public interface IStatisticsService
{
    Task<InvestmentSumResponse> GetInvestmentSumAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<FinancialGoalResponse> GetFinancialGoalAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ActiveStatisticResponse> GetActiveStatisticAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ArchiveStatisticResponse> GetArchiveStatisticAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<InventoryStatisticResponse> GetInventoryStatisticAsync(
        User user,
        CancellationToken cancellationToken = default);

    Task<ItemsCountResponse> GetItemsCountAsync(
        User user,
        CancellationToken cancellationToken = default);
}