using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.GamesController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SkinsController : ControllerBase
    {
        #region Enums
        public enum OrderName
        {
            Title, Price
        }
        #endregion Enums

        #region Fields
        private readonly ILogger<SkinsController> _logger;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<OrderName, Func<Skin, object>> _orderNames = new()
        {
            [OrderName.Title] = x => x.Title,
            [OrderName.Price] = x => x.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price
        };
        #endregion Fields

        #region Constructor
        public SkinsController(ILogger<SkinsController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record SkinResponse(int Id, GameResponse Game, string MarketHashName, string Title, string SkinIconUrl, string MarketUrl, IEnumerable<SkinDynamicResponse> SkinDynamics);
        public record SkinDynamicResponse(int Id, DateTime DateUpdate, decimal Price);
        public record PageCountRespose(int Count);
        public record SkinRequest(int SkinId);
        public record SkinsRequest(int GameId, string? Filter, OrderName? OrderName, bool? IsAscending, int PageNumber, int PageSize);
        public record PageCountRequest(int GameId, string? Filter, int PageSize);
        #endregion Records

        #region Methods
        private Skin? FindSkin(int Id)
        {
            return _context.Skins.FirstOrDefault(x => x.Id == Id);
        }

        private async Task<SkinResponse?> GetSkinResponse(Skin? skin)
        {
            if (skin is null)
                return null;

            await _context.Games.LoadAsync();
            await _context.SkinsDynamics.LoadAsync();

            return new SkinResponse(skin.Id,
                                    new GameResponse(skin.Game.Id, skin.Game.SteamGameId, skin.Game.Title, SteamUrls.GetGameIconUrl(skin.Game.SteamGameId, skin.Game.GameIconUrl)),
                                    skin.MarketHashName,
                                    skin.Title,
                                    SteamUrls.GetSkinIconUrl(skin.SkinIconUrl),
                                    SteamUrls.GetSkinMarketUrl(skin.Game.Id, skin.MarketHashName),
                                    skin.SkinsDynamics.OrderBy(x => x.DateUpdate)
                                    .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, x.Price)));
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetSkinInfo")]
        public async Task<ActionResult<SkinResponse>> GetSkinInfo([FromQuery] SkinRequest request)
        {
            try
            {
                Skin? skin = FindSkin(request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                return Ok(await GetSkinResponse(skin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSkins")]
        public async Task<ActionResult<IEnumerable<SkinResponse>>> GetSkins([FromQuery] SkinsRequest request)
        {
            try
            {
                IEnumerable<Skin>? skins = _context.Skins.Where(x => x.GameId == request.GameId
                                                               && request.Filter == null || x.Title.Contains(request.Filter));
                if (request.OrderName != null && request.IsAscending != null)
                    skins = (bool)request.IsAscending ? skins.OrderBy(_orderNames[(OrderName)request.OrderName]) : skins.OrderByDescending(_orderNames[(OrderName)request.OrderName]);

                skins = skins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

                return Ok(skins.Select(async x => await GetSkinResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetPagesCount")]
        public ActionResult<PageCountRespose> GetPagesCount([FromQuery] PageCountRequest request)
        {
            try
            {
                IEnumerable<Skin>? skins = _context.Skins.Where(x => x.GameId == request.GameId
                                                               && request.Filter == null || x.Title.Contains(request.Filter));

                return Ok(new PageCountRespose((int)Math.Ceiling((double)skins.Count() / request.PageSize)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET
    }
}
