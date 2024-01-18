using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
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

        private readonly ILogger<InventoryController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<InventoryOrderName, Func<Inventory, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public InventoryController(ILogger<InventoryController> logger, IHttpClientFactory httpClientFactory,
            ISkinService skinService, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [InventoryOrderName.Title] = x => x.Skin.Title,
                [InventoryOrderName.Count] = x => x.Count,
                [InventoryOrderName.Price] = x => _skinService.GetCurrentPrice(x.Skin),
                [InventoryOrderName.Sum] = x => _skinService.GetCurrentPrice(x.Skin) * x.Count
            };
        }

        #endregion Constructor

        #region Records

        public record InventoryResponse(int Id, BaseSkinResponse Skin, int Count);

        public record InventoryPagesCountResponse(int Count);

        public record SavedInventoriesCountResponse(int Count);

        public record GetInventoryRequest(
            int? GameId,
            string? Filter,
            InventoryOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        public record GetInventoryPagesCountRequest(int? GameId, string? Filter, int PageSize);

        public record GetSavedInventoriesCountRequest(int? GameId, string? Filter);

        public record RefreshInventoryRequest(int GameId);

        #endregion Records

        #region GET

        [HttpGet(Name = "GetInventory")]
        public ActionResult<IEnumerable<InventoryResponse>> GetInventory([FromQuery] GetInventoryRequest request)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                    throw new("Размер и номер страницы не могут быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<Inventory> inventories = _context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!)));

                if (request is { OrderName: not null, IsAscending: not null })
                    inventories = (bool)request.IsAscending
                        ? inventories.OrderBy(_orderNames[(InventoryOrderName)request.OrderName])
                        : inventories.OrderByDescending(_orderNames[(InventoryOrderName)request.OrderName]);

                inventories = inventories.Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize);

                return Ok(inventories.Select(x =>
                    new InventoryResponse(x.Id, _skinService.GetBaseSkinResponse(x.Skin), x.Count)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetInventoryPagesCount")]
        public ActionResult<InventoryPagesCountResponse> GetInventoryPagesCount(
            [FromQuery] GetInventoryPagesCountRequest request)
        {
            try
            {
                if (request.PageSize <= 0)
                    throw new("Размер страницы не может быть меньше или равен нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<Inventory> inventories = _context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!)));

                return Ok(new InventoryPagesCountResponse(
                    (int)Math.Ceiling((double)inventories.Count() / request.PageSize)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSavedInventoriesCount")]
        public ActionResult<SavedInventoriesCountResponse> GetSavedInventoriesCount(
            [FromQuery] GetSavedInventoriesCountRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new SavedInventoriesCountResponse(_context
                    .Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Count(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!)))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region POST

        [HttpPost(Name = "RefreshInventory")]
        public async Task<ActionResult> RefreshInventory(RefreshInventoryRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                _context.Inventories.RemoveRange(_context.Entry(user)
                    .Collection(x => x.Inventories)
                    .Query()
                    .Include(x => x.Skin)
                    .Where(x => x.Skin.GameId == game.Id));


                HttpClient client = _httpClientFactory.CreateClient();
                SteamInventoryResponse? response =
                    await client.GetFromJsonAsync<SteamInventoryResponse>(
                        SteamUrls.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000));

                if (response is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                foreach (InventoryDescription item in response.descriptions)
                {
                    if (item is { marketable: 0, tradable: 0 })
                        continue;

                    Skin? skin = _context.Skins.FirstOrDefault(x => x.MarketHashName == item.market_hash_name);
                    if (skin is null)
                    {
                        skin = new()
                        {
                            GameId = game.Id,
                            MarketHashName = item.market_hash_name,
                            Title = item.name,
                            SkinIconUrl = item.icon_url
                        };
                        _context.Skins.Add(skin);
                    }


                    Inventory? inv = _context.Inventories.FirstOrDefault(x => x.SkinId == skin.Id);

                    if (inv is null)
                        _context.Inventories.Add(new()
                        {
                            User = user,
                            Skin = skin,
                            Count = 1
                        });
                    else
                        inv.Count++;
                }

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion POST
    }
}
