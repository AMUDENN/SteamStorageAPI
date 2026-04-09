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

        return new(currency.Id,
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
            await _context.Currencies.FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken)
            ?? throw new HttpResponseException(StatusCodes.Status404NotFound, "Не найдена валюта пользователя");

        CurrencyDynamic? dynamic = await _context.Entry(currency)
            .Collection(x => x.CurrencyDynamics)
            .Query()
            .OrderByDescending(x => x.DateUpdate)
            .FirstOrDefaultAsync(cancellationToken);

        return dynamic?.Price ?? 1;
    }

    public async Task<CurrenciesResponse> GetCurrenciesAsync(
        CancellationToken cancellationToken = default)
    {
        IQueryable<CurrencyResponse> currencies = _context.Currencies
            .AsNoTracking()
            .Include(x => x.CurrencyDynamics)
            .Select(x => new CurrencyResponse(
                x.Id,
                x.SteamCurrencyId,
                x.Title,
                x.Mark,
                x.CultureInfo,
                x.CurrencyDynamics.Count != 0
                    ? x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).First().Price
                    : 0,
                x.CurrencyDynamics.Count != 0
                    ? x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).First().DateUpdate
                    : DateTime.Now));

        return new(await currencies.CountAsync(cancellationToken), currencies);
    }

    public async Task<CurrencyResponse> GetCurrencyAsync(
        GetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "Валюты с таким Id не существует");

        return await GetCurrencyResponseAsync(currency, cancellationToken);
    }

    public async Task<CurrencyResponse> GetCurrentCurrencyAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        Currency currency = await _context.Currencies.AsNoTracking()
                                .FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken)
                            ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "Валюты с таким Id не существует");

        return await GetCurrencyResponseAsync(currency, cancellationToken);
    }

    public async Task PostCurrencyAsync(
        PostCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        await _context.Currencies.AddAsync(new()
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
                                "Валюты с таким Id не существует");

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
                "Валюты с таким Id не существует");

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
                                "Валюты с таким Id не существует");

        _context.Currencies.Remove(currency);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}