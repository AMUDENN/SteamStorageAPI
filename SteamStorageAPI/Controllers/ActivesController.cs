using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Actives;
using static SteamStorageAPI.Controllers.SkinsController;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class ActivesController : ControllerBase
    {
        #region Enums

        public enum ActiveOrderName
        {
            Title,
            Count,
            BuyPrice,
            CurrentPrice,
            CurrentSum,
            Change
        }

        #endregion Enums

        #region Fields

        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public ActivesController(
            ISkinService skinService,
            IUserService userService,
            ICurrencyService currencyService,
            SteamStorageContext context)
        {
            _skinService = skinService;
            _userService = userService;
            _currencyService = currencyService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record ActiveResponse(
            int Id,
            int GroupId,
            BaseSkinResponse Skin,
            DateTime BuyDate,
            int Count,
            decimal BuyPrice,
            decimal CurrentPrice,
            decimal CurrentSum,
            decimal? GoalPrice,
            double? GoalPriceCompletion,
            double Change,
            string? Description);

        public record ActivesResponse(
            int Count,
            int PagesCount,
            IEnumerable<ActiveResponse> Actives);

        public record ActivesStatisticResponse(
            int ActivesCount,
            decimal InvestmentSum,
            decimal CurrentSum);

        public record ActivesPagesCountResponse(
            int Count);

        public record ActivesCountResponse(
            int Count);

        [Validator<GetActivesRequestValidator>]
        public record GetActivesRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            ActiveOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        [Validator<GetActivesStatisticRequestValidator>]
        public record GetActivesStatisticRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        [Validator<GetActivesPagesCountRequestValidator>]
        public record GetActivesPagesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            int PageSize);

        [Validator<GetActivesCountRequestValidator>]
        public record GetActivesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        [Validator<PostActiveRequestValidator>]
        public record PostActiveRequest(
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal? GoalPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate);

        [Validator<PutActiveRequestValidator>]
        public record PutActiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal? GoalPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate);

        [Validator<SoldActiveRequestValidator>]
        public record SoldActiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal SoldPrice,
            DateTime SoldDate,
            string? Description);

        [Validator<DeleteActiveRequestValidator>]
        public record DeleteActiveRequest(int Id);

        #endregion Records

        #region Methods

        private async Task<ActivesResponse> GetActivesResponseAsync(
            IQueryable<Active> actives,
            int pageNumber,
            int pageSize,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            int activesCount = await actives.CountAsync(cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)activesCount / pageSize);

            actives = actives.AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new(activesCount,
                pagesCount,
                await Task.WhenAll(actives.AsEnumerable()
                    .Select(async x =>
                        new ActiveResponse(
                            x.Id,
                            x.GroupId,
                            await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                            x.BuyDate,
                            x.Count,
                            x.BuyPrice,
                            (decimal)((double)x.Skin.CurrentPrice * currencyExchangeRate),
                            (decimal)((double)x.Skin.CurrentPrice * currencyExchangeRate * x.Count),
                            x.GoalPrice,
                            x.GoalPrice == null
                                ? null
                                : (double)x.Skin.CurrentPrice * currencyExchangeRate / (double)x.GoalPrice,
                            ((double)x.Skin.CurrentPrice * currencyExchangeRate - (double)x.BuyPrice) /
                            (double)x.BuyPrice,
                            x.Description)
                    )).WaitAsync(cancellationToken)
            );
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка активов
        /// </summary>
        /// <response code="200">Возвращает список активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActives")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesResponse>> GetActives(
            [FromQuery] GetActivesRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .ThenInclude(x => x.Game)
                .SelectMany(x => x.Actives)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                            && (request.GroupId == null || x.GroupId == request.GroupId));

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ActiveOrderName.Title:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => x.Skin.Title)
                            : actives.OrderByDescending(x => x.Skin.Title);
                        break;
                    case ActiveOrderName.Count:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => x.Count)
                            : actives.OrderByDescending(x => x.Count);
                        break;
                    case ActiveOrderName.BuyPrice:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => x.BuyPrice)
                            : actives.OrderByDescending(x => x.BuyPrice);
                        break;
                    case ActiveOrderName.CurrentPrice:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => x.Skin.CurrentPrice)
                            : actives.OrderByDescending(x => x.Skin.CurrentPrice);
                        break;
                    case ActiveOrderName.CurrentSum:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => x.Skin.CurrentPrice * x.Count)
                            : actives.OrderByDescending(x => x.Skin.CurrentPrice * x.Count);
                        break;
                    case ActiveOrderName.Change:
                        actives = request.IsAscending.Value
                            ? actives.OrderBy(x => (x.Skin.CurrentPrice - x.BuyPrice) / x.BuyPrice)
                            : actives.OrderByDescending(x => (x.Skin.CurrentPrice - x.BuyPrice) / x.BuyPrice);
                        break;
                }
            else
                actives = actives.OrderBy(x => x.Id);

            return Ok(await GetActivesResponseAsync(actives,
                request.PageNumber,
                request.PageSize,
                user,
                cancellationToken));
        }

        /// <summary>
        /// Получение статистики по выборке активов
        /// </summary>
        /// <response code="200">Возвращает статистику по выборке активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActivesStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesStatisticResponse>> GetActivesStatistic(
            [FromQuery] GetActivesStatisticRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                            && (request.GroupId == null || x.GroupId == request.GroupId));

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            return Ok(new ActivesStatisticResponse(
                actives.Sum(x => x.Count),
                actives.Sum(x => x.BuyPrice * x.Count),
                (decimal)((double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate)
            ));
        }

        /// <summary>
        /// Получение количества страниц активов
        /// </summary>
        /// <response code="200">Возвращает количество страниц активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActivesPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesPagesCountResponse>> GetActivesPagesCount(
            [FromQuery] GetActivesPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            int count = await _context
                .Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                                 && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

            return Ok(new ActivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
        }

        /// <summary>
        /// Получение количества активов
        /// </summary>
        /// <response code="200">Возвращает количество активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActivesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesCountResponse>> GetActivesCount(
            [FromQuery] GetActivesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new ActivesCountResponse(await _context
                .Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                                 && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken)));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление актива
        /// </summary>
        /// <response code="200">Актив успешно добавлен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpPost(Name = "PostActive")]
        public async Task<ActionResult> PostActive(
            PostActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                    .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Предмета с таким Id не существует");

            await _context.Actives.AddAsync(new()
            {
                GroupId = request.GroupId,
                Count = request.Count,
                BuyPrice = request.BuyPrice,
                GoalPrice = request.GoalPrice,
                SkinId = request.SkinId,
                Description = request.Description,
                BuyDate = request.BuyDate
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение актива
        /// </summary>
        /// <response code="200">Актив успешно изменён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Актива с таким Id не существует, группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpPut(Name = "PutActive")]
        public async Task<ActionResult> PutActive(
            PutActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Active active = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .Include(x => x.Actives)
                                .SelectMany(x => x.Actives)
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ??
                            throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

            if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                    .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Предмета с таким Id не существует");

            active.GroupId = request.GroupId;
            active.Count = request.Count;
            active.BuyPrice = request.BuyPrice;
            active.GoalPrice = request.GoalPrice;
            active.SkinId = request.SkinId;
            active.Description = request.Description;
            active.BuyDate = request.BuyDate;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Продажа актива
        /// </summary>
        /// <response code="200">Актив успешно продан</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Актива с таким Id не существует, группы архива с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpPut(Name = "SoldActive")]
        public async Task<ActionResult> SoldActive(
            SoldActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Active active = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .Include(x => x.Actives)
                                .SelectMany(x => x.Actives)
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ??
                            throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

            if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                    .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            await _context.Archives.AddAsync(new()
            {
                GroupId = request.GroupId,
                SkinId = active.SkinId,
                Count = request.Count > active.Count ? active.Count : request.Count,
                BuyDate = active.BuyDate,
                BuyPrice = active.BuyPrice,
                SoldDate = request.SoldDate,
                SoldPrice = request.SoldPrice
            }, cancellationToken);

            if (request.Count >= active.Count)
                _context.Actives.Remove(active);
            else
                active.Count -= request.Count;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление актива
        /// </summary>
        /// <response code="200">Актив успешно удалён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Актива с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpDelete(Name = "DeleteActive")]
        public async Task<ActionResult> DeleteActive(
            DeleteActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Active active = await _context.Entry(user)
                                .Collection(u => u.ActiveGroups)
                                .Query()
                                .Include(x => x.Actives)
                                .SelectMany(x => x.Actives)
                                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ??
                            throw new HttpResponseException(StatusCodes.Status404NotFound,
                                "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

            _context.Actives.Remove(active);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
