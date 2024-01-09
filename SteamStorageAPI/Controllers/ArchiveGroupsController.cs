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
        #region Fields
        private readonly ILogger<ArchiveGroupsController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public ArchiveGroupsController(ILogger<ArchiveGroupsController> logger, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record ArchiveGroupsResponse(int Id, string Title, string Description, string Colour);
        public record PostArchiveGroupRequest(string Title, string? Description, string? Colour);
        public record PutArchiveGroupRequest(int GroupId, string Title, string? Description, string? Colour);
        public record DeleteArchiveGroupRequest(int GroupId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetArchiveGroups")]
        public ActionResult<IEnumerable<ArchiveGroupsResponse>> GetArchiveGroups()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(_context.Entry(user)
                                  .Collection(u => u.ArchiveGroups)
                                  .Query()
                                  .Select(x =>
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
        #endregion GET

        #region POST
        [HttpPost(Name = "PostArchiveGroup")]
        public async Task<ActionResult> PostArchiveGroups(PostArchiveGroupRequest request)
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
        public async Task<ActionResult> PutArchiveGroups(PutArchiveGroupRequest request)
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
