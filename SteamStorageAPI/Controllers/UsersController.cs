using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Users;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class UsersController : ControllerBase
    {
        #region Fields

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public UsersController(
            IHttpClientFactory httpClientFactory,
            IUserService userService,
            SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record UserResponse(
            int UserId,
            string SteamId,
            string ProfileUrl,
            string? ImageUrl,
            string? ImageUrlMedium,
            string? ImageUrlFull,
            string? Nickname,
            string Role,
            int StartPageId,
            int CurrencyId,
            DateTime DateRegistration,
            decimal? GoalSum);
        
        public record UsersResponse(
            int Count,
            IEnumerable<UserResponse?> Users);

        [Validator<GetUserRequestValidator>]
        public record GetUserRequest(
            int UserId);

        [Validator<PutGoalSumRequestValidator>]
        public record PutGoalSumRequest(
            decimal? GoalSum);

        #endregion Records

        #region Methods

        private async Task<UserResponse?> GetUserResponseAsync(
            User? user,
            CancellationToken cancellationToken = default)
        {
            if (user is null)
                return null;

            if (user.Username is null || user.IconUrl is null || user.IconUrlMedium is null ||
                user.IconUrlFull is null ||
                (user.DateUpdate.HasValue && user.DateUpdate.Value < DateTime.Now.AddDays(-1)))
            {
                HttpClient client = _httpClientFactory.CreateClient();
                SteamUserResult? steamUserResult =
                    await client.GetFromJsonAsync<SteamUserResult>(SteamApi.GetUserInfoUrl(user.SteamId),
                        cancellationToken);

                if (steamUserResult is null)
                    return null;

                SteamUser? steamUser = steamUserResult.response.players.FirstOrDefault();

                user.Username = steamUser?.personaname;
                user.IconUrl = steamUser?.avatar.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlMedium = steamUser?.avatarmedium.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlFull = steamUser?.avatarfull.Replace("https://avatars.steamstatic.com/", string.Empty);

                user.DateUpdate = DateTime.Now;
            }

            await _context.SaveChangesAsync(cancellationToken);

            Role? role = await _context.Roles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.RoleId, cancellationToken);
            
            return new(user.Id,
                user.SteamId.ToString(),
                SteamApi.GetUserUrl(user.SteamId),
                user.IconUrl is null ? null : SteamApi.GetUserIconUrl(user.IconUrl),
                user.IconUrlMedium is null ? null : SteamApi.GetUserIconUrl(user.IconUrlMedium),
                user.IconUrlFull is null ? null : SteamApi.GetUserIconUrl(user.IconUrlFull),
                user.Username?.Trim([' ']),
                role?.Title ?? "Роль не найдена",
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
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetUsers")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<UsersResponse>> GetUsers(
            CancellationToken cancellationToken = default)
        {
            List<User> users = await _context.Users.AsNoTracking().ToListAsync(cancellationToken);

            return Ok(new UsersResponse(users.Count, users.Select(x => GetUserResponseAsync(x, cancellationToken).Result)));
        }

        /// <summary>
        /// Получение информацию о пользователе
        /// </summary>
        /// <response code="200">Возвращает информацию о пользователе</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetUserInfo")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<UserResponse>> GetUserInfo(
            [FromQuery] GetUserRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(await GetUserResponseAsync(user, cancellationToken));
        }

        /// <summary>
        /// Получение информацию о текущем пользователе
        /// </summary>
        /// <response code="200">Возвращает информацию о текущем пользователе</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetCurrentUserInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<UserResponse>> GetCurrentUserInfo(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(await GetUserResponseAsync(user, cancellationToken));
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
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutGoalSum")]
        public async Task<ActionResult> PutGoalSum(
            PutGoalSumRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            user.GoalSum = request.GoalSum;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
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
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteUser")]
        public async Task<ActionResult> DeleteUser(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            _context.Users.Remove(user);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
