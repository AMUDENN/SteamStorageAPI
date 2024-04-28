using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Inventory;
using static SteamStorageAPI.Controllers.SkinsController;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class InventoryController : ControllerBase
    {
        #region Enums

        public enum InventoryOrderName
        {
            Title,
            Count,
            Price,
            Sum
        }

        #endregion Enums

        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public InventoryController(
            IHttpClientFactory httpClientFactory,
            ISkinService skinService,
            IUserService userService,
            ICurrencyService currencyService,
            SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _skinService = skinService;
            _userService = userService;
            _currencyService = currencyService;
            _context = context;
        }

        #endregion Constructor

        #region Records
        
        public record InventoryResponse(
            int Id,
            BaseSkinResponse Skin,
            int Count,
            decimal CurrentPrice,
            decimal CurrentSum);
        
        public record InventoriesResponse(
            int Count,
            int PagesCount,
            IEnumerable<InventoryResponse> Inventories);

        public record InventoryGameCountResponse(
            string GameTitle,
            double Percentage,
            int Count);

        public record InventoryGameSumResponse(
            string GameTitle,
            double Percentage,
            decimal Sum);

        public record InventoriesStatisticResponse(
            int InventoriesCount,
            decimal CurrentSum,
            IEnumerable<InventoryGameCountResponse> GameCount,
            IEnumerable<InventoryGameSumResponse> GameSum);

        public record InventoryPagesCountResponse(
            int Count);

        public record SavedInventoriesCountResponse(
            int Count);

        [Validator<GetInventoryRequestValidator>]
        public record GetInventoryRequest(
            int? GameId,
            string? Filter,
            InventoryOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);
        
        [Validator<GetInventoriesStatisticRequestValidator>]
        public record GetInventoriesStatisticRequest(
            int? GameId,
            string? Filter);

        [Validator<GetInventoryPagesCountRequestValidator>]
        public record GetInventoryPagesCountRequest(
            int? GameId,
            string? Filter,
            int PageSize);

        [Validator<GetSavedInventoriesCountRequestValidator>]
        public record GetSavedInventoriesCountRequest(
            int? GameId,
            string? Filter);

        [Validator<RefreshInventoryRequestValidator>]
        public record RefreshInventoryRequest(
            int GameId);

        #endregion Records
        
        #region Methods

        private async Task<InventoriesResponse> GetInventoriesResponseAsync(
            IQueryable<Inventory> inventories,
            int pageNumber,
            int pageSize,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            int inventoriesCount = await inventories.CountAsync(cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)inventoriesCount / pageSize);

            inventories = inventories.AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new(inventoriesCount,
                pagesCount,
                await Task.WhenAll(inventories.AsEnumerable()
                    .Select(async x =>
                        new InventoryResponse(
                            x.Id,
                            await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                            x.Count,
                            (decimal)((double)x.Skin.CurrentPrice * currencyExchangeRate),
                            (decimal)((double)x.Skin.CurrentPrice * currencyExchangeRate * x.Count)
                        ))).WaitAsync(cancellationToken));
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка инвенторя
        /// </summary>
        /// <response code="200">Возвращает список предметов в инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetInventory")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InventoriesResponse>> GetInventory(
            [FromQuery] GetInventoryRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Inventory> inventories = _context.Entry(user)
                .Collection(x => x.Inventories)
                .Query()
                .AsNoTracking()
                .Include(x => x.Skin)
                .ThenInclude(x => x.Game)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter)));

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case InventoryOrderName.Title:
                        inventories = request.IsAscending.Value
                            ? inventories.OrderBy(x => x.Skin.Title)
                            : inventories.OrderByDescending(x => x.Skin.Title);
                        break;
                    case InventoryOrderName.Count:
                        inventories = request.IsAscending.Value
                            ? inventories.OrderBy(x => x.Count)
                            : inventories.OrderByDescending(x => x.Count);
                        break;
                    case InventoryOrderName.Price:
                        inventories = request.IsAscending.Value
                            ? inventories.OrderBy(x => x.Skin.CurrentPrice)
                            : inventories.OrderByDescending(x => x.Skin.CurrentPrice);
                        break;
                    case InventoryOrderName.Sum:
                        inventories = request.IsAscending.Value
                            ? inventories.OrderBy(x => x.Skin.CurrentPrice * x.Count)
                            : inventories.OrderByDescending(x => x.Skin.CurrentPrice * x.Count);
                        break;
                }
            else
                inventories = inventories.OrderBy(x => x.Id);

            return Ok(await GetInventoriesResponseAsync(inventories, 
                request.PageNumber, 
                request.PageSize,
                user,
                cancellationToken));
        }

        /// <summary>
        /// Получение статистики по выборке предметов из инвентаря
        /// </summary>
        /// <response code="200">Возвращает статистику по выборке</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetInventoriesStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InventoriesStatisticResponse>> GetInventoriesStatistic(
            [FromQuery] GetInventoriesStatisticRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Inventory> inventories = _context.Entry(user)
                .Collection(x => x.Inventories)
                .Query()
                .AsNoTracking()
                .Include(x => x.Skin)
                .ThenInclude(x => x.Game)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter)));

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            int itemsCount = inventories.Sum(x => x.Count);

            decimal currentSum =
                (decimal)((double)inventories.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate);

            List<Game> games = inventories.Select(x => x.Skin.Game)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            List<InventoryGameCountResponse> gamesCountResponse = [];
            gamesCountResponse.AddRange(
                games.Select(item =>
                    new InventoryGameCountResponse(
                        item.Title,
                        itemsCount == 0
                            ? 0
                            : (double)inventories.Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.Count) / itemsCount,
                        inventories.Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.Count)))
            );

            List<InventoryGameSumResponse> gamesSumResponse = [];
            gamesSumResponse.AddRange(
                games.Select(item =>
                    new InventoryGameSumResponse(
                        item.Title,
                        currentSum == 0
                            ? 0
                            : (double)inventories
                                .Where(x => x.Skin.GameId == item.Id)
                                .AsEnumerable()
                                .Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate / (double)currentSum,
                        (decimal)((double)inventories
                            .Where(x => x.Skin.GameId == item.Id)
                            .AsEnumerable()
                            .Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate))
                ));

            return Ok(new InventoriesStatisticResponse(itemsCount, currentSum, gamesCountResponse, gamesSumResponse));
        }

        /// <summary>
        /// Получение количества страниц инвенторя
        /// </summary>
        /// <response code="200">Возвращает количество страниц инвенторя</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetInventoryPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InventoryPagesCountResponse>> GetInventoryPagesCount(
            [FromQuery] GetInventoryPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            int count = await _context
                .Entry(user)
                .Collection(x => x.Inventories)
                .Query()
                .AsNoTracking()
                .Include(x => x.Skin)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter)),
                    cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

            return Ok(new InventoryPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
        }

        /// <summary>
        /// Получение количества элементов в инвентаре
        /// </summary>
        /// <response code="200">Возвращает количество элементов в инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetSavedInventoriesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SavedInventoriesCountResponse>> GetSavedInventoriesCount(
            [FromQuery] GetSavedInventoriesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new SavedInventoriesCountResponse(await _context
                .Entry(user)
                .Collection(x => x.Inventories)
                .Query()
                .AsNoTracking()
                .Include(x => x.Skin)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter)),
                    cancellationToken)));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Обновление инвенторя
        /// </summary>
        /// <response code="200">Инвентарь успешно обновлён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpPost(Name = "RefreshInventory")]
        public async Task<ActionResult> RefreshInventory(
            RefreshInventoryRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "Игры с таким Id не существует");
            
            HttpClient client = _httpClientFactory.CreateClient();
            SteamInventoryResponse? response =
                await client.GetFromJsonAsync<SteamInventoryResponse>(
                    SteamApi.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000), cancellationToken);

            if (response is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "При получении данных с сервера Steam произошла ошибка");

            _context.Inventories.RemoveRange(_context.Entry(user)
                            .Collection(x => x.Inventories)
                            .Query()
                            .Include(x => x.Skin)
                            .Where(x => x.Skin.GameId == game.Id));
            
            foreach (InventoryDescription item in response.descriptions)
            {
                if (item is { marketable: 0, tradable: 0 })
                    continue;

                Skin skin =
                    await _context.Skins
                        .FirstOrDefaultAsync(x => x.MarketHashName == item.market_hash_name, cancellationToken) ??
                    await _skinService.AddSkinAsync(game.Id, item.market_hash_name, item.name, item.icon_url,
                        cancellationToken);

                Inventory? inventory =
                    await _context.Inventories.FirstOrDefaultAsync(x => x.SkinId == skin.Id, cancellationToken);

                if (inventory is null)
                    await _context.Inventories.AddAsync(new()
                    {
                        User = user,
                        Skin = skin,
                        Count = 1
                    }, cancellationToken);
                else
                    inventory.Count++;
                
                await _context.SaveChangesAsync(cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST
    }
}
