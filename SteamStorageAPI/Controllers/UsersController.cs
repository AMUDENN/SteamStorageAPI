using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.UserService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class UsersController : ControllerBase
{
    #region Fields

    private readonly IUserService _userService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public UsersController(IUserService userService, IContextUserService contextUserService)
    {
        _userService = userService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

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
        CancellationToken cancellationToken = default) =>
        Ok(await _userService.GetUsersAsync(request, cancellationToken));

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
        Ok(new UsersCountResponse(await _userService.GetUsersCountAsync(cancellationToken)));

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
        CancellationToken cancellationToken = default) =>
        Ok(await _userService.GetUserInfoAsync(request, cancellationToken));

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
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _userService.GetCurrentUserInfoAsync(user, cancellationToken));
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
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(_userService.GetCurrentUserGoalSum(user));
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
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _userService.GetHasAccessToAdminPanelAsync(user, cancellationToken));
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
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _userService.PutGoalSumAsync(user, request, cancellationToken);
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
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _userService.DeleteUserAsync(user, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}