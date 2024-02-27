using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.CurrencyService;

public class CurrencyService : ICurrencyService
{
    #region Fields
    
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public CurrencyService(SteamStorageContext context)
    {
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<double> GetCurrencyExchangeRateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        Currency currency =
            await _context.Currencies.FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status404NotFound, "Не найдена валюта пользователя");

        CurrencyDynamic? dynamic = await _context.Entry(currency).Collection(x => x.CurrencyDynamics).Query()
            .OrderByDescending(x => x.DateUpdate).FirstOrDefaultAsync(cancellationToken);

        return dynamic?.Price ?? 1;
    }

    #endregion Methods
}
