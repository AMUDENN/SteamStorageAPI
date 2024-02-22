using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UsersController : ControllerBase
    {
        #region Fields

        private readonly ILogger<UsersController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public UsersController(ILogger<UsersController> logger, IHttpClientFactory httpClientFactory,
            IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record UserResponse(
            int UserId,
            string SteamId,
            string? ImageUrl,
            string? ImageUrlMedium,
            string? ImageUrlFull,
            string? Nickname,
            int RoleId,
            int StartPageId,
            int CurrencyId,
            DateTime DateRegistration,
            decimal? GoalSum);

        public record GetUserRequest(
            int UserId);

        public record PutGoalSumRequest(
            decimal? GoalSum);

        public record PutStartPageRequest(
            int StartPageId);

        #endregion Records

        #region Methods

        private async Task<UserResponse?> GetUserResponse(User? user)
        {
            if (user is null)
                return null;

            HttpClient client = _httpClientFactory.CreateClient();
            SteamUserResult? steamUserResult =
                await client.GetFromJsonAsync<SteamUserResult>(SteamApi.GetUserInfoUrl(user.SteamId));

            if (steamUserResult is null)
                return null;

            SteamUser? steamUser = steamUserResult.response.players.FirstOrDefault();

            return new(user.Id,
                user.SteamId.ToString(),
                steamUser?.avatar, steamUser?.avatarmedium, steamUser?.avatarfull,
                steamUser?.personaname,
                user.RoleId,
                user.StartPageId,
                user.CurrencyId,
                user.DateRegistration,
                user.GoalSum);
        }

        #endregion Methods

        #region GET
        
        /// <summary>
        /// Получение списка пользователей
        /// </summary>
        /// <response code="200">Возвращает список пользователей</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        [HttpGet(Name = "GetUsers")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<UserResponse>> GetUsers()
        {
            try
            {
                return Ok(_context.Users.ToList().Select(async x => await GetUserResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        
        /// <summary>
        /// Получение информацию о пользователе
        /// </summary>
        /// <response code="200">Возвращает информацию о пользователе</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetUserInfo")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<UserResponse>> GetUserInfo([FromQuery] GetUserRequest request)
        {
            try
            {
                User? user = _context.Users.FirstOrDefault(x => x.Id == request.UserId);

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

        /// <summary>
        /// Получение информацию о текущем пользователе
        /// </summary>
        /// <response code="200">Возвращает информацию о текущем пользователе</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetCurrentUserInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<UserResponse>> GetCurrentUserInfo()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

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

        #region PUT

        /// <summary>
        /// Установка финансовой цели
        /// </summary>
        /// <response code="200">Финансовая цель успешно установлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpPut(Name = "PutGoalSum")]
        public async Task<ActionResult> PutGoalSum(PutGoalSumRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                user.GoalSum = request.GoalSum;

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

        /// <summary>
        /// Удаление текущего пользователя
        /// </summary>
        /// <response code="200">Пользователь успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpDelete(Name = "DeleteUser")]
        public async Task<ActionResult> DeleteUser()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.Users.Remove(user);

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
