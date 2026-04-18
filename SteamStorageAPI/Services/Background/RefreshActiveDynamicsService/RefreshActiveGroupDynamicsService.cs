using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Background.RefreshActiveDynamicsService;

public class RefreshActiveGroupDynamicsService : IRefreshActiveGroupDynamicsService
{
    #region Fields

    private readonly ILogger<RefreshActiveGroupDynamicsService> _logger;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public RefreshActiveGroupDynamicsService(
        ILogger<RefreshActiveGroupDynamicsService> logger,
        SteamStorageContext context)
    {
        _logger = logger;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task RefreshActiveDynamicsAsync(CancellationToken cancellationToken = default)
    {
        DateTime todayUtc = DateTime.UtcNow.Date;

        int updatedToday = await _context.ActiveGroupsDynamics
            .CountAsync(x => x.DateUpdate >= todayUtc && x.DateUpdate < todayUtc.AddDays(1), cancellationToken);
        int totalGroups = await _context.ActiveGroups.CountAsync(cancellationToken);

        if (updatedToday >= totalGroups)
        {
            _logger.LogInformation(
                "ActiveGroupsDynamic already updated today ({Count} groups)", totalGroups);
            return;
        }

        List<ActiveGroup> activeGroups = await _context.ActiveGroups
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin)
            .Include(x => x.User)
            .ToListAsync(cancellationToken);

        List<int> currencyIds = activeGroups.Select(x => x.User.CurrencyId).Distinct().ToList();

        Dictionary<int, decimal> currencyRates = await _context.Currencies
            .Where(x => currencyIds.Contains(x.Id))
            .Include(x => x.CurrencyDynamics.OrderByDescending(d => d.DateUpdate).Take(1))
            .ToDictionaryAsync(
                x => x.Id,
                x => (decimal)(x.CurrencyDynamics.FirstOrDefault()?.Price ?? 1),
                cancellationToken);

        List<ActiveGroupsDynamic> dynamics = activeGroups
            .Select(group => new ActiveGroupsDynamic
            {
                GroupId = group.Id,
                Sum = group.Actives.Sum(a => a.Skin.CurrentPrice * a.Count)
                      * currencyRates.GetValueOrDefault(group.User.CurrencyId, 1),
                DateUpdate = DateTime.UtcNow
            })
            .ToList();

        await _context.ActiveGroupsDynamics.AddRangeAsync(dynamics, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "ActiveGroupsDynamic updated for {Count} groups", dynamics.Count);
    }

    #endregion Methods
}
