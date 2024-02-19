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
    public class ArchivesController : ControllerBase
    {
        #region Enums

        public enum ArchiveOrderName
        {
            Title,
            Count,
            BuyPrice,
            SoldPrice,
            SoldSum,
            Change
        }

        #endregion Enums

        #region Fields

        private readonly ILogger<ArchivesController> _logger;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ArchiveOrderName, Func<Archive, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public ArchivesController(ILogger<ArchivesController> logger, ISkinService skinService,
            IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [ArchiveOrderName.Title] = x => x.Skin.Title,
                [ArchiveOrderName.Count] = x => x.Count,
                [ArchiveOrderName.BuyPrice] = x => x.BuyPrice,
                [ArchiveOrderName.SoldPrice] = x => x.SoldPrice,
                [ArchiveOrderName.SoldSum] = x => x.Count * x.SoldPrice,
                [ArchiveOrderName.Change] = x => x.BuyPrice == 0 ? 0 : (x.SoldPrice - x.BuyPrice) / x.BuyPrice
            };
        }

        #endregion Constructor

        #region Records

        public record ArchiveResponse(
            int Id,
            BaseSkinResponse Skin,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            decimal SoldSum,
            double Change);

        public record ArchivesResponse(
            int ArchivesCount,
            int PagesCount,
            IEnumerable<ArchiveResponse> Skins);

        public record ArchivesPagesCountResponse(
            int Count);

        public record ArchivesCountResponse(
            int Count);

        public record GetArchivesRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            ArchiveOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        public record GetArchivesPagesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            int PageSize);

        public record GetArchivesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        public record PostArchiveRequest(
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate,
            DateTime SoldDate);

        public record PutArchiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate,
            DateTime SoldDate);

        public record DeleteArchiveRequest(
            int Id);

        #endregion Records

        #region Methods

        private ArchiveResponse GetArchiveResponse(Archive archive)
        {
            return new(archive.Id,
                _skinService.GetBaseSkinResponse(archive.Skin),
                archive.Count,
                archive.BuyPrice,
                archive.SoldPrice,
                archive.SoldPrice * archive.Count,
                (double)(archive.BuyPrice == 0 ? 0 : (archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice));
        }

        #endregion Methods

        #region GET

        [HttpGet(Name = "GetArchives")]
        public ActionResult<ArchivesResponse> GetArchives([FromQuery] GetArchivesRequest request)
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

                IEnumerable<Archive> archives = _context.Entry(user)
                    .Collection(x => x.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Archives)
                    .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                && (request.GroupId == null || x.GroupId == request.GroupId));

                if (request is { OrderName: not null, IsAscending: not null })
                    archives = (bool)request.IsAscending
                        ? archives.OrderBy(_orderNames[(ArchiveOrderName)request.OrderName])
                        : archives.OrderByDescending(_orderNames[(ArchiveOrderName)request.OrderName]);

                archives = archives.Skip((request.PageNumber - 1) * request.PageSize)
                    .Take(request.PageSize);

                int archivesCount = _context
                    .Entry(user)
                    .Collection(x => x.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Archives).Count(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                                            && (string.IsNullOrEmpty(request.Filter) ||
                                                                x.Skin.Title.Contains(request.Filter!))
                                                            && (request.GroupId == null ||
                                                                x.GroupId == request.GroupId));

                int pagesCount = (int)Math.Ceiling((double)archivesCount / request.PageSize);

                return Ok(new ArchivesResponse(archivesCount, pagesCount, archives.Select(GetArchiveResponse)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetArchivesPagesCount")]
        public ActionResult<ArchivesPagesCountResponse> GetArchivesPagesCount(
            [FromQuery] GetArchivesPagesCountRequest request)
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

                int count = _context
                    .Entry(user)
                    .Collection(x => x.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Archives).Count(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                                            && (string.IsNullOrEmpty(request.Filter) ||
                                                                x.Skin.Title.Contains(request.Filter!))
                                                            && (request.GroupId == null ||
                                                                x.GroupId == request.GroupId));

                int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

                return Ok(new ArchivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetArchivesCount")]
        public ActionResult<ArchivesCountResponse> GetArchivesCount([FromQuery] GetArchivesCountRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ArchivesCountResponse(_context
                    .Entry(user)
                    .Collection(x => x.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .ThenInclude(x => x.Skin)
                    .SelectMany(x => x.Archives)
                    .Count(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                && (request.GroupId == null || x.GroupId == request.GroupId))));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region POST

        [HttpPost(Name = "PostArchive")]
        public async Task<ActionResult> PostArchive(PostArchiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Entry(user).Collection(x => x.ArchiveGroups).Query().Any(x => x.Id == request.GroupId))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                if (!_context.Skins.Any(x => x.Id == request.SkinId))
                    return NotFound("Скина с таким Id не существует");

                _context.Archives.Add(new()
                {
                    GroupId = request.GroupId,
                    Count = request.Count,
                    BuyPrice = request.BuyPrice,
                    SoldPrice = request.SoldPrice,
                    SkinId = request.SkinId,
                    Description = request.Description,
                    BuyDate = request.BuyDate,
                    SoldDate = request.SoldDate
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

        [HttpPut(Name = "PutArchive")]
        public async Task<ActionResult> PutArchive(PutArchiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Archive? archive = _context.Entry(user)
                    .Collection(u => u.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .SelectMany(x => x.Archives)
                    .FirstOrDefault(x => x.Id == request.Id);

                if (archive is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                if (!_context.Entry(user).Collection(x => x.ActiveGroups).Query().Any(x => x.Id == request.GroupId))
                    return NotFound("У вас нет доступа к этой группе или группы с таким Id не существует");

                if (!_context.Skins.Any(x => x.Id == request.SkinId))
                    return NotFound("Скина с таким Id не существует");

                archive.GroupId = request.GroupId;
                archive.Count = request.Count;
                archive.BuyPrice = request.BuyPrice;
                archive.SoldPrice = request.SoldPrice;
                archive.SkinId = request.SkinId;
                archive.Description = request.Description;
                archive.BuyDate = request.BuyDate;
                archive.SoldDate = request.SoldDate;

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

        [HttpDelete(Name = "DeleteArchive")]
        public async Task<ActionResult> DeleteArchive(DeleteArchiveRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                Archive? archive = _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                    .Include(x => x.Archives).SelectMany(x => x.Archives).FirstOrDefault(x => x.Id == request.Id);

                if (archive is null)
                    return NotFound("У вас нет доступа к изменению этого актива или актива с таким Id не существует");

                _context.Archives.Remove(archive);

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
