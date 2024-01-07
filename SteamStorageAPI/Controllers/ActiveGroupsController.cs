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
    public class ActiveGroupsController : ControllerBase
    {
        #region Fields
        private readonly ILogger<ActiveGroupsController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public ActiveGroupsController(ILogger<ActiveGroupsController> logger, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record ActiveGroupsResponse(int Id, string Title, string Description, string Colour, decimal? GoalSum);
        public record ActiveGroupDynamicsResponse(int Id, DateTime DateUpdate, decimal Sum);
        public record GetActiveGroupDynamicRequest(int GroupID, int DaysDynamic = 30);
        public record PostActiveGroupRequest(string Title, string? Description, string? Colour, decimal? GoalSum);
        public record PutActiveGroupRequest(int GroupID, string Title, string? Description, string? Colour, decimal? GoalSum);
        public record DeleteActiveGroupRequest(int GroupID);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetActiveGroups")]
        public ActionResult<IEnumerable<ActiveGroupsResponse>> GetActiveGroups()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.Entry(user).Collection(u => u.ActiveGroups).Load();

                return Ok(user.ActiveGroups.Select(x =>
                        new ActiveGroupsResponse(x.Id,
                                                 x.Title,
                                                 x.Description ?? string.Empty,
                                                 $"#{x.Colour ?? ProgramConstants.BaseActiveGroupColour}",
                                                 x.GoalSum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetActiveGroupDynamics")]
        public ActionResult<IEnumerable<ActiveGroupDynamicsResponse>> GetActiveGroupDynamics([FromQuery] GetActiveGroupDynamicRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.Entry(user).Collection(u => u.ActiveGroups).Load();

                ActiveGroup? group = user.ActiveGroups.FirstOrDefault(x => x.Id == request.GroupID);

                if (group is null)
                    return NotFound("У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

                DateTime startDate = DateTime.Now.AddDays(-request.DaysDynamic);

                _context.Entry(group).Collection(s => s.ActiveGroupsDynamics).Load();

                return Ok(group.ActiveGroupsDynamics.Where(x => x.DateUpdate > startDate).Select(x =>
                        new ActiveGroupDynamicsResponse(x.Id,
                                                        x.DateUpdate,
                                                        x.Sum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET

        #region POST
        [HttpPost(Name = "PostActiveGroup")]
        public async Task<ActionResult> PostActiveGroups(PostActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ActiveGroups.Add(new ActiveGroup()
                {
                    UserId = user.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Colour = request.Colour,
                    GoalSum = request.GoalSum
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
        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroups(PutActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.Entry(user).Collection(u => u.ActiveGroups).Load();

                ActiveGroup? group = user.ActiveGroups.FirstOrDefault(x => x.Id == request.GroupID);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                group.Title = request.Title;
                group.Description = request.Description;
                group.Colour = request.Colour;
                group.GoalSum = request.GoalSum;

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
        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(DeleteActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.Entry(user).Collection(u => u.ActiveGroups).Load();

                ActiveGroup? group = user.ActiveGroups.FirstOrDefault(x => x.Id == request.GroupID);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                _context.ActiveGroups.Remove(group);

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
