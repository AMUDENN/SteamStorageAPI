using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Controllers.GroupsController;
using SteamStorageAPI.Utilities;
using static SteamStorageAPI.Controllers.SkinsController;
using Microsoft.EntityFrameworkCore;
using static SteamStorageAPI.Controllers.GamesController;
using SteamStorageAPI.Utilities.Steam;
using System.Net.Http;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Models.SteamAPIModels.Inventory;

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
        #endregion Records

        #region GET
        [HttpGet(Name = "GetInventory")]
        public async Task<ActionResult<IEnumerable<InventoryResponse>>> GetInventory()
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                await _context.Skins.LoadAsync();
                await _context.Games.LoadAsync();
                await _context.SkinsDynamics.LoadAsync();

                return Ok(_context.Inventories.Where(x => x.UserId == userId)
                    .Select(x =>
                        new InventoryResponse(x.Id,
                                              GetSkinResponse(x.Skin)!,
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
        public async Task<ActionResult> RefreshInventory()
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                User user = _context.Users.First(x => x.Id == userId);

                _context.Inventories.RemoveRange(_context.Inventories.Where(x => x.UserId == userId));

                HttpClient client = _httpClientFactory.CreateClient();

                IEnumerable<Game> games = _context.Games.ToList();

                foreach (Game game in games)
                {
                    SteamInventoryResponse? response = await client.GetFromJsonAsync<SteamInventoryResponse>(SteamUrls.GetInventoryUrl(user.SteamId, game.SteamGameId, 2000));

                    if (response is null)
                        continue;

                    foreach (var item in response!.descriptions)
                    {
                        Skin? skin = _context.Skins.FirstOrDefault(x => x.MarketHashName == item.market_hash_name);
                        if (skin is null)
                            continue;

                        _context.Inventories.Add(new Inventory()
                        {
                            User = user,
                            Skin = skin,
                            Count = 1
                        });
                    }
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
