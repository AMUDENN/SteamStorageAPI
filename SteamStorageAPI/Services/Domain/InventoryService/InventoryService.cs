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
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        int inventoriesCount = await inventories.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)inventoriesCount / pageSize);

        List<Inventory> page = await inventories.AsNoTracking()
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new InventoriesResponse(inventoriesCount,
            pagesCount,
            await Task.WhenAll(page
                .Select(async x => new InventoryResponse(
                    x.Id,
                    await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                    x.Count,
                    x.Skin.CurrentPrice * rate,
                    x.Skin.CurrentPrice * rate * x.Count
                ))).WaitAsync(cancellationToken));
    }

    public async Task<InventoriesStatisticResponse> GetInventoriesStatisticAsync(
        User user,
        int? gameId,
        string? filter,
        CancellationToken cancellationToken = default)
    {
        decimal rate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        var gameStats = await GetInventoryQuery(user, gameId, filter)
            .GroupBy(x => new
            {
                x.Skin.GameId,
                x.Skin.Game.Title
            })
            .Select(g => new
            {
                GameTitle = g.Key.Title,
                Count = g.Sum(x => x.Count),
                PriceSum = g.Sum(x => x.Skin.CurrentPrice * x.Count)
            })
            .ToListAsync(cancellationToken);

        int itemsCount = gameStats.Sum(g => g.Count);
        decimal rawSum = gameStats.Sum(g => g.PriceSum);
        decimal currentSum = rawSum * rate;

        List<InventoryGameCountResponse> gamesCountResponse = gameStats
            .Select(g => new InventoryGameCountResponse(
                g.GameTitle,
                itemsCount == 0 ? 0 : g.Count / (decimal)itemsCount,
                g.Count))
            .ToList();

        List<InventoryGameSumResponse> gamesSumResponse = gameStats
            .Select(g => new InventoryGameSumResponse(
                g.GameTitle,
                currentSum == 0 ? 0 : g.PriceSum * rate / currentSum,
                g.PriceSum * rate))
            .ToList();

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
                        "A game with this Id does not exist");

        using HttpClient client = _httpClientFactory.CreateClient();
        SteamInventoryResponse? response =
            await client.GetFromJsonAsync<SteamInventoryResponse>(
                _steamApiUrlBuilder.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000), cancellationToken);

        if (response is null)
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "An error occurred while retrieving data from the Steam server");

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

            List<InventoryDescription> validItems = (response.descriptions ?? [])
                .Where(x => !(x is { marketable: 0, tradable: 0 }))
                .ToList();

            List<string> hashNames = validItems
                .Select(x => x.market_hash_name!)
                .Distinct()
                .ToList();

            Dictionary<string, Skin> skinsByHash = await _context.Skins
                .Where(x => hashNames.Contains(x.MarketHashName))
                .ToDictionaryAsync(x => x.MarketHashName, cancellationToken);

            foreach (string hash in hashNames.Where(h => !skinsByHash.ContainsKey(h)))
            {
                InventoryDescription item = validItems.First(x => x.market_hash_name == hash);
                Skin newSkin = new()
                {
                    GameId = game.Id,
                    MarketHashName = hash,
                    Title = item.name!,
                    SkinIconUrl = item.icon_url!
                };
                await _context.Skins.AddAsync(newSkin, cancellationToken);
                skinsByHash[hash] = newSkin;
            }

            Dictionary<string, int> counts = new();
            foreach (InventoryDescription item in validItems)
                counts[item.market_hash_name!] = counts.GetValueOrDefault(item.market_hash_name!) + 1;

            foreach ((string hash, int count) in counts)
            {
                if (!skinsByHash.TryGetValue(hash, out Skin? skin))
                    continue;
                await _context.Inventories.AddAsync(new Inventory
                {
                    User = user,
                    Skin = skin,
                    Count = count
                }, cancellationToken);
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