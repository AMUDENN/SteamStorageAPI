using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Services.Infrastructure.CurrencyService;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.ActiveGroupService;

public class ActiveGroupService : IActiveGroupService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public ActiveGroupService(
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<ActiveGroupResponse> GetActiveGroupResponseAsync(
        ActiveGroup group,
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        return new(group.Id,
            group.Title,
            group.Description,
            $"#{group.Colour ?? ActiveGroup.BASE_ACTIVE_GROUP_COLOUR}",
            group.GoalSum,
            group.GoalSum == null
                ? null
                : (double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate / (double)group.GoalSum,
            group.Actives.Sum(y => y.Count),
            group.Actives.Sum(y => y.BuyPrice * y.Count),
            (decimal)((double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate),
            group.Actives.Sum(y => y.BuyPrice) != 0
                ? ((double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate
                   - (double)group.Actives.Sum(y => y.BuyPrice * y.Count))
                  / (double)group.Actives.Sum(y => y.BuyPrice * y.Count)
                : 0,
            group.DateCreation);
    }

    public async Task<IEnumerable<ActiveGroupResponse>> GetActiveGroupsResponseAsync(
        IQueryable<ActiveGroup> groups,
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        return await groups.Select(x => new ActiveGroupResponse(
            x.Id,
            x.Title,
            x.Description,
            $"#{x.Colour ?? ActiveGroup.BASE_ACTIVE_GROUP_COLOUR}",
            x.GoalSum,
            x.GoalSum == null
                ? null
                : (double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate / (double)x.GoalSum,
            x.Actives.Sum(y => y.Count),
            x.Actives.Sum(y => y.BuyPrice * y.Count),
            (decimal)((double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate),
            x.Actives.Sum(y => y.BuyPrice) != 0
                ? ((double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * rate
                   - (double)x.Actives.Sum(y => y.BuyPrice * y.Count))
                  / (double)x.Actives.Sum(y => y.BuyPrice * y.Count)
                : 0,
            x.DateCreation)).ToListAsync(cancellationToken);
    }

    public IQueryable<ActiveGroup> GetActiveGroupsQuery(User user)
    {
        return _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin);
    }

    public IEnumerable<ActiveGroupResponse> ApplyOrder(
        IEnumerable<ActiveGroupResponse> groups,
        ActiveGroupOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return groups.OrderBy(x => x.Id);

        return orderName switch
        {
            ActiveGroupOrderName.Title => isAscending.Value
                ? groups.OrderBy(x => x.Title)
                : groups.OrderByDescending(x => x.Title),
            ActiveGroupOrderName.Count => isAscending.Value
                ? groups.OrderBy(x => x.Count)
                : groups.OrderByDescending(x => x.Count),
            ActiveGroupOrderName.BuySum => isAscending.Value
                ? groups.OrderBy(x => x.BuySum)
                : groups.OrderByDescending(x => x.BuySum),
            ActiveGroupOrderName.CurrentSum => isAscending.Value
                ? groups.OrderBy(x => x.CurrentSum)
                : groups.OrderByDescending(x => x.CurrentSum),
            ActiveGroupOrderName.Change => isAscending.Value
                ? groups.OrderBy(x => x.Change)
                : groups.OrderByDescending(x => x.Change),
            _ => groups.OrderBy(x => x.Id)
        };
    }

    public async Task<ActiveGroupsStatisticResponse> GetActiveGroupsStatisticAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        IQueryable<ActiveGroup> groups = _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin).ThenInclude(x => x.Game);

        IQueryable<Active> actives = groups.SelectMany(x => x.Actives);

        List<Game> games = actives
            .Select(x => x.Skin.Game)
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();

        int activesCount = actives.Sum(x => x.Count);
        decimal buyPriceSum = actives.Sum(x => x.BuyPrice * x.Count);
        decimal latestPriceSum = (decimal)((double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * rate);

        List<ActiveGroupsGameCountResponse> gamesCountResponse = games.Select(item =>
            new ActiveGroupsGameCountResponse(
                item.Title,
                activesCount == 0
                    ? 0
                    : (double)actives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count) / activesCount,
                actives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count))).ToList();

        List<ActiveGroupsGameInvestmentSumResponse> gamesInvestmentSumResponse = games.Select(item =>
            new ActiveGroupsGameInvestmentSumResponse(
                item.Title,
                buyPriceSum == 0
                    ? 0
                    : (double)(actives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.BuyPrice * x.Count)
                               / buyPriceSum),
                actives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.BuyPrice * x.Count))).ToList();

        List<ActiveGroupsGameCurrentSumResponse> gamesCurrentSumResponse = games.Select(item =>
            new ActiveGroupsGameCurrentSumResponse(
                item.Title,
                latestPriceSum == 0
                    ? 0
                    : (double)actives.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Skin.CurrentPrice * x.Count)
                      * rate
                      / (double)latestPriceSum,
                (decimal)((double)actives.Where(x => x.Skin.GameId == item.Id)
                              .Sum(x => x.Skin.CurrentPrice * x.Count)
                          * rate))).ToList();

        return new(activesCount, buyPriceSum, latestPriceSum,
            gamesCountResponse, gamesInvestmentSumResponse, gamesCurrentSumResponse);
    }

    public async Task<ActiveGroupDynamicStatsResponse> GetActiveGroupDynamicsAsync(
        User user,
        GetActiveGroupDynamicRequest request,
        CancellationToken cancellationToken = default)
    {
        ActiveGroup group = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

        DateTime startDate = request.StartDate.Date;
        DateTime endDate = request.EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

        List<ActiveGroupDynamicResponse> dynamic = await _context.Entry(group)
            .Collection(s => s.ActiveGroupsDynamics)
            .Query()
            .AsNoTracking()
            .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
            .Select(x => new ActiveGroupDynamicResponse(x.Id, x.DateUpdate, x.Sum))
            .ToListAsync(cancellationToken);

        double changePeriod = (double)(dynamic.Count == 0 || dynamic.First().Sum == 0
            ? 0
            : (dynamic.Last().Sum - dynamic.First().Sum) / dynamic.First().Sum);

        return new(changePeriod, dynamic);
    }

    public async Task PostActiveGroupAsync(
        User user,
        PostActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        await _context.ActiveGroups.AddAsync(new()
        {
            UserId = user.Id,
            Title = request.Title,
            Description = request.Description,
            Colour = request.Colour,
            GoalSum = request.GoalSum,
            DateCreation = DateTime.Now
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutActiveGroupAsync(
        User user,
        PutActiveGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        ActiveGroup group = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

        group.Title = request.Title;
        group.Description = request.Description;
        group.Colour = request.Colour;
        group.GoalSum = request.GoalSum;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteActiveGroupAsync(
        User user,
        int groupId,
        CancellationToken cancellationToken = default)
    {
        ActiveGroup group = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

        _context.ActiveGroups.Remove(group);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}