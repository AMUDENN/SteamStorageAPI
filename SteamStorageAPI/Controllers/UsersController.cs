using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.Infrastructure.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

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

    #region Methods

    private static UserResponse GetUserResponse(User user) =>
        new(user.Id,
            user.SteamId.ToString(),
            SteamApi.GetUserUrl(user.SteamId),
            user.IconUrl is null ? null : SteamApi.GetUserIconUrl(user.IconUrl),
            user.IconUrlMedium is null ? null : SteamApi.GetUserIconUrl(user.IconUrlMedium),
            user.IconUrlFull is null ? null : SteamApi.GetUserIconUrl(user.IconUrlFull),
            user.Username?.Trim([' ']),
            user.RoleId,
            user.Role.Title,
            user.StartPageId,
            user.StartPage.Title,
            user.CurrencyId,
            user.DateRegistration,
            user.GoalSum);

    private async Task<UserResponse> GetCurrentUserResponseAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (user.Username is null
            || user.IconUrl is null
            || user.IconUrlMedium is null
            || user.IconUrlFull is null
            || user.DateUpdate.HasValue && user.DateUpdate.Value < DateTime.Now.AddDays(-1))
        {
            HttpClient client = _httpClientFactory.CreateClient();
            SteamUserResult? steamUserResult =
                await client.GetFromJsonAsync<SteamUserResult>(
                    SteamApi.GetUserInfoUrl(user.SteamId), cancellationToken);

            if (steamUserResult is not null)
            {
                SteamUser? steamUser = steamUserResult.response.players.FirstOrDefault();
                user.Username = steamUser?.personaname;
                user.IconUrl = steamUser?.avatar.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlMedium = steamUser?.avatarmedium.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlFull = steamUser?.avatarfull.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.DateUpdate = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return GetUserResponse(user);
    }

    #endregion Methods

    #region GET

    /// <summary>
    /// Получение списка пользователей
    /// </summary>
    /// <response code="200">Возвращает список пользователей</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUsers")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UsersResponse>> GetUsers(
        [FromQuery] GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = _context.Users.AsNoTracking();
        int usersCount = await users.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)usersCount / request.PageSize);

        users = users
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(x => x.Role)
            .Include(x => x.StartPage);

        return Ok(new UsersResponse(usersCount, pagesCount == 0 ? 1 : pagesCount,
            users.Select(x => GetUserResponse(x))));
    }

    /// <summary>
    /// Получение количества пользователей
    /// </summary>
    /// <response code="200">Возвращает количество пользователей</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUsersCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UsersCountResponse>> GetUsersCount(
        CancellationToken cancellationToken = default) =>
        Ok(new UsersCountResponse(await _context.Users.CountAsync(cancellationToken)));

    /// <summary>
    /// Получение информации о пользователе
    /// </summary>
    /// <response code="200">Возвращает информацию о пользователе</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetUserInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UserResponse>> GetUserInfo(
        [FromQuery] GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _context.Users.AsNoTracking()
                        .Include(x => x.Role)
                        .Include(x => x.StartPage)
                        .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await GetCurrentUserResponseAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    /// <response code="200">Возвращает информацию о текущем пользователе</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentUserInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<UserResponse>> GetCurrentUserInfo(
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _context.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);
        await _context.Entry(user).Reference(x => x.StartPage).LoadAsync(cancellationToken);

        return Ok(await GetCurrentUserResponseAsync(user, cancellationToken));
    }

    /// <summary>
    /// Получение финансовой цели текущего пользователя
    /// </summary>
    /// <response code="200">Возвращает финансовую цель текущего пользователя</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentUserGoalSum")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<GoalSumResponse>> GetCurrentUserGoalSum(
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(new GoalSumResponse(user.GoalSum));
    }

    /// <summary>
    /// Получение информации о доступе к админ панели
    /// </summary>
    /// <response code="200">Возвращает информацию о доступе к админ панели</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetHasAccessToAdminPanel")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<HasAccessToAdminPanelResponse>> GetHasAccessToAdminPanel(
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _context.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);

        return Ok(new HasAccessToAdminPanelResponse(user.Role.Title == nameof(Role.Roles.Admin)));
    }

    #endregion GET

    #region PUT

    /// <summary>
    /// Установка финансовой цели
    /// </summary>
    /// <response code="200">Финансовая цель успешно установлена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "PutGoalSum")]
    public async Task<ActionResult> PutGoalSum(
        PutGoalSumRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
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
    /// <response code="200">Пользователь успешно удалён</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpDelete(Name = "DeleteUser")]
    public async Task<ActionResult> DeleteUser(CancellationToken cancellationToken = default)
    {
        User user = await _userService.GetCurrentUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok();
    }

    #endregion DELETE
}