using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;

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
            Title,
            Price,
            Change7D,
            Change30D
        }

        #endregion Enums

        #region Fields

        private readonly ILogger<SkinsController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public SkinsController(ILogger<SkinsController> logger, IHttpClientFactory httpClientFactory,
            ISkinService skinService, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _skinService = skinService;
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record BaseSkinResponse(
            int Id,
            string SkinIconUrl,
            string Title,
            string MarketHashName,
            string MarketUrl);

        public record SkinResponse(
            BaseSkinResponse Skin,
            decimal CurrentPrice,
            double Change7D,
            double Change30D,
            bool IsMarked);

        public record SkinsResponse(
            int SkinsCount,
            int PagesCount,
            IEnumerable<SkinResponse> Skins);

        public record SkinDynamicStatsResponse(
            double ChangePeriod,
            IEnumerable<SkinDynamicResponse> Dynamic);

        public record SkinDynamicResponse(
            int Id,
            DateTime DateUpdate,
            decimal Price);

        public record SkinPagesCountResponse(
            int Count);

        public record SteamSkinsCountResponse(
            int Count);

        public record SavedSkinsCountResponse(
            int Count);

        public record GetSkinRequest(
            int SkinId);

        public record GetSkinsRequest(
            int? GameId,
            string? Filter,
            SkinOrderName? OrderName,
            bool? IsAscending,
            bool? IsMarked,
            int PageNumber,
            int PageSize);

        public record GetSkinDynamicsRequest(
            int SkinId,
            DateTime StartDate,
            DateTime EndDate);

        public record GetSkinPagesCountRequest(
            int? GameId,
            string? Filter,
            bool? IsMarked,
            int PageSize);

        public record GetSteamSkinsCountRequest(
            int GameId);

        public record GetSavedSkinsCountRequest(
            int? GameId,
            string? Filter,
            bool? IsMarked);

        public record PostSkinsRequest(
            int GameId);

        public record PostSkinRequest(
            int GameId,
            string MarketHashName);

        public record SetMarkedSkinRequest(
            int SkinId);

        public record DeleteMarkedSkinRequest(
            int SkinId);

        #endregion Records

        #region Methods

        private SkinResponse GetSkinResponse(Skin skin, IEnumerable<int> markedSkinsIds)
        {
            List<SkinsDynamic> dynamics = _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .OrderBy(x => x.DateUpdate)
                .ToList();

            List<SkinsDynamic> dynamic7 = dynamics.Where(x => x.DateUpdate > DateTime.Now.AddDays(-7)).ToList();
            List<SkinsDynamic> dynamic30 = dynamics.Where(x => x.DateUpdate > DateTime.Now.AddDays(-30)).ToList();

            decimal currentPrice = dynamics.Count == 0 ? 0 : dynamics.Last().Price;

            double change7D = (double)(dynamic7.Count == 0
                ? 0
                : (currentPrice - dynamic7.First().Price) / dynamic7.First().Price);
            double change30D = (double)(dynamic30.Count == 0
                ? 0
                : (currentPrice - dynamic30.First().Price) / dynamic30.First().Price);

            bool isMarked = markedSkinsIds.Any(x => x == skin.Id);

            return new(_skinService.GetBaseSkinResponse(skin), currentPrice, change7D, change30D, isMarked);
        }

        private IEnumerable<SkinResponse> GetSkinResponse(IEnumerable<Skin> skins, IEnumerable<int> markedSkinsIds)
        {
            var skinsResult = skins.GroupJoin(
                _context.SkinsDynamics
                    .GroupBy(sd => sd.SkinId)
                    .Select(g => new
                    {
                        SkinID = g.Key,
                        LastPrice = g.Any() ? g.OrderByDescending(sd => sd.DateUpdate).First().Price : 0,
                        Change7D = g.Count(sd => sd.DateUpdate > DateTime.Now.AddDays(-7)) > 1
                            ? (double)((g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-7))
                                            .OrderByDescending(sd => sd.DateUpdate).First().Price -
                                        g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-7))
                                            .OrderBy(sd => sd.DateUpdate).First().Price) /
                                       g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-7))
                                           .OrderBy(sd => sd.DateUpdate).First().Price)
                            : 0,
                        Change30D = g.Count(sd => sd.DateUpdate > DateTime.Now.AddDays(-30)) > 1
                            ? (double)((g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-30))
                                            .OrderByDescending(sd => sd.DateUpdate).First().Price -
                                        g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-30))
                                            .OrderBy(sd => sd.DateUpdate).First().Price) /
                                       g.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-30))
                                           .OrderBy(sd => sd.DateUpdate).First().Price)
                            : 0
                    }), s => s.Id, d => d.SkinID,
                (s, d) => new
                {
                    Skin = s,
                    LastPrice = d.Any() ? d.First().LastPrice : 0,
                    Change7D = d.Any() ? d.First().Change7D : 0,
                    Change30D = d.Any() ? d.First().Change30D : 0
                });
            return skinsResult.Select(x => new SkinResponse(_skinService.GetBaseSkinResponse(x.Skin), x.LastPrice,
                x.Change7D, x.Change30D, markedSkinsIds.Any(y => y == x.Skin.Id)));
        }

        #endregion Methods

        #region GET
        
        /// <summary>
        /// Получение информации об одном предмете
        /// </summary>
        /// <response code="200">Возвращает подробную информацию о предмете</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
        [HttpGet(Name = "GetSkinInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<SkinResponse> GetSkinInfo([FromQuery] GetSkinRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Skin? skin = _context.Skins.FirstOrDefault(x => x.Id == request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                List<int> markedSkinsIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query()
                    .Select(x => x.SkinId).ToList();

                return Ok(GetSkinResponse(skin, markedSkinsIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение списка предметов
        /// </summary>
        /// <response code="200">Возвращает список предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetSkins")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<SkinsResponse> GetSkins([FromQuery] GetSkinsRequest request)
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

                List<int> markedSkinsIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query()
                    .Select(x => x.SkinId).ToList();

                IQueryable<Skin> skins = _context.Skins.Where(x =>
                    (request.GameId == null || x.GameId == request.GameId)
                    && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter!))
                    && (request.IsMarked == null || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)));

                if (request is { OrderName: not null, IsAscending: not null })
                    switch (request.OrderName)
                    {
                        case SkinOrderName.Title:
                            skins = (bool)request.IsAscending
                                ? skins.OrderBy(x => x.Title)
                                : skins.OrderByDescending(x => x.Title);
                            break;
                        case SkinOrderName.Price:
                            var skinsPriceResult = skins.GroupJoin(
                                _context.SkinsDynamics.GroupBy(sd => sd.SkinId)
                                    .Select(g => new
                                    {
                                        SkinID = g.Key,
                                        LastPrice = g.Any() ? g.OrderByDescending(sd => sd.DateUpdate).First().Price : 0
                                    }), s => s.Id, d => d.SkinID,
                                (s, d) => new { Skin = s, LastPrice = d.Any() ? d.First().LastPrice : 0 });
                            skins = ((bool)request.IsAscending
                                    ? skinsPriceResult
                                        .OrderBy(result => result.LastPrice)
                                    : skinsPriceResult
                                        .OrderByDescending(result => result.LastPrice))
                                .Select(result => result.Skin);
                            break;
                        case SkinOrderName.Change7D:
                            var skinsChange7DResult = skins.GroupJoin(
                                _context.SkinsDynamics.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-7))
                                    .GroupBy(sd => sd.SkinId)
                                    .Select(g => new
                                    {
                                        SkinID = g.Key,
                                        Change7D = g.Any()
                                            ? (g.OrderByDescending(sd => sd.DateUpdate).First().Price -
                                               g.OrderBy(sd => sd.DateUpdate).First().Price) /
                                              g.OrderBy(sd => sd.DateUpdate).First().Price
                                            : 0
                                    }), s => s.Id, d => d.SkinID,
                                (s, d) => new { Skin = s, Change7D = d.Any() ? d.First().Change7D : 0 });
                            skins = ((bool)request.IsAscending
                                    ? skinsChange7DResult
                                        .OrderBy(result => result.Change7D)
                                    : skinsChange7DResult
                                        .OrderByDescending(result => result.Change7D))
                                .Select(result => result.Skin);
                            break;
                        case SkinOrderName.Change30D:
                            var skinsChange30DResult = skins.GroupJoin(
                                _context.SkinsDynamics.Where(sd => sd.DateUpdate > DateTime.Now.AddDays(-30))
                                    .GroupBy(sd => sd.SkinId)
                                    .Select(g => new
                                    {
                                        SkinID = g.Key,
                                        Change30D = g.Any()
                                            ? (g.OrderByDescending(sd => sd.DateUpdate).First().Price -
                                               g.OrderBy(sd => sd.DateUpdate).First().Price) /
                                              g.OrderBy(sd => sd.DateUpdate).First().Price
                                            : 0
                                    }), s => s.Id, d => d.SkinID,
                                (s, d) => new { Skin = s, Change30D = d.Any() ? d.First().Change30D : 0 });
                            skins = ((bool)request.IsAscending
                                    ? skinsChange30DResult
                                        .OrderBy(result => result.Change30D)
                                    : skinsChange30DResult
                                        .OrderByDescending(result => result.Change30D))
                                .Select(result => result.Skin);
                            break;
                    }

                int skinsCount = skins.Count();

                int pagesCount = (int)Math.Ceiling((double)skinsCount / request.PageSize);

                List<Skin> resultSkins = skins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
                    .ToList();

                return Ok(new SkinsResponse(skinsCount, pagesCount == 0 ? 1 : pagesCount,
                    GetSkinResponse(resultSkins, markedSkinsIds)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение динамики стоимости предмета
        /// </summary>
        /// <response code="200">Возвращает динамику стоимости предмета</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
        [HttpGet(Name = "GetSkinDynamics")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<SkinDynamicStatsResponse> GetSkinDynamics(
            [FromQuery] GetSkinDynamicsRequest request)
        {
            try
            {
                Skin? skin = _context.Skins.FirstOrDefault(x => x.Id == request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                List<SkinDynamicResponse> dynamic =
                    _skinService.GetSkinDynamicsResponse(skin, request.StartDate, request.EndDate);

                double changePeriod = (double)(dynamic.Count == 0
                    ? 0
                    : (dynamic.Last().Price - dynamic.First().Price) / dynamic.First().Price);

                return Ok(new SkinDynamicStatsResponse(changePeriod, dynamic));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества страниц предметов
        /// </summary>
        /// <response code="200">Возвращает количество страниц определённого размера</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetSkinPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<SkinPagesCountResponse> GetSkinPagesCount([FromQuery] GetSkinPagesCountRequest request)
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

                List<int> markedSkinsIds = [];

                if (request.IsMarked is not null)
                    markedSkinsIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Select(x => x.SkinId)
                        .ToList();

                int count = _context.Skins.Count(x => (request.GameId == null || x.GameId == request.GameId)
                                                      && (string.IsNullOrEmpty(request.Filter) ||
                                                          x.Title.Contains(request.Filter!))
                                                      && (request.IsMarked == null || request.IsMarked ==
                                                          markedSkinsIds.Any(y => y == x.Id)));

                int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

                return Ok(new SkinPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение общего количества предметов в Steam
        /// </summary>
        /// <response code="200">Возвращает количество предметов в Steam</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует или пользователь не найден</response>
        [HttpGet(Name = "GetSteamSkinsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SteamSkinsCountResponse>> GetSteamSkinsCount(
            [FromQuery] GetSteamSkinsCountRequest request)
        {
            try
            {
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();
                SteamSkinResponse? response =
                    await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetSkinsUrl(game.SteamGameId, 1, 0));

                if (response is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                return Ok(new SteamSkinsCountResponse(response.total_count));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества сохранённых предметов
        /// </summary>
        /// <response code="200">Возвращает количество сохранённых предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetSavedSkinsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<SavedSkinsCountResponse> GetSavedSkinsCount([FromQuery] GetSavedSkinsCountRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<int> markedSkinsIds = [];

                if (request.IsMarked is not null)
                    markedSkinsIds = _context.Entry(user).Collection(x => x.MarkedSkins).Query().Select(x => x.SkinId)
                        .ToList();

                int count = _context.Skins.Count(x => (request.GameId == null || x.GameId == request.GameId)
                                                      && (string.IsNullOrEmpty(request.Filter) ||
                                                          x.Title.Contains(request.Filter!))
                                                      && (request.IsMarked == null || request.IsMarked ==
                                                          markedSkinsIds.Any(y => y == x.Id)));

                return Ok(new SavedSkinsCountResponse(count));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Занесение списка предметов из Steam
        /// </summary>
        /// <response code="200">Предметы успешно добавлены</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует</response>
        [HttpPost(Name = "PostSkins")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
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

                SteamSkinResponse? response =
                    await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetSkinsUrl(game.SteamGameId, 1, 0));

                if (response is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                int totalCount = response.total_count;

                Random rnd = new();

                while (count == answerCount || start < totalCount)
                {
                    try
                    {
                        _logger.LogInformation(
                            $"Процесс выполнения загрузки скинов:\nЗагружено: {start} / {totalCount}");


                        response = await client.GetFromJsonAsync<SteamSkinResponse>(
                            SteamApi.GetSkinsUrl(game.SteamGameId, count, start));

                        if (response is null)
                            throw new("При получении данных с сервера Steam произошла ошибка");


                        List<Skin> skins = [];

                        foreach (SkinResult item in response.results)
                        {
                            if (_context.Skins.Any(x => x.MarketHashName == item.hash_name))
                                continue;

                            skins.Add(new()
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

        /// <summary>
        /// Занесение одного предмета из Steam
        /// </summary>
        /// <response code="200">Предмет успешно добавлен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует</response>
        [HttpPost(Name = "PostSkin")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PostSkin(PostSkinRequest request)
        {
            try
            {
                Game? game = _context.Games.FirstOrDefault(x => x.Id == request.GameId);

                if (game is null)
                    return NotFound("Игры с таким Id не существует");

                HttpClient client = _httpClientFactory.CreateClient();

                SteamSkinResponse? response =
                    await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetSkinInfoUrl(request.MarketHashName));

                if (response is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                SkinResult result = response.results.First();

                await _context.Skins.AddAsync(new()
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

        /// <summary>
        /// Добавление предмета в отмеченные
        /// </summary>
        /// <response code="200">Предмет отмечен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
        [HttpPost(Name = "SetMarkedSkin")]
        public async Task<ActionResult> SetMarkedSkin(SetMarkedSkinRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Skin? skin = _context.Skins.FirstOrDefault(x => x.Id == request.SkinId);

                if (skin is null)
                    return NotFound("Скина с таким Id не существует");

                MarkedSkin? markedSkin = _context.MarkedSkins.FirstOrDefault(x => x.SkinId == request.SkinId);

                if (markedSkin is not null)
                    return NotFound("Скин с таким Id уже добавлен в избранное");

                await _context.MarkedSkins.AddAsync(new()
                {
                    SkinId = skin.Id,
                    UserId = user.Id
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

        #region DELETE

        /// <summary>
        /// Удаление отмеченного предмета
        /// </summary>
        /// <response code="200">Отметка предмета снята</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id в таблице отмеченных предметов нет или пользователь не найден</response>
        [HttpDelete(Name = "DeleteMarkedSkin")]
        public async Task<ActionResult> DeleteMarkedSkin(DeleteMarkedSkinRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                MarkedSkin? markedSkin = _context.MarkedSkins.FirstOrDefault(x => x.SkinId == request.SkinId);

                if (markedSkin is null)
                    return NotFound("Скина с таким Id в таблице отмеченных скинов нет");

                _context.MarkedSkins.Remove(markedSkin);

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

        #endregion DELETE
    }
}
