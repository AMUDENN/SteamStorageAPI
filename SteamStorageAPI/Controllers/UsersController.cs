using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UsersController : ControllerBase
    {
        #region Fields
        private readonly ILogger<UsersController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public UsersController(ILogger<UsersController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record UserResponse(int UserId, long SteamId, int RoleId, int CurrencyId, DateTime DateRegistration);
        public record GetUserRequest(int UserId);
        #endregion Records

        #region Methods
        private User? FindUser(int? Id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == Id);
        }

        private UserResponse? GetUserResponse(User? user)
        {
            if (user is null)
                return null;

            return new UserResponse(user.Id,
                                    user.SteamId,
                                    user.RoleId,
                                    user.CurrencyId,
                                    user.DateRegistration);
        }
        #endregion Methods

        #region GET
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpGet(Name = "GetUsers")]
        public ActionResult<IEnumerable<UserResponse>> GetUsers()
        {
            try
            {
                return Ok(_context.Users.ToList().Select(x => GetUserResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetUserInfo")]
        public ActionResult<UserResponse> GetUserInfo([FromQuery] GetUserRequest request)
        {
            try
            {
                User? user = FindUser(request.UserId);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(GetUserResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetCurrentUserInfo")]
        public ActionResult<UserResponse> GetCurrentUserInfo()
        {
            try
            {
                User? user = FindUser(UserContext.GetUserId(HttpContext));

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(GetUserResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET
    }
}
