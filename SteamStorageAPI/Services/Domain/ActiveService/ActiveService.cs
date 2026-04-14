using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Extensions;

namespace SteamStorageAPI.Services.Domain.ActiveService;

public class ActiveService : IActiveService
{
    #region Fields

    private readonly ISkinService _skinService;
    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public ActiveService(
        ISkinService skinService,
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _skinService = skinService;
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    private async Task<ActiveResponse> MapToResponseAsync(
        Active active,
        double rate,
        CancellationToken cancellationToken = default)
    {
        return new ActiveResponse(
            active.Id,
            active.GroupId,
            await _skinService.GetBaseSkinResponseAsync(active.Skin, cancellationToken),
            active.BuyDate,
            active.Count,
            active.BuyPrice,
            (decimal)((double)active.Skin.CurrentPrice * rate),
            (decimal)((double)active.Skin.CurrentPrice * rate * active.Count),
            active.GoalPrice,
            active.GoalPrice == null
                ? null
                : (double)active.Skin.CurrentPrice * rate / (double)active.GoalPrice,
            ((double)active.Skin.CurrentPrice * rate - (double)active.BuyPrice) / (double)active.BuyPrice,
            active.Description);
    }

    public async Task<ActiveResponse> GetActiveResponseAsync(
        Active active,
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);
        return await MapToResponseAsync(active, rate, cancellationToken);
    }

    public async Task<ActiveResponse> GetActiveInfoAsync(
        User user,
        int activeId,
        CancellationToken cancellationToken = default)
    {
        Active active = await _context.Entry(user)
                            .Collection(x => x.ActiveGroups)
                            .Query()
                            .AsNoTracking()
                            .SelectMany(x => x.Actives)
                            .Include(x => x.Skin)
                            .ThenInclude(x => x.Game)
                            .FirstOrDefaultAsync(x => x.Id == activeId, cancellationToken)
                        ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Актива с таким Id не существует");

        return await GetActiveResponseAsync(active, user, cancellationToken);
    }

    public async Task<ActivesResponse> GetActivesResponseAsync(
        IQueryable<Active> actives,
        int pageNumber,
        int pageSize,
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        int activesCount = await actives.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)activesCount / pageSize);

        List<Active> page = await actives
            .AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        IEnumerable<ActiveResponse> responses = await Task.WhenAll(
            page.Select(x => MapToResponseAsync(x, rate, cancellationToken)));

        return new ActivesResponse(activesCount, pagesCount, responses);
    }

    public async Task<ActivesStatisticResponse> GetActivesStatisticAsync(
        User user,
        GetActivesStatisticRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Active> actives = _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .SelectMany(x => x.Actives)
            .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                        && (request.GroupId == null || x.GroupId == request.GroupId))
            .WhereMatchFilter(x => x.Skin.Title, request.Filter);

        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        return new ActivesStatisticResponse(
            await actives.SumAsync(x => x.Count, cancellationToken),
            await actives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken),
            (decimal)((double)await actives.SumAsync(x => x.Skin.CurrentPrice * x.Count, cancellationToken) * rate));
    }

    public async Task<ActivesPagesCountResponse> GetActivesPagesCountAsync(
        User user,
        GetActivesPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        int count = await _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .SelectMany(x => x.Actives)
            .WhereMatchFilter(x => x.Skin.Title, request.Filter)
            .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                             && (request.GroupId == null || x.GroupId == request.GroupId),
                cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

        return new ActivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount);
    }

    public async Task<ActivesCountResponse> GetActivesCountAsync(
        User user,
        GetActivesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        int count = await _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .SelectMany(x => x.Actives)
            .WhereMatchFilter(x => x.Skin.Title, request.Filter)
            .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                             && (request.GroupId == null || x.GroupId == request.GroupId),
                cancellationToken);

        return new ActivesCountResponse(count);
    }

    public IQueryable<Active> GetActivesQuery(
        User user,
        int? groupId,
        int? gameId,
        string? filter)
    {
        return _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin).ThenInclude(x => x.Game)
            .SelectMany(x => x.Actives)
            .Where(x => (gameId == null || x.Skin.GameId == gameId)
                        && (groupId == null || x.GroupId == groupId))
            .WhereMatchFilter(x => x.Skin.Title, filter);
    }

    public IQueryable<Active> ApplyOrder(
        IQueryable<Active> actives,
        ActiveOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return actives.OrderBy(x => x.Id);

        return orderName switch
        {
            ActiveOrderName.Title => isAscending.Value
                ? actives.OrderBy(x => x.Skin.Title)
                : actives.OrderByDescending(x => x.Skin.Title),
            ActiveOrderName.Count => isAscending.Value
                ? actives.OrderBy(x => x.Count)
                : actives.OrderByDescending(x => x.Count),
            ActiveOrderName.BuyPrice => isAscending.Value
                ? actives.OrderBy(x => x.BuyPrice)
                : actives.OrderByDescending(x => x.BuyPrice),
            ActiveOrderName.CurrentPrice => isAscending.Value
                ? actives.OrderBy(x => x.Skin.CurrentPrice)
                : actives.OrderByDescending(x => x.Skin.CurrentPrice),
            ActiveOrderName.CurrentSum => isAscending.Value
                ? actives.OrderBy(x => x.Skin.CurrentPrice * x.Count)
                : actives.OrderByDescending(x => x.Skin.CurrentPrice * x.Count),
            ActiveOrderName.Change => isAscending.Value
                ? actives.OrderBy(x => (x.Skin.CurrentPrice - x.BuyPrice) / x.BuyPrice)
                : actives.OrderByDescending(x => (x.Skin.CurrentPrice - x.BuyPrice) / x.BuyPrice),
            _ => actives.OrderBy(x => x.Id)
        };
    }

    public async Task PostActiveAsync(
        User user,
        PostActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

        if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "Предмета с таким Id не существует");

        await _context.Actives.AddAsync(new Active
        {
            GroupId = request.GroupId,
            Count = request.Count,
            BuyPrice = request.BuyPrice,
            GoalPrice = request.GoalPrice,
            SkinId = request.SkinId,
            Description = request.Description,
            BuyDate = request.BuyDate
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutActiveAsync(
        User user,
        PutActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        Active active = await _context.Entry(user)
                            .Collection(u => u.ActiveGroups)
                            .Query()
                            .Include(x => x.Actives)
                            .SelectMany(x => x.Actives)
                            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                        ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

        if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

        if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "Предмета с таким Id не существует");

        active.GroupId = request.GroupId;
        active.Count = request.Count;
        active.BuyPrice = request.BuyPrice;
        active.GoalPrice = request.GoalPrice;
        active.SkinId = request.SkinId;
        active.Description = request.Description;
        active.BuyDate = request.BuyDate;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SoldActiveAsync(
        User user,
        SoldActiveRequest request,
        CancellationToken cancellationToken = default)
    {
        Active active = await _context.Entry(user)
                            .Collection(u => u.ActiveGroups)
                            .Query()
                            .Include(x => x.Actives)
                            .SelectMany(x => x.Actives)
                            .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                        ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

        if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

        if (request.Count > active.Count)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                $"Количество продаваемых предметов ({request.Count}) превышает количество в активе ({active.Count})");

        await _context.Archives.AddAsync(new Archive
        {
            GroupId = request.GroupId,
            SkinId = active.SkinId,
            Count = request.Count,
            BuyDate = active.BuyDate,
            BuyPrice = active.BuyPrice,
            SoldDate = request.SoldDate,
            SoldPrice = request.SoldPrice
        }, cancellationToken);

        if (request.Count >= active.Count)
            _context.Actives.Remove(active);
        else
            active.Count -= request.Count;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteActiveAsync(
        User user,
        int activeId,
        CancellationToken cancellationToken = default)
    {
        Active active = await _context.Entry(user)
                            .Collection(u => u.ActiveGroups)
                            .Query()
                            .Include(x => x.Actives)
                            .SelectMany(x => x.Actives)
                            .FirstOrDefaultAsync(x => x.Id == activeId, cancellationToken)
                        ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

        _context.Actives.Remove(active);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}