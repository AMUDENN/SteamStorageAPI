using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.StatisticsService;

public class StatisticsService : IStatisticsService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public StatisticsService(
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<InvestmentSumResponse> GetInvestmentSumAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        IQueryable<Active> actives = GetActivesQuery(user);
        IQueryable<Archive> archives = GetArchivesQuery(user);

        decimal investedSum =
            actives.Sum(y => y.BuyPrice * y.Count) + archives.Sum(y => y.BuyPrice * y.Count);

        decimal currentSum =
            actives.Sum(x => x.Skin.CurrentPrice * x.Count) * rate
            + archives.Sum(y => y.SoldPrice * y.Count);

        decimal percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

        return new InvestmentSumResponse(currentSum, percentage);
    }

    public async Task<FinancialGoalResponse> GetFinancialGoalAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        IQueryable<Active> actives = GetActivesQuery(user);
        IQueryable<Archive> archives = GetArchivesQuery(user);

        decimal financialGoal = user.GoalSum ?? 0;

        decimal currentSum =
            actives.Sum(x => x.Skin.CurrentPrice * x.Count) * rate
            + archives.Sum(y => y.SoldPrice * y.Count);

        decimal percentage = financialGoal == 0 ? 1 : currentSum / financialGoal;

        return new FinancialGoalResponse(financialGoal, percentage);
    }

    public async Task<ActiveStatisticResponse> GetActiveStatisticAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        IQueryable<Active> actives = GetActivesQuery(user);

        int count = actives.Sum(x => x.Count);
        decimal investedSum = actives.Sum(y => y.BuyPrice * y.Count);
        decimal currentSum = actives.Sum(x => x.Skin.CurrentPrice * x.Count) * rate;
        decimal percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

        return new ActiveStatisticResponse(count, currentSum, percentage);
    }

    public async Task<ArchiveStatisticResponse> GetArchiveStatisticAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Archive> archives = GetArchivesQuery(user);

        int count = archives.Sum(x => x.Count);
        decimal investedSum = archives.Sum(y => y.BuyPrice * y.Count);
        decimal soldSum = archives.Sum(y => y.SoldPrice * y.Count);
        decimal percentage = investedSum == 0 ? 1 : (soldSum - investedSum) / investedSum;

        return new ArchiveStatisticResponse(count, soldSum, percentage);
    }

    public async Task<InventoryStatisticResponse> GetInventoryStatisticAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        List<Inventory> inventories = await _context.Entry(user)
            .Collection(u => u.Inventories)
            .Query()
            .AsNoTracking()
            .Include(x => x.Skin).ThenInclude(x => x.Game)
            .ToListAsync(cancellationToken);

        int count = inventories.Sum(x => x.Count);
        decimal sum = inventories.Sum(x => x.Skin.CurrentPrice * x.Count) * rate;

        List<Game> games = inventories
            .Select(x => x.Skin.Game)
            .DistinctBy(x => x.Id)
            .ToList();

        List<InventoryGameStatisticResponse> gamesResponse = games.Select(item =>
            new InventoryGameStatisticResponse(
                item.Title,
                count == 0 ? 0 : inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count) / (decimal)count,
                inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count))).ToList();

        return new InventoryStatisticResponse(count, sum, gamesResponse);
    }

    public async Task<ItemsCountResponse> GetItemsCountAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        int activesCount = GetActivesQuery(user).Sum(x => x.Count);
        int archivesCount = GetArchivesQuery(user).Sum(x => x.Count);
        int inventoriesCount = _context.Entry(user).Collection(u => u.Inventories).Query()
            .AsNoTracking().Sum(x => x.Count);

        return new ItemsCountResponse(activesCount + archivesCount + inventoriesCount);
    }

    public async Task<UsersCountByCurrencyResponse> GetUsersCountByCurrencyAsync(
        CancellationToken cancellationToken = default)
    {
        List<UsersCountByCurrencyItemResponse> items = await _context.Currencies
            .AsNoTracking()
            .Select(c => new UsersCountByCurrencyItemResponse(
                c.Id,
                c.Title,
                c.Users.Count))
            .ToListAsync(cancellationToken);

        return new UsersCountByCurrencyResponse(items);
    }

    public async Task<ItemsCountResponse> GetItemsCountByGameAsync(
        GetItemsCountByGameRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Games.AnyAsync(x => x.Id == request.GameId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A game with this Id does not exist");

        int activesCount = await _context.Actives
            .AsNoTracking()
            .Where(x => x.Skin.GameId == request.GameId)
            .SumAsync(x => x.Count, cancellationToken);

        int archivesCount = await _context.Archives
            .AsNoTracking()
            .Where(x => x.Skin.GameId == request.GameId)
            .SumAsync(x => x.Count, cancellationToken);

        int inventoriesCount = await _context.Inventories
            .AsNoTracking()
            .Where(x => x.Skin.GameId == request.GameId)
            .SumAsync(x => x.Count, cancellationToken);

        return new ItemsCountResponse(activesCount + archivesCount + inventoriesCount);
    }

    #endregion Methods

    #region Private helpers

    private IQueryable<Active> GetActivesQuery(User user)
    {
        return _context.Entry(user)
            .Collection(u => u.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .SelectMany(x => x.Actives);
    }

    private IQueryable<Archive> GetArchivesQuery(User user)
    {
        return _context.Entry(user)
            .Collection(u => u.ArchiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Archives)
            .SelectMany(x => x.Archives);
    }

    #endregion Private helpers
}