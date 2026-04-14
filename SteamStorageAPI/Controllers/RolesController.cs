using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.RoleService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class RolesController : ControllerBase
{
    #region Fields

    private readonly IRoleService _roleService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public RolesController(IRoleService roleService, IContextUserService contextUserService)
    {
        _roleService = roleService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение списка ролей
    /// </summary>
    /// <response code="200">Возвращает список ролей</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpGet(Name = "GetRoles")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<RolesResponse>> GetRoles(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _roleService.GetRolesAsync(cancellationToken));
    }

    #endregion GET

    #region PUT

    /// <summary>
    /// Установка роли пользователю
    /// </summary>
    /// <response code="200">Роль успешно установлена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Роли с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPut(Name = "SetRole")]
    public async Task<ActionResult> SetRole(
        SetRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _roleService.SetRoleAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT
}