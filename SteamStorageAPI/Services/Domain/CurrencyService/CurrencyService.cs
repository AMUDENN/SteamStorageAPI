using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.CurrencyService;

public class CurrencyService : ICurrencyService
{
    #region Fields

    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public CurrencyService(
        SteamStorageContext context)
    {
        _context = context;
    }

    #endregion Constructor

    #region Methods

    private async Task<CurrencyResponse> GetCurrencyResponseAsync(
        Currency currency,
        CancellationToken cancellationToken = default)
    {
        CurrencyDynamic? lastDynamic = await _context.Entry(currency)
            .Collection(s => s.CurrencyDynamics)
            .Query()
            .AsNoTracking()
            .OrderBy(x => x.DateUpdate)
            .LastOrDefaultAsync(cancellationToken);

        return new CurrencyResponse(currency.Id,
            currency.SteamCurrencyId,
            currency.Title,
            currency.Mark,
            currency.CultureInfo,
            lastDynamic?.Price ?? 0,
            lastDynamic?.DateUpdate ?? DateTime.Now);
    }

    public async Task<double> GetCurrencyExchangeRateAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        Currency currency =
            await _context.Currencies
                .Include(x => x.CurrencyDynamics.OrderByDescending(d => d.DateUpdate).Take(1))
                .FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status404NotFound, "User currency not found");

        return currency.CurrencyDynamics.FirstOrDefault()?.Price ?? 1;
    }

    public async Task<CurrenciesResponse> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        IQueryable<CurrencyResponse> currencies = _context.Currencies
            .AsNoTracking()
            .Select(x => new CurrencyResponse(
                x.Id,
                x.SteamCurrencyId,
                x.Title,
                x.Mark,
                x.CultureInfo,
                x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).Select(y => y.Price).FirstOrDefault(),
                x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).Select(y => (DateTime?)y.DateUpdate)
                    .FirstOrDefault() ?? DateTime.Now));

        return new CurrenciesResponse(await currencies.CountAsync(cancellationToken), currencies);
    }

    public async Task<CurrencyResponse> GetCurrencyAsync(
        GetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "A currency with this Id does not exist");

        return await GetCurrencyResponseAsync(currency, cancellationToken);
    }

    public async Task<CurrencyResponse> GetCurrentCurrencyAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "A currency with this Id does not exist");

        return await GetCurrencyResponseAsync(currency, cancellationToken);
    }

    public async Task PostCurrencyAsync(
        PostCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        await _context.Currencies.AddAsync(new Currency
        {
            SteamCurrencyId = request.SteamCurrencyId,
            Title = request.Title,
            Mark = request.Mark,
            CultureInfo = request.CultureInfo
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task PutCurrencyInfoAsync(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies
                                .FirstOrDefaultAsync(x => x.Id == request.CurrencyId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "A currency with this Id does not exist");

        currency.Title = request.Title;
        currency.Mark = request.Mark;
        currency.CultureInfo = request.CultureInfo;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetCurrencyAsync(
        User user,
        SetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Currencies.AnyAsync(x => x.Id == request.CurrencyId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A currency with this Id does not exist");

        user.CurrencyId = request.CurrencyId;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteCurrencyAsync(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies
                                .FirstOrDefaultAsync(x => x.Id == request.CurrencyId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "A currency with this Id does not exist");

        _context.Currencies.Remove(currency);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrencyDynamicsResponse> GetCurrencyDynamicsAsync(
        GetCurrencyDynamicsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _context.Currencies.AnyAsync(x => x.Id == request.CurrencyId, cancellationToken))
            throw new HttpResponseException(StatusCodes.Status404NotFound,
                "A currency with this Id does not exist");

        DateTime since = DateTime.Now.AddDays(-30);

        List<CurrencyDynamicResponse> dynamics = await _context.CurrencyDynamics
            .AsNoTracking()
            .Where(x => x.CurrencyId == request.CurrencyId && x.DateUpdate >= since)
            .OrderBy(x => x.DateUpdate)
            .Select(x => new CurrencyDynamicResponse(x.Id, x.DateUpdate, x.Price))
            .ToListAsync(cancellationToken);

        return new CurrencyDynamicsResponse(dynamics);
    }

    #endregion Methods
}