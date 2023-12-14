using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities;
using static SteamStorageAPI.Controllers.CurrenciesController;
using static SteamStorageAPI.Controllers.RolesController;
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
        public record UserRequest(int UserId);
        public record UserResponse(int UserId, long SteamId, RoleResponse Role, CurrencyResponse Currency, DateTime DateRegistration);
        #endregion Records

        #region Methods
        private User? FindUser(int? Id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == Id);
        }

        private async Task<UserResponse?> GetUserResponse(User? user)
        {
            if (user is null)
                return null;

            await _context.Roles.LoadAsync();
            await _context.Currencies.LoadAsync();

            return new UserResponse(user.Id,
                                    user.SteamId,
                                    new RoleResponse(user.Role.Id, user.Role.Title),
                                    new CurrencyResponse(user.Currency.Id, user.Currency.SteamCurrencyId, user.Currency.Title, user.Currency.Mark),
                                    user.DateRegistration);
        }
        #endregion Methods

        #region GET
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpGet(Name = "GetUsers")]
        public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
        {
            try
            {
                await _context.Roles.LoadAsync();
                await _context.Currencies.LoadAsync();

                return Ok(_context.Users.ToList().Select(async x => await GetUserResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetUserInfo")]
        public async Task<ActionResult<UserResponse>> GetUserInfo([FromQuery]UserRequest request)
        {
            try
            {
                User? user = FindUser(request.UserId);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(await GetUserResponse(user));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetCurrentUserInfo")]
        public async Task<ActionResult<UserResponse>> GetCurrentUserInfo()
        {
            try
            {
                User? user = FindUser(UserContext.GetUserId(HttpContext));

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(await GetUserResponse(user));
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
