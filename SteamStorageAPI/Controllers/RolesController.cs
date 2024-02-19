using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [Route("api/[controller]/[action]")]
    public class RolesController : ControllerBase
    {
        #region Fields

        private readonly ILogger<RolesController> _logger;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public RolesController(ILogger<RolesController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record RoleResponse(
            int Id,
            string Title);

        public record SetRoleRequest(
            int UserId,
            int RoleId);

        #endregion Records

        #region GET

        [HttpGet(Name = "GetRoles")]
        public ActionResult<IEnumerable<RoleResponse>> GetRoles()
        {
            try
            {
                return Ok(_context.Roles.ToList().Select(x =>
                    new RoleResponse(x.Id, x.Title)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET

        #region PUT

        [HttpPut(Name = "SetRole")]
        public async Task<ActionResult> SetRole(SetRoleRequest request)
        {
            try
            {
                User? user = _context.Users.FirstOrDefault(x => x.Id == request.UserId);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Roles.Any(x => x.Id == request.RoleId))
                    return NotFound("Роли с таким Id не существует");

                user.RoleId = request.RoleId;

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
    }
}
