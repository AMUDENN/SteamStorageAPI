using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ArchiveGroupsController : ControllerBase
    {
        #region Enums
        public enum ArchiveGroupOrderName
        {
            Title, Count, BuySum, SoldSum, Change
        }
        #endregion Enums

        #region Fields
        private readonly ILogger<ArchiveGroupsController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ArchiveGroupOrderName, Func<ArchiveGroup, object>> _orderNames;
        #endregion Fields

        #region Constructor
        public ArchiveGroupsController(ILogger<ArchiveGroupsController> logger, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;


            _orderNames = new()
            {
                [ArchiveGroupOrderName.Title] = x => x.Title,
                [ArchiveGroupOrderName.Count] = x => _context.Entry(x).Collection(x => x.Archives).Query().Count(),
                [ArchiveGroupOrderName.BuySum] = x => _context.Entry(x).Collection(x => x.Archives).Query().Sum(x => x.Count * x.BuyPrice),
                [ArchiveGroupOrderName.SoldSum] = x => _context.Entry(x).Collection(x => x.Archives).Query().Sum(x => x.Count * x.SoldPrice),
                [ArchiveGroupOrderName.Change] = x =>
                {
                    decimal buySum = _context.Entry(x).Collection(x => x.Archives).Query().Sum(x => x.Count * x.BuyPrice);
                    decimal soldSum = _context.Entry(x).Collection(x => x.Archives).Query().Sum(x => x.Count * x.SoldPrice);

                    return buySum == 0 ? 0 : (soldSum - buySum) / buySum;
                }
            };
        }
        #endregion Constructor

        #region Records
        public record ArchiveGroupsResponse(int Id, string Title, string Description, string Colour);
        public record ArchiveGroupsCountResponse(int Count);
        public record GetArchiveGroupsRequest(ArchiveGroupOrderName? OrderName, bool? IsAscending);
        public record PostArchiveGroupRequest(string Title, string? Description, string? Colour);
        public record PutArchiveGroupRequest(int GroupId, string Title, string? Description, string? Colour);
        public record DeleteArchiveGroupRequest(int GroupId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetArchiveGroups")]
        public ActionResult<IEnumerable<ArchiveGroupsResponse>> GetArchiveGroups([FromQuery] GetArchiveGroupsRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");


                IEnumerable<ArchiveGroup> groups = _context.Entry(user).Collection(x => x.ArchiveGroups).Query();

                if (request.OrderName != null && request.IsAscending != null)
                    groups = (bool)request.IsAscending ? groups.OrderBy(_orderNames[(ArchiveGroupOrderName)request.OrderName])
                                                       : groups.OrderByDescending(_orderNames[(ArchiveGroupOrderName)request.OrderName]);

                return Ok(groups.Select(x =>
                            new ArchiveGroupsResponse(x.Id,
                                                        x.Title,
                                                        x.Description ?? string.Empty,
                                                        $"#{x.Colour ?? ProgramConstants.BaseArchiveGroupColour}")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetArchiveGroupsCount")]
        public ActionResult<ArchiveGroupsCountResponse> GetArchiveGroupsCount()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ArchiveGroupsCountResponse(_context.Entry(user).Collection(x => x.ArchiveGroups).Query().Count()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET

        #region POST
        [HttpPost(Name = "PostArchiveGroup")]
        public async Task<ActionResult> PostArchiveGroup(PostArchiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ArchiveGroups.Add(new ArchiveGroup()
                {
                    UserId = user.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Colour = request.Colour
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
        [HttpPut(Name = "PutArchiveGroup")]
        public async Task<ActionResult> PutArchiveGroup(PutArchiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = _context.Entry(user).Collection(u => u.ArchiveGroups).Query().FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                group.Title = request.Title;
                group.Description = request.Description;
                group.Colour = request.Colour;

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
        [HttpDelete(Name = "DeleteArchiveGroup")]
        public async Task<ActionResult> DeleteArchiveGroup(DeleteArchiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = _context.Entry(user).Collection(u => u.ArchiveGroups).Query().FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                _context.ArchiveGroups.Remove(group);

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
