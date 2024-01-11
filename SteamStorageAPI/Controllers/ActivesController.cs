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
            Title, Count, BuyPrice, CurrentPrice, CurrentSum, Change
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
        public ActivesController(ILogger<ActivesController> logger, ISkinService skinService, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [ActiveOrderName.Title] = x => x.Skin.Title,
                [ActiveOrderName.Count] = x => x.Count,
                [ActiveOrderName.BuyPrice] = x => x.BuyPrice,
                [ActiveOrderName.CurrentPrice] = x => _skinService.GetCurrentPrice(x.Skin),
                [ActiveOrderName.CurrentSum] = x => x.Count * _skinService.GetCurrentPrice(x.Skin),
                [ActiveOrderName.Change] = x => x.BuyPrice == 0 ? 0 : (_skinService.GetCurrentPrice(x.Skin) - x.BuyPrice) / x.BuyPrice
            };
        }
        #endregion Constructor

        #region Records
        public record ActiveResponse(int Id, BaseSkinResponse Skin, int Count, decimal BuyPrice, decimal CurrentPrice, decimal CurrentSum, double Change);
        public record ActivesPagesCountRespose(int Count);
        public record ActivesCountResponse(int Count);
        public record GetActivesRequest(int? GroupId, int? GameId, string? Filter, ActiveOrderName? OrderName, bool? IsAscending, int PageNumber, int PageSize);
        public record GetActivesPagesCountRequest(int? GroupId, int? GameId, string? Filter, int PageSize);
        public record GetActivesCountRequest(int? GroupId, int? GameId, string? Filter);
        public record PostActiveRequest(int GroupId, int Count, decimal BuyPrice, decimal? GoalPrice, int SkinId, string? Description, DateTime BuyDate);
        public record PutActiveRequest(int Id, int GroupId, int Count, decimal BuyPrice, decimal? GoalPrice, int SkinId, string? Description, DateTime BuyDate);
        public record SoldActiveRequest(int Id, int GroupId, int Count, decimal SoldPrice, DateTime SoldDate, string? Description);
        public record DeleteActiveRequest(int Id);
        #endregion Records

        #region Methods
        private ActiveResponse GetActiveResponse(Active active)
        {
            decimal currentPrice = _skinService.GetCurrentPrice(active.Skin);

            return new ActiveResponse(active.Id,
                                       _skinService.GetBaseSkinResponse(active.Skin),
                                       active.Count,
                                       active.BuyPrice,
                                       currentPrice,
                                       currentPrice * active.Count,
                                       (double)(active.BuyPrice == 0 ? 0 : (_skinService.GetCurrentPrice(active.Skin) - active.BuyPrice) / active.BuyPrice));
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetActives")]
        public ActionResult<IEnumerable<ActiveResponse>> GetActives([FromQuery] GetActivesRequest request)
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

                IEnumerable<Active>? actives = _context.Entry(user)
                                                       .Collection(x => x.ActiveGroups)
                                                       .Query()
                                                       .Include(x => x.Actives)
                                                       .ThenInclude(x => x.Skin)
                                                       .SelectMany(x => x.Actives)
                                                       .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                                                && (request.GroupId == null || x.GroupId == request.GroupId));

                if (request.OrderName != null && request.IsAscending != null)
                    actives = (bool)request.IsAscending ? actives.OrderBy(_orderNames[(ActiveOrderName)request.OrderName])
                                                        : actives.OrderByDescending(_orderNames[(ActiveOrderName)request.OrderName]);

                actives = actives.Skip((request.PageNumber - 1) * request.PageSize)
                                 .Take(request.PageSize);

                return Ok(actives.Select(GetActiveResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetActivesPagesCount")]
        public ActionResult<ActivesPagesCountRespose> GetActivesPagesCount([FromQuery] GetActivesPagesCountRequest request)
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

                IEnumerable<Active>? actives = _context.Entry(user)
                                                       .Collection(x => x.ActiveGroups)
                                                       .Query()
                                                       .Include(x => x.Actives)
                                                       .ThenInclude(x => x.Skin)
                                                       .SelectMany(x => x.Actives)
                                                       .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                                                && (request.GroupId == null || x.GroupId == request.GroupId));

                return Ok(new ActivesPagesCountRespose((int)Math.Ceiling((double)actives.Count() / request.PageSize)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetActivesCount")]
        public ActionResult<ActivesCountResponse> GetActivesCount([FromQuery] GetActivesCountRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ActivesCountResponse(_context.Entry(user)
                                                               .Collection(x => x.ActiveGroups)
                                                               .Query()
                                                               .Include(x => x.Actives)
                                                               .ThenInclude(x => x.Skin)
                                                               .SelectMany(x => x.Actives)
                                                               .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                                                        && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                                                        && (request.GroupId == null || x.GroupId == request.GroupId))
                                                            .Count()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET

        #region POST
        [HttpPost(Name = "PostActive")]
        public async Task<ActionResult> PostActive(PostActiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Entry(user).Collection(x => x.ActiveGroups).Query().Any(x => x.Id == request.GroupId))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                if (!_context.Skins.Any(x => x.Id == request.SkinId))
                    return NotFound("Скина с таким Id не существует");

                _context.Actives.Add(new Active()
                {
                    GroupId = request.GroupId,
                    Count = request.Count,
                    BuyPrice = request.BuyPrice,
                    GoalPrice = request.GoalPrice,
                    SkinId = request.SkinId,
                    Description = request.Description,
                    BuyDate = request.BuyDate
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

        #region PUT
        [HttpPut(Name = "PutActive")]
        public async Task<ActionResult> PutActive(PutActiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = _context.Entry(user)
                                         .Collection(u => u.ActiveGroups)
                                         .Query()
                                         .Include(x => x.Actives)
                                         .SelectMany(x => x.Actives)
                                         .FirstOrDefault(x => x.Id == request.Id);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                if (!_context.Entry(user).Collection(x => x.ActiveGroups).Query().Any(x => x.Id == request.GroupId))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                if (!_context.Skins.Any(x => x.Id == request.SkinId))
                    return NotFound("Скина с таким Id не существует");

                active.GroupId = request.GroupId;
                active.Count = request.Count;
                active.BuyPrice = request.BuyPrice;
                active.GoalPrice = request.GoalPrice;
                active.SkinId = request.SkinId;
                active.Description = request.Description;
                active.BuyDate = request.BuyDate;

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

        [HttpPut(Name = "SoldActive")]
        public async Task<ActionResult> SoldActive(SoldActiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = _context.Entry(user)
                                         .Collection(u => u.ActiveGroups)
                                         .Query()
                                         .Include(x => x.Actives)
                                         .SelectMany(x => x.Actives)
                                         .FirstOrDefault(x => x.Id == request.Id);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                if (!_context.Entry(user).Collection(x => x.ArchiveGroups).Query().Any(x => x.Id == request.GroupId))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                _context.Archives.Add(new Archive()
                {
                    GroupId = request.GroupId,
                    SkinId = active.SkinId,
                    Count = request.Count > active.Count ? active.Count : request.Count,
                    BuyDate = active.BuyDate,
                    BuyPrice = active.BuyPrice,
                    SoldDate = request.SoldDate,
                    SoldPrice = request.SoldPrice
                });

                if (request.Count >= active.Count)
                    _context.Actives.Remove(active);
                else
                    active.Count -= request.Count;

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
        #endregion PUT

        #region DELETE
        [HttpDelete(Name = "DeleteActive")]
        public async Task<ActionResult> DeleteActive(DeleteActiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Active? active = _context.Entry(user).Collection(u => u.ActiveGroups).Query().Include(x => x.Actives).SelectMany(x => x.Actives).FirstOrDefault(x => x.Id == request.Id);

                if (active is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                _context.Actives.Remove(active);

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
