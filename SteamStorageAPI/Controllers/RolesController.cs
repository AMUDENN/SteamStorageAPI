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
    /// Get the list of roles
    /// </summary>
    /// <response code="200">Returns the list of roles</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
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
    /// Assign a role to a user
    /// </summary>
    /// <response code="200">The role was successfully assigned</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No role with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPut(Name = "SetRole")]
    public async Task<ActionResult> SetRole(
        SetRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _roleService.SetRoleAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT
}