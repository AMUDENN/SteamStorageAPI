using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
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

        private readonly ILogger<ActivesController> _logger;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ActiveOrderName, Func<Active, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public ActivesController(
            ILogger<ActivesController> logger, 
            ISkinService skinService, 
            IUserService userService,
            SteamStorageContext context)
        {
            _logger = logger;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                // TODO: Сортировка по параметрам!
            };
        }

        #endregion Constructor

        #region Records

        public record ActiveResponse(
            int Id,
            BaseSkinResponse Skin,
            int Count,
            decimal BuyPrice,
            decimal CurrentPrice,
            decimal CurrentSum,
            double Change);

        public record ActivesResponse(
            int ActivesCount,
            int PagesCount,
            IEnumerable<ActiveResponse> Skins);

        public record ActivesPagesCountResponse(
            int Count);

        public record ActivesCountResponse(
            int Count);

        public record GetActivesRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            ActiveOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        public record GetActivesPagesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            int PageSize);

        public record GetActivesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        public record PostActiveRequest(
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal? GoalPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate);

        public record PutActiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal? GoalPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate);

        public record SoldActiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal SoldPrice,
            DateTime SoldDate,
            string? Description);

        public record DeleteActiveRequest(int Id);

        #endregion Records

        #region Methods

        private async Task<ActiveResponse> GetActiveResponseAsync(
            Active active,
            CancellationToken cancellationToken = default)
        {
            decimal currentPrice = await _skinService.GetCurrentPriceAsync(active.Skin, cancellationToken);

            return new(active.Id,
                await _skinService.GetBaseSkinResponseAsync(active.Skin, cancellationToken),
                active.Count,
                active.BuyPrice,
                currentPrice,
                currentPrice * active.Count,
                (double)(active.BuyPrice == 0
                    ? 0
                    : (await _skinService.GetCurrentPriceAsync(active.Skin, cancellationToken) - active.BuyPrice) /
                      active.BuyPrice));
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
        [HttpGet(Name = "GetActives")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesResponse>> GetActives(
            [FromQuery] GetActivesRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.PageNumber <= 0 || request.PageSize <= 0)
                    throw new("Размер и номер страницы не могут быть меньше или равны нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<Active> actives = _context.Entry(user)
                    .Collection(x => x.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Actives)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                && (request.GroupId == null || x.GroupId == request.GroupId));

                if (request is { OrderName: not null, IsAscending: not null })
                    actives = (bool)request.IsAscending
                        ? actives.OrderBy(_orderNames[(ActiveOrderName)request.OrderName])
                        : actives.OrderByDescending(_orderNames[(ActiveOrderName)request.OrderName]);

                int activesCount = await _context
                    .Entry(user)
                    .Collection(x => x.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Actives).CountAsync(x =>
                        (request.GameId == null || x.Skin.GameId == request.GameId)
                        && (string.IsNullOrEmpty(request.Filter) ||
                            x.Skin.Title.Contains(request.Filter!))
                        && (request.GroupId == null ||
                            x.GroupId == request.GroupId), cancellationToken);

                int pagesCount = (int)Math.Ceiling((double)activesCount / request.PageSize);

                actives = actives.Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize);

                return Ok(new ActivesResponse(activesCount, pagesCount == 0 ? 1 : pagesCount,
                    actives.Select(x => GetActiveResponseAsync(x, cancellationToken).Result)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества страниц активов
        /// </summary>
        /// <response code="200">Возвращает количество страниц активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetActivesPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesPagesCountResponse>> GetActivesPagesCount(
            [FromQuery] GetActivesPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (request.PageSize <= 0)
                    throw new("Размер страницы не может быть меньше или равен нулю.");

                if (request.PageSize > 200)
                    throw new("Размер страницы не может превышать 200 предметов");

                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                int count = await _context
                    .Entry(user)
                    .Collection(x => x.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Actives).CountAsync(x =>
                        (request.GameId == null || x.Skin.GameId == request.GameId)
                        && (string.IsNullOrEmpty(request.Filter) ||
                            x.Skin.Title.Contains(request.Filter!))
                        && (request.GroupId == null ||
                            x.GroupId == request.GroupId), cancellationToken);

                int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

                return Ok(new ActivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества активов
        /// </summary>
        /// <response code="200">Возвращает количество активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetActivesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActivesCountResponse>> GetActivesCount(
            [FromQuery] GetActivesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ActivesCountResponse(await _context
                    .Entry(user)
                    .Collection(x => x.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Actives)
                    .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                     && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                     && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken)));
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
        /// Добавление актива
        /// </summary>
        /// <response code="200">Актив успешно добавлен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        [HttpPost(Name = "PostActive")]
        public async Task<ActionResult> PostActive(
            PostActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                        .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                    return NotFound("Скина с таким Id не существует");

                _context.Actives.Add(new()
                {
                    GroupId = request.GroupId,
                    Count = request.Count,
                    BuyPrice = request.BuyPrice,
                    GoalPrice = request.GoalPrice,
                    SkinId = request.SkinId,
                    Description = request.Description,
                    BuyDate = request.BuyDate
                });

                await _context.SaveChangesAsync(cancellationToken);

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

        #region PUT

        /// <summary>
        /// Изменение актива
        /// </summary>
        /// <response code="200">Актив успешно изменён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Актива с таким Id не существует, группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "PutActive")]
        public async Task<ActionResult> PutActive(
            PutActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = await _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .SelectMany(x => x.Actives)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                        .AnyAsync(x => x.Id == request.GroupId, cancellationToken: cancellationToken))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                    return NotFound("Скина с таким Id не существует");

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
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Продажа актива
        /// </summary>
        /// <response code="200">Актив успешно продан</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Актива с таким Id не существует, группы архива с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "SoldActive")]
        public async Task<ActionResult> SoldActive(
            SoldActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = await _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .SelectMany(x => x.Actives)
                    .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                        .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

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
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpDelete(Name = "DeleteActive")]
        public async Task<ActionResult> DeleteActive(
            DeleteActiveRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = await _context.Entry(user).Collection(u => u.ActiveGroups).Query()
                    .Include(x => x.Actives)
                    .SelectMany(x => x.Actives).FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                _context.Actives.Remove(active);

                await _context.SaveChangesAsync(cancellationToken);

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
