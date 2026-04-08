using SteamStorageAPI.Application.DTOs.Statistics;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Statistics;

public sealed class StatisticsService
{
    #region Fields

    private readonly IStatisticsRepository _statisticsRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrencyRepository _currencyRepository;

    #endregion

    #region Constructor

    public StatisticsService(
        IStatisticsRepository statisticsRepository,
        IUserRepository userRepository,
        ICurrencyRepository currencyRepository)
    {
        _statisticsRepository = statisticsRepository;
        _userRepository = userRepository;
        _currencyRepository = currencyRepository;
    }

    #endregion

    #region Methods

    public async Task<InvestmentSumDto> GetInvestmentSumAsync(
        int userId, CancellationToken ct = default)
    {
        double rate = await GetRateAsync(userId, ct);
        return await _statisticsRepository.GetInvestmentSumAsync(userId, rate, ct);
    }

    public async Task<FinancialGoalDto> GetFinancialGoalAsync(
        int userId, CancellationToken ct = default)
    {
        User user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        double rate = await _currencyRepository.GetExchangeRateAsync(user.CurrencyId, ct);
        InvestmentSumDto stats = await _statisticsRepository.GetInvestmentSumAsync(userId, rate, ct);

        return new FinancialGoalDto(
            (decimal)(user.GoalSum ?? 0),
            user.CalculateGoalCompletion(stats.TotalSum));
    }

    public async Task<ActivesStatisticDto> GetActivesStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        double rate = await GetRateAsync(userId, ct);
        return await _statisticsRepository.GetActivesStatisticAsync(userId, rate, ct);
    }

    public async Task<ArchivesStatisticDto> GetArchivesStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _statisticsRepository.GetArchivesStatisticAsync(userId, ct);
    }

    public async Task<InventoryStatisticDto> GetInventoryStatisticAsync(
        int userId, CancellationToken ct = default)
    {
        double rate = await GetRateAsync(userId, ct);
        return await _statisticsRepository.GetInventoryStatisticAsync(userId, rate, ct);
    }

    public async Task<ItemsCountDto> GetItemsCountAsync(
        int userId, CancellationToken ct = default)
    {
        await EnsureUserExistsAsync(userId, ct);
        return await _statisticsRepository.GetItemsCountAsync(userId, ct);
    }

    #endregion

    #region Private helpers

    private async Task<double> GetRateAsync(int userId, CancellationToken ct)
    {
        User user = await _userRepository.GetByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);
        return await _currencyRepository.GetExchangeRateAsync(user.CurrencyId, ct);
    }

    private async Task EnsureUserExistsAsync(int userId, CancellationToken ct)
    {
        if (await _userRepository.GetByIdAsync(userId, ct) is null)
            throw new NotFoundException("User", userId);
    }

    #endregion
}
