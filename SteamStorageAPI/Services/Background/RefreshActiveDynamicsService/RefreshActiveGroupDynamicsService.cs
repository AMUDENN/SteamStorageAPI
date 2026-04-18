using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Background.RefreshActiveDynamicsService;

public class RefreshActiveGroupDynamicsService : IRefreshActiveGroupDynamicsService
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshActiveGroupDynamicsService(
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshActiveDynamicsAsync(
        CancellationToken cancellationToken = default)
    {
        if (await _context.ActiveGroupsDynamics.CountAsync(x => x.DateUpdate.Date == DateTime.Today,
                cancellationToken)
            == await _context.ActiveGroups.CountAsync(cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "ActiveDynamics update has already been performed today!");

        List<ActiveGroup> activeGroups = await _context.ActiveGroups
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .Include(x => x.User)
            .ToListAsync(cancellationToken);

        // Load all required currencies in a single query
        List<int> currencyIds = activeGroups.Select(x => x.User.CurrencyId).Distinct().ToList();
        Dictionary<int, double> currencyRates = await _context.Currencies
            .Where(x => currencyIds.Contains(x.Id))
            .Include(x => x.CurrencyDynamics.OrderByDescending(d => d.DateUpdate).Take(1))
            .ToDictionaryAsync(
                x => x.Id,
                x => (double)(x.CurrencyDynamics.FirstOrDefault()?.Price ?? 1),
                cancellationToken);

        List<ActiveGroupsDynamic> dynamics = activeGroups
            .Select(group => new ActiveGroupsDynamic
            {
                GroupId = group.Id,
                Sum = (decimal)((double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count)
                                * currencyRates.GetValueOrDefault(group.User.CurrencyId, 1)),
                DateUpdate = DateTime.Now
            })
            .ToList();

        await _context.ActiveGroupsDynamics.AddRangeAsync(dynamics, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}