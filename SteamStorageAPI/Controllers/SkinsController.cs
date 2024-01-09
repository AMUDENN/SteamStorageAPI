using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class SkinsController : ControllerBase
    {
        #region Enums
        public enum SkinOrderName
        {
            Title, Price, Change7D, Change30D
        }
        #endregion Enums

        #region Fields
        private readonly ILogger<SkinsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<SkinOrderName, Func<Skin, object>> _orderNames;
        #endregion Fields

        #region Constructor
        public SkinsController(ILogger<SkinsController> logger, IHttpClientFactory httpClientFactory, ISkinService skinService, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [SkinOrderName.Title] = x => x.Title,
                [SkinOrderName.Price] = x => _skinService.GetCurrentPrice(x),
                [SkinOrderName.Change7D] = x =>
                {
                    List<SkinsDynamic> dynamic7 = _context.Entry(x)
                                   .Collection(x => x.SkinsDynamics)
                                   .Query()
                                   .Where(x => x.DateUpdate > DateTime.Now.AddDays(-7))
                                   .OrderBy(x => x.DateUpdate)
                                   .ToList();

                    double currentPrice = _skinService.GetCurrentPrice(x);

                    return dynamic7.Count == 0 ? 0 : (currentPrice - (double)dynamic7.First().Price) / (double)dynamic7.First().Price;
                },
                [SkinOrderName.Change30D] = x =>
                {
                    List<SkinsDynamic> dynamic30 = _context.Entry(x)
                                   .Collection(x => x.SkinsDynamics)
                                   .Query()
                                   .Where(x => x.DateUpdate > DateTime.Now.AddDays(-30))
                                   .OrderBy(x => x.DateUpdate)
                                   .ToList();

                    double currentPrice = _skinService.GetCurrentPrice(x);

                    return dynamic30.Count == 0 ? 0 : (currentPrice - (double)dynamic30.First().Price) / (double)dynamic30.First().Price;
                }
            };
        }
        #endregion Constructor

        #region Records
        public record BaseSkinResponse(int Id, string SkinIconUrl, string Title, string MarketHashName, string MarketUrl);
        public record SkinResponse(BaseSkinResponse Skin, double CurrentPrice, double Change7D, double Change30D, bool IsMarked);
        public record SkinDynamicResponse(int Id, DateTime DateUpdate, decimal Price);
        public record SkinPageCountRespose(int Count);
        public record SteamSkinsCountResponse(int Count);
        public record SavedSkinsCountResponse(int Count);
        public record GetSkinRequest(int SkinId);
        public record GetSkinsRequest(int? GameId, string? Filter, SkinOrderName? OrderName, bool? IsAscending, bool? IsMarked, int PageNumber, int PageSize);
        public record GetSkinDynamicsRequest(int SkinId, int Days);
        public record GetSkinPagesCountRequest(int? GameId, string? Filter, bool? IsMarked, int PageSize);
        public record GetSteamSkinsCountRequest(int GameId);
        public record GetSavedSkinsCountRequest(int? GameId, string? Filter, bool? IsMarked);
        public record PostSkinsRequest(int GameId);
        public record PostSkinRequest(int GameId, string MarketHashName);
        #endregion Records

        #region Methods
        private SkinResponse GetSkinResponse(Skin skin, User user)
        {
            List<SkinsDynamic> dynamics = _context.Entry(skin)
                               .Collection(x => x.SkinsDynamics)
                               .Query()
                               .OrderBy(x => x.DateUpdate)
                               .ToList();

            List<SkinsDynamic> dynamic7 = dynamics.Where(x => x.DateUpdate > DateTime.Now.AddDays(-7)).ToList();
            List<SkinsDynamic> dynamic30 = dynamics.Where(x => x.DateUpdate > DateTime.Now.AddDays(-30)).ToList();

            double currentPrice = _skinService.GetCurrentPrice(skin);

            double change7D = dynamic7.Count == 0 ? 0 : (currentPrice - (double)dynamic7.First().Price) / (double)dynamic7.First().Price;
            double change30D = dynamic30.Count == 0 ? 0 : (currentPrice - (double)dynamic30.First().Price) / (double)dynamic30.First().Price;

            bool isMarked = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Where(x => x.SkinId ==  skin.Id).Any();

            return new SkinResponse(_skinService.GetBaseSkinResponse(skin), currentPrice, change7D, change30D, isMarked);
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetSkinInfo")]
        public ActionResult<SkinResponse> GetSkinInfo([FromQuery] GetSkinRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");
                
                Skin? skin =  _context.Skins.FirstOrDefault(x => x.Id == request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                return Ok(GetSkinResponse(skin, user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSkins")]
        public ActionResult<IEnumerable<SkinResponse>> GetSkins([FromQuery] GetSkinsRequest request)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                    throw new Exception("Размер и номер страницы не могут быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new Exception("Размер страницы не может превышать 200 предметов");

                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<int> markedSkinIds = Enumerable.Empty<int>();

                if (request.IsMarked is not null)
                    markedSkinIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Select(x => x.SkinId).ToList();

                IEnumerable<Skin>? skins = _context.Skins.Where(x => (request.GameId == null || x.GameId == request.GameId)
                                                && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter!))
                                                && (request.IsMarked == null || request.IsMarked == markedSkinIds.Any(y => y == x.Id)));

                if (request.OrderName != null && request.IsAscending != null)
                    skins = (bool)request.IsAscending ? skins.OrderBy(_orderNames[(SkinOrderName)request.OrderName]) 
                                                      : skins.OrderByDescending(_orderNames[(SkinOrderName)request.OrderName]);

                skins = skins.Skip((request.PageNumber - 1) * request.PageSize)
                             .Take(request.PageSize);

                return Ok(skins.Select(x => GetSkinResponse(x, user)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSkinDynamics")]
        public ActionResult<IEnumerable<SkinDynamicResponse>> GetSkinDynamics([FromQuery] GetSkinDynamicsRequest request)
        {
            try
            {
                Skin? skin = _context.Skins.FirstOrDefault(x => x.Id == request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                return Ok(_skinService.GetSkinDynamicsResponse(skin, request.Days));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSkinPagesCount")]
        public ActionResult<SkinPageCountRespose> GetSkinPagesCount([FromQuery] GetSkinPagesCountRequest request)
        {
            try
            {
                if (request.PageSize <= 0)
                    throw new Exception("Размер страницы не может быть меньше или равен нулю.");

                if (request.PageSize > 200)
                    throw new Exception("Размер страницы не может превышать 200 предметов");

                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<int> markedSkinIds = Enumerable.Empty<int>();

                if (request.IsMarked is not null)
                    markedSkinIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Select(x => x.SkinId).ToList();

                IEnumerable<Skin>? skins = _context.Skins.Where(x => (request.GameId == null || x.GameId == request.GameId)
                                                && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter!))
                                                && (request.IsMarked == null || request.IsMarked == markedSkinIds.Any(y => y == x.Id)));

                return Ok(new SkinPageCountRespose((int)Math.Ceiling((double)skins.Count() / request.PageSize)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSteamSkinsCount")]
        public async Task<ActionResult<SteamSkinsCountResponse>> GetSteamSkinsCount([FromQuery] GetSteamSkinsCountRequest request)
        {
            try
            {
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();
                SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(SteamUrls.GetSkinsUrl(game.SteamGameId, 1, 0));

                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                return Ok(new SteamSkinsCountResponse(response.total_count));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetSavedSkinsCount")]
        public ActionResult<SavedSkinsCountResponse> GetSavedSkinsCount([FromQuery] GetSavedSkinsCountRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<int> markedSkinIds = Enumerable.Empty<int>(); 

                if (request.IsMarked is not null)
                    markedSkinIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Select(x => x.SkinId).ToList();

                IEnumerable<Skin>? skins = _context.Skins.Where(x => (request.GameId == null || x.GameId == request.GameId)
                                                                && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter!))
                                                                && (request.IsMarked == null || request.IsMarked == markedSkinIds.Any(y => y == x.Id)));

                return Ok(new SavedSkinsCountResponse(skins.Count()));
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
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();

                int count = 100;
                int start = 0;

                int answerCount = 100;

                SteamSkinResponse? response = await client.GetFromJsonAsync<SteamSkinResponse>(SteamUrls.GetSkinsUrl(game.SteamGameId, 1, 0));

                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                int totalCount = response.total_count;

                Random rnd = new();

                while (count == answerCount || start < totalCount)
                {
                    try
                    {
                        _logger.LogInformation($"Процесс выполнения загрузки скинов:\nЗагружено: {start} / {totalCount}");


                        response = await client.GetFromJsonAsync<SteamSkinResponse>(SteamUrls.GetSkinsUrl(game.SteamGameId, count, start));

                        if (response is null)
                            throw new Exception("При получении данных с сервера Steam произошла ошибка");


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

                        count = 100;

                        await Task.Delay(rnd.Next(3000, 4000));
                    }
                    catch (Exception ex)
                    {
                        count = rnd.Next(20, 99);
                        start -= 1;
                        _logger.LogError(ex.Message);
                        await Task.Delay(rnd.Next(100000, 150000));
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
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

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
