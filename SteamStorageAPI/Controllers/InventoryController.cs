using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
using SteamStorageAPI.Utilities;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class InventoryController : ControllerBase
    {
        #region Fields
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<InventoryController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public InventoryController(IHttpClientFactory httpClientFactory, ILogger<InventoryController> logger, SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record InventoryResponse(int Id, SkinResponse Skin, int Count);
        public record GetInventoryRequest(int GameId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetInventory")]
        public async Task<ActionResult<IEnumerable<InventoryResponse>>> GetInventory([FromQuery]GetInventoryRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                Game? game = _context.Games.Where(x => x.Id == request.GameId).FirstOrDefault();

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                return Ok(_context.Inventories.Include(x => x.Skin).Where(x => x.UserId == userId && x.Skin.GameId == game.Id)
                    .Select(x =>
                        new InventoryResponse(x.Id,
                                              GetSkinResponse(_context, x.Skin)!,
                                              x.Count)));
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
        public async Task<ActionResult> RefreshInventory(GetInventoryRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                User user = _context.Users.First(x => x.Id == userId);

                Game? game = _context.Games.Where(x => x.Id == request.GameId).FirstOrDefault();

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                _context.Inventories.RemoveRange(_context.Inventories.Where(x => x.UserId == userId && x.Skin.GameId == game.Id));

                HttpClient client = _httpClientFactory.CreateClient();
                SteamInventoryResponse? response = await client.GetFromJsonAsync<SteamInventoryResponse>(SteamUrls.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000));

                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                foreach (var item in response!.descriptions)
                {
                    if (item.marketable == 0 && item.tradable == 0)
                        continue;

                    //Сделать добавление нового скина в базу, если его ещё нет
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
                    

                    _context.Inventories.Add(new Inventory()
                    {
                        User = user,
                        Skin = skin,
                        Count = 1
                    });
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
