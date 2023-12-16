using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities;
using System.Text.RegularExpressions;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class GroupsController : ControllerBase
    {
        #region Fields
        private readonly ILogger<GroupsController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public GroupsController(ILogger<GroupsController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record ArchiveGroupsResponse(int Id, string Title, string Description, string Colour);
        public record ActiveGroupsResponse(int Id, string Title, string Description, string Colour, decimal? GoalSum);
        public record ActiveGroupDynamicsResponse(int Id, DateTime DateUpdate, decimal Sum);
        public record GetActiveGroupDynamicRequest(int GroupID, int DaysDynamic = 30);
        public record PostArchiveGroupRequest(string Title, string? Description, string? Colour);
        public record PostActiveGroupRequest(string Title, string? Description, string? Colour, decimal? GoalSum);
        public record PutArchiveGroupRequest(int GroupId, string Title, string? Description, string? Colour);
        public record PutActiveGroupRequest(int GroupId, string Title, string? Description, string? Colour, decimal? GoalSum);
        public record DeleteArchiveGroupRequest(int GroupId);
        public record DeleteActiveGroupRequest(int GroupId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetArchiveGroups")]
        public ActionResult<IEnumerable<ArchiveGroupsResponse>> GetArchiveGroups()
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(_context.ArchiveGroups.ToList().Where(x => x.UserId == userId).Select(x =>
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

        [HttpGet(Name = "GetActiveGroups")]
        public ActionResult<IEnumerable<ActiveGroupsResponse>> GetActiveGroups()
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(_context.ActiveGroups.ToList().Where(x => x.UserId == userId).Select(x =>
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
        public ActionResult<IEnumerable<ActiveGroupDynamicsResponse>> GetActiveGroupDynamics([FromQuery]GetActiveGroupDynamicRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.ActiveGroups.Where(x => x.UserId == userId).FirstOrDefault();

                if (group is null)
                    return NotFound("У вас нет доступа к информации о группе с таким Id");

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
        [HttpPost(Name = "PostArchiveGroup")]
        public async Task<ActionResult> PostArchiveGroups(PostArchiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ArchiveGroups.Add(new ArchiveGroup()
                {
                    UserId = (int)userId,
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

        [HttpPost(Name = "PostActiveGroup")]
        public async Task<ActionResult> PostActiveGroups(PostActiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ActiveGroups.Add(new ActiveGroup()
                {
                    UserId = (int)userId,
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
        [HttpPut(Name = "PutArchiveGroup")]
        public async Task<ActionResult> PutArchiveGroups(PutArchiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = _context.ArchiveGroups.Where(x => x.UserId == userId && x.Id == request.GroupId).FirstOrDefault();

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы");

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

        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroups(PutActiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.ActiveGroups.Where(x => x.UserId == userId && x.Id == request.GroupId).FirstOrDefault();

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы");

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
        [HttpDelete(Name = "DeleteArchiveGroup")]
        public async Task<ActionResult> DeleteArchiveGroup(DeleteArchiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = _context.ArchiveGroups.Where(x => x.UserId == userId && x.Id == request.GroupId).FirstOrDefault();

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы");

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

        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(DeleteActiveGroupRequest request)
        {
            try
            {
                int? userId = UserContext.GetUserId(HttpContext);

                if (userId is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.ActiveGroups.Where(x => x.UserId == userId && x.Id == request.GroupId).FirstOrDefault();

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы");

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
