using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Skins;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Skins;
// ReSharper disable NotAccessedPositionalProperty.Global

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

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public SkinsController(
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

        public record BaseSkinResponse(
            int Id,
            string SkinIconUrl,
            string Title,
            string MarketHashName,
            string MarketUrl);

        public record BaseSkinsResponse(
            int Count,
            IEnumerable<BaseSkinResponse> Skins);

        public record SkinResponse(
            BaseSkinResponse Skin,
            decimal CurrentPrice,
            double Change7D,
            double Change30D,
            bool IsMarked);

        public record SkinsResponse(
            int Count,
            int PagesCount,
            IEnumerable<SkinResponse> Skins);

        public record SkinDynamicResponse(
            int Id,
            DateTime DateUpdate,
            decimal Price);

        public record SkinDynamicStatsResponse(
            double ChangePeriod,
            IEnumerable<SkinDynamicResponse> Dynamic);

        public record SkinPagesCountResponse(
            int Count);

        public record SteamSkinsCountResponse(
            int Count);

        public record SavedSkinsCountResponse(
            int Count);

        [Validator<GetSkinRequestValidator>]
        public record GetSkinRequest(
            int SkinId);

        public record GetBaseSkinsRequest(
            string? Filter);

        [Validator<GetSkinsRequestValidator>]
        public record GetSkinsRequest(
            int? GameId,
            string? Filter,
            SkinOrderName? OrderName,
            bool? IsAscending,
            bool? IsMarked,
            int PageNumber,
            int PageSize);

        [Validator<GetSkinDynamicsRequestValidator>]
        public record GetSkinDynamicsRequest(
            int SkinId,
            DateTime StartDate,
            DateTime EndDate);

        [Validator<GetSkinPagesCountRequestValidator>]
        public record GetSkinPagesCountRequest(
            int? GameId,
            string? Filter,
            bool? IsMarked,
            int PageSize);

        [Validator<GetSteamSkinsCountRequestValidator>]
        public record GetSteamSkinsCountRequest(
            int GameId);

        [Validator<GetSavedSkinsCountRequestValidator>]
        public record GetSavedSkinsCountRequest(
            int? GameId,
            string? Filter,
            bool? IsMarked);

        [Validator<PostSkinRequestValidator>]
        public record PostSkinRequest(
            int GameId,
            string MarketHashName);

        [Validator<SetMarkedSkinRequestValidator>]
        public record SetMarkedSkinRequest(
            int SkinId);

        [Validator<DeleteMarkedSkinRequestValidator>]
        public record DeleteMarkedSkinRequest(
            int SkinId);

        #endregion Records

        #region Methods

        private async Task<SkinResponse> GetSkinResponseAsync(
            Skin skin,
            User user,
            IEnumerable<int> markedSkinsIds,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            List<SkinsDynamic> dynamic30 = await _context.Entry(skin)
                .Collection(x => x.SkinsDynamics)
                .Query()
                .AsNoTracking()
                .Where(x => x.DateUpdate > DateTime.Now.AddDays(-30))
                .OrderBy(x => x.DateUpdate)
                .ToListAsync(cancellationToken);

            List<SkinsDynamic> dynamic7 = dynamic30.Where(x => x.DateUpdate > DateTime.Now.AddDays(-7)).ToList();

            double change7D = (double)(dynamic7.Count == 0
                ? 0
                : (skin.CurrentPrice - dynamic7.First().Price) / dynamic7.First().Price);
            double change30D = (double)(dynamic30.Count == 0
                ? 0
                : (skin.CurrentPrice - dynamic30.First().Price) / dynamic30.First().Price);

            bool isMarked = markedSkinsIds.Any(x => x == skin.Id);

            return new(await _skinService.GetBaseSkinResponseAsync(skin, cancellationToken),
                (decimal)((double)skin.CurrentPrice * currencyExchangeRate),
                change7D,
                change30D,
                isMarked);
        }

        private async Task<IEnumerable<SkinResponse>> GetSkinsResponseAsync(
            IQueryable<Skin> skins,
            User user,
            IEnumerable<int> markedSkinsIds,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            return await Task.WhenAll(skins
                .Include(x => x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30)))
                .AsEnumerable()
                .Select(async x =>
                    new SkinResponse(
                        await _skinService.GetBaseSkinResponseAsync(x, cancellationToken),
                        (decimal)((double)x.CurrentPrice * currencyExchangeRate),
                        x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                            ? (double)((x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                           .OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                       / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                           .OrderBy(y => y.DateUpdate).First().Price)
                            : 0,
                        x.SkinsDynamics.Count != 0
                            ? (double)((x.SkinsDynamics.OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                       / x.SkinsDynamics.OrderBy(y => y.DateUpdate).First().Price)
                            : 0,
                        markedSkinsIds.Any(y => y == x.Id)))
            ).WaitAsync(cancellationToken);
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
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSkinInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SkinResponse>> GetSkinInfo(
            [FromQuery] GetSkinRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Skin skin = await _context.Skins.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Предмета с таким Id не существует");

            await _context.Entry(skin).Reference(x => x.Game).LoadAsync(cancellationToken);

            List<int> markedSkinsIds = await _context.Entry(user)
                .Collection(x => x.MarkedSkins)
                .Query()
                .AsNoTracking()
                .Select(x => x.SkinId)
                .ToListAsync(cancellationToken);

            return Ok(await GetSkinResponseAsync(skin, user, markedSkinsIds, cancellationToken));
        }

        /// <summary>
        /// Получение упрощённого списка предметов
        /// </summary>
        /// <response code="200">Возвращает упрощённый список предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetBaseSkins")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<BaseSkinsResponse>> GetBaseSkins(
            [FromQuery] GetBaseSkinsRequest request,
            CancellationToken cancellationToken = default)
        {
            IQueryable<Skin> skins = _context.Skins
                .AsNoTracking()
                .Include(x => x.Game)
                .Where(x => string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter));

            skins = skins.OrderBy(x => x.Id).Take(20);

            return Ok(new BaseSkinsResponse(await skins.CountAsync(cancellationToken),
                await Task.WhenAll(skins.AsEnumerable()
                        .Select(async x => await _skinService.GetBaseSkinResponseAsync(x, cancellationToken)))
                    .WaitAsync(cancellationToken)));
        }

        /// <summary>
        /// Получение списка предметов
        /// </summary>
        /// <response code="200">Возвращает список предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSkins")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SkinsResponse>> GetSkins(
            [FromQuery] GetSkinsRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            List<int> markedSkinsIds = await _context.Entry(user)
                .Collection(x => x.MarkedSkins)
                .Query()
                .AsNoTracking()
                .Select(x => x.SkinId)
                .ToListAsync(cancellationToken);

            IQueryable<Skin> skins = _context.Skins
                .AsNoTracking()
                .Include(x => x.Game)
                .Where(x =>
                    (request.GameId == null || x.GameId == request.GameId)
                    && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter))
                    && (request.IsMarked == null || request.IsMarked == markedSkinsIds.Any(y => y == x.Id)));

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case SkinOrderName.Title:
                        skins = request.IsAscending.Value
                            ? skins.OrderBy(x => x.Title)
                            : skins.OrderByDescending(x => x.Title);
                        break;
                    case SkinOrderName.Price:
                        skins = request.IsAscending.Value
                            ? skins.OrderBy(x => x.CurrentPrice)
                            : skins.OrderByDescending(x => x.CurrentPrice);
                        break;
                    case SkinOrderName.Change7D:
                        skins = request.IsAscending.Value
                            ? skins.OrderBy(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                ? (double)((x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                               .OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                           / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                               .OrderBy(y => y.DateUpdate).First().Price)
                                : 0)
                            : skins.OrderByDescending(x =>
                                x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                    ? (double)((x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                                   .OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-7))
                                                   .OrderBy(y => y.DateUpdate).First().Price)
                                    : 0);
                        break;
                    case SkinOrderName.Change30D:
                        skins = request.IsAscending.Value
                            ? skins.OrderBy(x => x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                ? (double)((x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                               .OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                           / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                               .OrderBy(y => y.DateUpdate).First().Price)
                                : 0)
                            : skins.OrderByDescending(x =>
                                x.SkinsDynamics.Any(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                    ? (double)((x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                                   .OrderBy(y => y.DateUpdate).First().Price - x.CurrentPrice)
                                               / x.SkinsDynamics.Where(y => y.DateUpdate > DateTime.Now.AddDays(-30))
                                                   .OrderBy(y => y.DateUpdate).First().Price)
                                    : 0);
                        break;
                }
            else
                skins = skins.OrderBy(x => x.Id);

            int skinsCount = await skins.CountAsync(cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)skinsCount / request.PageSize);

            skins = skins.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize);

            return Ok(new SkinsResponse(skinsCount,
                pagesCount == 0 ? 1 : pagesCount,
                await GetSkinsResponseAsync(skins, user, markedSkinsIds, cancellationToken)));
        }

        /// <summary>
        /// Получение динамики стоимости предмета
        /// </summary>
        /// <response code="200">Возвращает динамику стоимости предмета</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSkinDynamics")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SkinDynamicStatsResponse>> GetSkinDynamics(
            [FromQuery] GetSkinDynamicsRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Skin skin = await _context.Skins.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Предмета с таким Id не существует");

            List<SkinDynamicResponse> dynamic = await
                _skinService.GetSkinDynamicsResponseAsync(skin, user, request.StartDate, request.EndDate,
                    cancellationToken);

            double changePeriod = (double)(dynamic.Count == 0
                ? 0
                : (dynamic.Last().Price - dynamic.First().Price) / dynamic.First().Price);

            return Ok(new SkinDynamicStatsResponse(changePeriod, dynamic));
        }

        /// <summary>
        /// Получение количества страниц предметов
        /// </summary>
        /// <response code="200">Возвращает количество страниц определённого размера</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSkinPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SkinPagesCountResponse>> GetSkinPagesCount(
            [FromQuery] GetSkinPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            List<int> markedSkinsIds = [];

            if (request.IsMarked is not null)
                markedSkinsIds = await _context.Entry(user)
                    .Collection(x => x.MarkedSkins)
                    .Query()
                    .AsNoTracking()
                    .Select(x => x.SkinId)
                    .ToListAsync(cancellationToken);

            int count = await _context.Skins.AsNoTracking()
                .CountAsync(x => (request.GameId == null || x.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter))
                                 && (request.IsMarked == null ||
                                     request.IsMarked == markedSkinsIds.Any(y => y == x.Id)),
                    cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

            return Ok(new SkinPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
        }

        /// <summary>
        /// Получение общего количества предметов в Steam
        /// </summary>
        /// <response code="200">Возвращает количество предметов в Steam</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSteamSkinsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SteamSkinsCountResponse>> GetSteamSkinsCount(
            [FromQuery] GetSteamSkinsCountRequest request,
            CancellationToken cancellationToken = default)
        {
            Game game = await _context.Games.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "Игры с таким Id не существует");

            HttpClient client = _httpClientFactory.CreateClient();
            SteamSkinResponse response =
                await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetMostPopularSkinUrl(game.SteamGameId),
                    cancellationToken) ?? throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "При получении данных с сервера Steam произошла ошибка");

            return Ok(new SteamSkinsCountResponse(response.total_count));
        }

        /// <summary>
        /// Получение количества сохранённых предметов
        /// </summary>
        /// <response code="200">Возвращает количество сохранённых предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetSavedSkinsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<SavedSkinsCountResponse>> GetSavedSkinsCount(
            [FromQuery] GetSavedSkinsCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            List<int> markedSkinsIds = [];

            if (request.IsMarked is not null)
                markedSkinsIds = await _context.Entry(user)
                    .Collection(x => x.MarkedSkins)
                    .Query()
                    .AsNoTracking()
                    .Select(x => x.SkinId)
                    .ToListAsync(cancellationToken: cancellationToken);

            int count = await _context.Skins.AsNoTracking()
                .CountAsync(x => (request.GameId == null || x.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Title.Contains(request.Filter))
                                 && (request.IsMarked == null ||
                                     request.IsMarked == markedSkinsIds.Any(y => y == x.Id)),
                    cancellationToken);

            return Ok(new SavedSkinsCountResponse(count));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Занесение одного предмета из Steam
        /// </summary>
        /// <response code="200">Предмет успешно добавлен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Игры с таким Id не существует</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostSkin")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PostSkin(
            PostSkinRequest request,
            CancellationToken cancellationToken = default)
        {
            Game game = await _context.Games.FirstOrDefaultAsync(x => x.Id == request.GameId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status400BadRequest,
                            "Игры с таким Id не существует");

            HttpClient client = _httpClientFactory.CreateClient();

            SteamSkinResponse? response =
                await client.GetFromJsonAsync<SteamSkinResponse>(SteamApi.GetSkinInfoUrl(request.MarketHashName),
                    cancellationToken);

            if (response is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "При получении данных с сервера Steam произошла ошибка");

            SkinResult result = response.results.First();

            if (await _context.Skins.AnyAsync(x => x.MarketHashName == result.asset_description.market_hash_name,
                    cancellationToken))
                throw new HttpResponseException(StatusCodes.Status502BadGateway,
                    "Скин с таким MarketHashName уже присутствует в базе");

            await _context.Skins.AddAsync(new()
            {
                GameId = game.Id,
                MarketHashName = result.asset_description.market_hash_name,
                Title = result.name,
                SkinIconUrl = result.asset_description.icon_url
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Добавление предмета в отмеченные
        /// </summary>
        /// <response code="200">Предмет отмечен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "SetMarkedSkin")]
        public async Task<ActionResult> SetMarkedSkin(
            SetMarkedSkinRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Skin skin = await _context.Skins.FirstOrDefaultAsync(x => x.Id == request.SkinId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Предмета с таким Id не существует");

            MarkedSkin? markedSkin =
                await _context.MarkedSkins.FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

            if (markedSkin is not null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "Скин с таким Id уже добавлен в избранное");

            await _context.MarkedSkins.AddAsync(new()
            {
                SkinId = skin.Id,
                UserId = user.Id
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
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
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteMarkedSkin")]
        public async Task<ActionResult> DeleteMarkedSkin(
            DeleteMarkedSkinRequest request,
            CancellationToken cancellationToken = default)
        {
            MarkedSkin? markedSkin =
                await _context.MarkedSkins.FirstOrDefaultAsync(x => x.SkinId == request.SkinId, cancellationToken);

            if (markedSkin is null)
                throw new HttpResponseException(StatusCodes.Status400BadRequest,
                    "Скина с таким Id в таблице отмеченных скинов нет");

            _context.MarkedSkins.Remove(markedSkin);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
