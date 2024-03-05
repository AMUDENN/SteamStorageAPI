using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.RefreshActiveDynamicsService;

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
                cancellationToken) ==
            await _context.ActiveGroups.CountAsync(cancellationToken))
            throw new HttpResponseException(StatusCodes.Status502BadGateway,
                "Сегодня уже было выполнено обновление ActiveDynamics!");

        //TODO: Оптимизировать надо

        List<ActiveGroupsDynamic> dynamics = [];

        IQueryable<ActiveGroup> activeGroups = _context.ActiveGroups
            .AsQueryable()
            .Include(x => x.Actives)
            .ThenInclude(x => x.Skin)
            .ThenInclude(x => x.SkinsDynamics)
            .Include(x => x.User)
            .AsQueryable();

        foreach (ActiveGroup group in activeGroups)
        {
            dynamics.Add(new()
            {
                GroupId = group.Id,
                Sum = (decimal)((double)group.Actives.Where(y => y.Skin.SkinsDynamics.Count != 0).Sum(y =>
                                    y.Skin.SkinsDynamics.OrderByDescending(z => z.DateUpdate).First().Price * y.Count) *
                                _currencyService.GetCurrencyExchangeRateAsync(group.User, cancellationToken).Result),
                DateUpdate = DateTime.Now
            });
        }

        await _context.ActiveGroupsDynamics.AddRangeAsync(dynamics, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}
