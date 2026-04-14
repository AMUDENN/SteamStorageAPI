using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.DTOs.Enums;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Extensions;

namespace SteamStorageAPI.Services.Domain.InventoryService;

public class InventoryService : IInventoryService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISkinService _skinService;
    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public InventoryService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        ISkinService skinService,
        ICurrencyService currencyService,
        SteamStorageContext context)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _skinService = skinService;
        _currencyService = currencyService;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<InventoriesResponse> GetInventoriesResponseAsync(
        IQueryable<Inventory> inventories,
        int pageNumber,
        int pageSize,
        User user,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        int inventoriesCount = await inventories.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)inventoriesCount / pageSize);

        inventories = inventories.AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return new InventoriesResponse(inventoriesCount,
            pagesCount,
            await Task.WhenAll(inventories.AsEnumerable()
                .Select(async x => new InventoryResponse(
                    x.Id,
                    await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                    x.Count,
                    (decimal)((double)x.Skin.CurrentPrice * rate),
                    (decimal)((double)x.Skin.CurrentPrice * rate * x.Count)
                ))).WaitAsync(cancellationToken));
    }

    public async Task<InventoriesStatisticResponse> GetInventoriesStatisticAsync(
        User user,
        int? gameId,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        double rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        IQueryable<Inventory> inventories = GetInventoryQuery(user, gameId, filter);

        int itemsCount = inventories.Sum(x => x.Count);

        decimal currentSum = (decimal)((double)inventories.Sum(x => x.Skin.CurrentPrice * x.Count) * rate);

        List<Game> games = inventories
            .Select(x => x.Skin.Game)
            .GroupBy(x => x.Id)
            .Select(g => g.First())
            .ToList();

        List<InventoryGameCountResponse> gamesCountResponse = games.Select(item =>
            new InventoryGameCountResponse(
                item.Title,
                itemsCount == 0
                    ? 0
                    : (double)inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count) / itemsCount,
                inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count))).ToList();

        List<InventoryGameSumResponse> gamesSumResponse = games.Select(item =>
            new InventoryGameSumResponse(
                item.Title,
                currentSum == 0
                    ? 0
                    : (double)inventories.Where(x => x.Skin.GameId == item.Id)
                          .AsEnumerable()
                          .Sum(x => x.Skin.CurrentPrice * x.Count)
                      * rate
                      / (double)currentSum,
                (decimal)((double)inventories.Where(x => x.Skin.GameId == item.Id)
                              .AsEnumerable()
                              .Sum(x => x.Skin.CurrentPrice * x.Count)
                          * rate))).ToList();

        return new InventoriesStatisticResponse(itemsCount, currentSum, gamesCountResponse, gamesSumResponse);
    }

    public async Task<InventoryPagesCountResponse> GetInventoryPagesCountAsync(
        User user,
        GetInventoryPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        int count = await _context.Entry(user)
            .Collection(x => x.Inventories)
            .Query()
            .AsNoTracking()
            .Include(x => x.Skin).ThenInclude(x => x.Game)
            .Where(x => request.GameId == null || x.Skin.GameId == request.GameId)
            .WhereMatchFilter(x => x.Skin.Title, request.Filter)
            .CountAsync(cancellationToken);

        int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

        return new InventoryPagesCountResponse(pagesCount == 0 ? 1 : pagesCount);
    }

    public IQueryable<Inventory> GetInventoryQuery(
        User user,
        int? gameId,
        string? filter)
    {
        return _context.Entry(user)
            .Collection(x => x.Inventories)
            .Query()
            .AsNoTracking()
            .Include(x => x.Skin).ThenInclude(x => x.Game)
            .Where(x => gameId == null || x.Skin.GameId == gameId)
            .WhereMatchFilter(x => x.Skin.Title, filter);
    }

    public IQueryable<Inventory> ApplyOrder(
        IQueryable<Inventory> inventories,
        InventoryOrderName? orderName,
        bool? isAscending)
    {
        if (orderName is null || isAscending is null)
            return inventories.OrderBy(x => x.Id);

        return orderName switch
        {
            InventoryOrderName.Title => isAscending.Value
                ? inventories.OrderBy(x => x.Skin.Title)
                : inventories.OrderByDescending(x => x.Skin.Title),
            InventoryOrderName.Count => isAscending.Value
                ? inventories.OrderBy(x => x.Count)
                : inventories.OrderByDescending(x => x.Count),
            InventoryOrderName.Price => isAscending.Value
                ? inventories.OrderBy(x => x.Skin.CurrentPrice)
                : inventories.OrderByDescending(x => x.Skin.CurrentPrice),
            InventoryOrderName.Sum => isAscending.Value
                ? inventories.OrderBy(x => x.Skin.CurrentPrice * x.Count)
                : inventories.OrderByDescending(x => x.Skin.CurrentPrice * x.Count),
            _ => inventories.OrderBy(x => x.Id)
        };
    }

    public async Task RefreshInventoryAsync(
        User user,
        RefreshInventoryRequest request,
        CancellationToken cancellationToken = default)
    {
        Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                        "Игры с таким Id не существует");

        HttpClient client = _httpClientFactory.CreateClient();
        SteamInventoryResponse? response =
            await client.GetFromJsonAsync<SteamInventoryResponse>(
                _steamApiUrlBuilder.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000), cancellationToken);

        if (response is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "При получении данных с сервера Steam произошла ошибка");

        await using IDbContextTransaction
            transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _context.Inventories.RemoveRange(_context.Entry(user)
                .Collection(x => x.Inventories)
                .Query()
                .Include(x => x.Skin)
                .Where(x => x.Skin.GameId == game.Id));

            await _context.SaveChangesAsync(cancellationToken);

            foreach (InventoryDescription item in response.descriptions)
            {
                if (item is { marketable: 0, tradable: 0 })
                    continue;

                Skin skin =
                    await _context.Skins.FirstOrDefaultAsync(x => x.MarketHashName == item.market_hash_name,
                        cancellationToken)
                    ?? await _skinService.AddSkinAsync(game.Id, item.market_hash_name, item.name, item.icon_url,
                        cancellationToken);

                Inventory? inventory =
                    await _context.Inventories.FirstOrDefaultAsync(x => x.SkinId == skin.Id, cancellationToken);

                if (inventory is null)
                    await _context.Inventories.AddAsync(new Inventory
                    {
                        User = user,
                        Skin = skin,
                        Count = 1
                    }, cancellationToken);
                else
                    inventory.Count++;
            }

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    #endregion Methods
}