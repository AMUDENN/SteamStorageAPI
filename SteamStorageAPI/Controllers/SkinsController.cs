using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Games;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Controllers.GamesController;
using static SteamStorageAPI.Utilities.ProgramConstants;

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
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<SkinsController> _logger;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<OrderName, Func<Skin, object>> _orderNames = new()
        {
            [OrderName.Title] = x => x.Title,
            [OrderName.Price] = x => x.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price
        };
        #endregion Fields

        #region Constructor
        public SkinsController(IHttpClientFactory httpClientFactory, ILogger<SkinsController> logger, SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
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
        public record PostSkinsRequest(int GameId);
        public record PostSkinRequest(int GameId, string MarketHashName);
        #endregion Records

        #region Methods
        private Skin? FindSkin(int Id)
        {
            return _context.Skins.FirstOrDefault(x => x.Id == Id);
        }

        private Game? FindGame(int Id)
        {
            return _context.Games.FirstOrDefault(x => x.Id == Id);
        }

        public static SkinResponse? GetSkinResponse(Skin? skin)
        {
            if (skin is null)
                return null;

            return new SkinResponse(skin.Id,
                                    new GameResponse(skin.Game.Id, skin.Game.SteamGameId, skin.Game.Title, SteamUrls.GetGameIconUrl(skin.Game.SteamGameId, skin.Game.GameIconUrl)),
                                    skin.MarketHashName,
                                    skin.Title,
                                    SteamUrls.GetSkinIconUrl(skin.SkinIconUrl),
                                    SteamUrls.GetSkinMarketUrl(skin.Game.SteamGameId, skin.MarketHashName),
                                    skin.SkinsDynamics.OrderBy(x => x.DateUpdate)
                                    .Select(x => new SkinDynamicResponse(x.Id, x.DateUpdate, x.Price)));
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetSkinInfo")]
        public async Task<ActionResult<SkinResponse>> GetSkinInfo([FromQuery]SkinRequest request)
        {
            try
            {
                Skin? skin = FindSkin(request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                await _context.Games.LoadAsync();
                await _context.SkinsDynamics.LoadAsync();

                return Ok(GetSkinResponse(skin));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSkins")]
        public async Task<ActionResult<IEnumerable<SkinResponse>>> GetSkins([FromQuery]SkinsRequest request)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                    throw new Exception("Размер и номер страницы не могут быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new Exception("Размер страницы не может превышать 200 предметов");

                IEnumerable<Skin>? skins = _context.Skins.Where(x => x.GameId == request.GameId
                                                               && request.Filter == null || x.Title.Contains(request.Filter));
                if (request.OrderName != null && request.IsAscending != null)
                    skins = (bool)request.IsAscending ? skins.OrderBy(_orderNames[(OrderName)request.OrderName]) : skins.OrderByDescending(_orderNames[(OrderName)request.OrderName]);

                skins = skins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

                await _context.Games.LoadAsync();
                await _context.SkinsDynamics.LoadAsync();

                return Ok(skins.Select(x => GetSkinResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetPagesCount")]
        public ActionResult<PageCountRespose> GetPagesCount([FromQuery]PageCountRequest request)
        {
            try
            {
                if (request.PageSize <= 0)
                    throw new Exception("Размер страницы не может быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new Exception("Размер страницы не может превышать 200 предметов");

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

        #region POST
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost(Name = "PostSkins")]
        public async Task<ActionResult> PostSkins(PostSkinsRequest request)
        {
            try
            {
                Game? game = FindGame(request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();

                int count = 100;
                int start = 0;

                int answerCount = 100;

                int totalCount = -1;

                while (count == answerCount || start < totalCount)
                {
                    try
                    {
                        SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(SteamUrls.GetSkinsUrl(game.SteamGameId, count, start));

                        if (response is null)
                            throw new Exception("При получении данных с сервера Steam произошла ошибка");

                        totalCount = response.total_count;

                        List<Skin> skins = [];

                        foreach (SkinResult item in response.results)
                        {
                            if (_context.Skins.Any(x => x.MarketHashName == item.hash_name))
                                continue;

                            skins.Add(new Skin()
                            {
                                GameId = game.Id,
                                MarketHashName = item.hash_name,
                                Title = item.name,
                                SkinIconUrl = item.asset_description.icon_url
                            });
                        }

                        _context.Skins.AddRange(skins);

                        await _context.SaveChangesAsync();
                        
                        answerCount = response.results.Length;
                        start += response.results.Length;

                        await Task.Delay(3000);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        await Task.Delay(30000);
                    }
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpPost(Name = "PostSkin")]
        public async Task<ActionResult> PostSkin(PostSkinRequest request)
        {
            try
            {
                Game? game = FindGame(request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();

                
                SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(SteamUrls.GetSkinInfo(request.MarketHashName));

                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                var result = response.results.First();

                _context.Skins.Add(new Skin()
                {
                    GameId = game.Id,
                    MarketHashName = result.hash_name,
                    Title = result.name,
                    SkinIconUrl = result.asset_description.icon_url
                });

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
