using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;
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
        #region Fields
        private readonly ILogger<InventoryController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public InventoryController(ILogger<InventoryController> logger, IHttpClientFactory httpClientFactory, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record InventoryResponse(int Id, SkinResponse Skin, int Count);
        public record GetInventoryRequest(int GameId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetInventory")]
        public ActionResult<IEnumerable<InventoryResponse>> GetInventory([FromQuery] GetInventoryRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                return Ok(_context.Entry(user)
                                    .Collection(u => u.Inventories)
                                    .Query()
                                    .Include(x => x.Skin)
                                    .Where(x => x.Skin.GameId == game.Id)
                                    .Select(x => new InventoryResponse(
                                        x.Id,
                                        GetSkinResponse(_context, x.Skin)!,
                                        x.Count
                                    )));
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
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                _context.Inventories.RemoveRange(_context.Inventories.Where(x => x.UserId == user.Id && x.Skin.GameId == game.Id));


                HttpClient client = _httpClientFactory.CreateClient();
                SteamInventoryResponse? response = await client.GetFromJsonAsync<SteamInventoryResponse>(SteamUrls.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000));

                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                foreach (var item in response!.descriptions)
                {
                    if (item.marketable == 0 && item.tradable == 0)
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
                        _context.Inventories.Add(new Inventory()
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
