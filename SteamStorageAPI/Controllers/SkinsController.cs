using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.SkinService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class SkinsController : ControllerBase
{
    #region Fields

    private readonly ISkinService _skinService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public SkinsController(ISkinService skinService, IContextUserService contextUserService)
    {
        _skinService = skinService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get information about a single skin
    /// </summary>
    /// <response code="200">Returns detailed information about the skin</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No skin with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSkinInfo")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinResponse>> GetSkinInfo(
        [FromQuery] GetSkinInfoRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _skinService.GetSkinInfoAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Get a simplified list of skins
    /// </summary>
    /// <response code="200">Returns a simplified list of skins</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetBaseSkins")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<BaseSkinsResponse>> GetBaseSkins(
        [FromQuery] GetBaseSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _skinService.GetBaseSkinsAsync(request, cancellationToken));
    }

    /// <summary>
    /// Get the list of skins
    /// </summary>
    /// <response code="200">Returns the list of skins</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSkins")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinsResponse>> GetSkins(
        [FromQuery] GetSkinsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _skinService.GetSkinsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Get the price dynamics of a skin
    /// </summary>
    /// <response code="200">Returns the price dynamics of the skin</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No skin with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSkinDynamics")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinDynamicStatsResponse>> GetSkinDynamics(
        [FromQuery] GetSkinDynamicsRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _skinService.GetSkinDynamicsAsync(user, request, cancellationToken));
    }

    /// <summary>
    /// Get the number of skin pages
    /// </summary>
    /// <response code="200">Returns the number of pages of the specified size</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSkinPagesCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SkinPagesCountResponse>> GetSkinPagesCount(
        [FromQuery] GetSkinPagesCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _skinService.GetSkinPagesCountAsync(user, request, cancellationToken));
    }


    /// <summary>
    /// Get the total number of skins in Steam
    /// </summary>
    /// <response code="200">Returns the number of skins in Steam</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSteamSkinsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SteamSkinsCountResponse>> GetSteamSkinsCount(
        [FromQuery] GetSteamSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _skinService.GetSteamSkinsCountAsync(request, cancellationToken));
    }

    /// <summary>
    /// Get the number of saved skins
    /// </summary>
    /// <response code="200">Returns the number of saved skins</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">The user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetSavedSkinsCount")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<SavedSkinsCountResponse>> GetSavedSkinsCount(
        [FromQuery] GetSavedSkinsCountRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _skinService.GetSavedSkinsCountAsync(user, request, cancellationToken));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Import a single skin from Steam
    /// </summary>
    /// <response code="201">The skin was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPost(Name = "PostSkin")]
    public async Task<ActionResult> PostSkin(
        PostSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        await _skinService.PostSkinAsync(request, cancellationToken);
        return Created();
    }

    /// <summary>
    /// Add a skin to marked items
    /// </summary>
    /// <response code="200">The skin was marked</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No skin with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPost(Name = "SetMarkedSkin")]
    public async Task<ActionResult> SetMarkedSkin(
        SetMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _skinService.SetMarkedSkinAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion POST

    #region DELETE

    /// <summary>
    /// Remove a skin from marked items
    /// </summary>
    /// <response code="200">The skin mark was removed</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No skin with the given Id exists in the marked skins table, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpDelete(Name = "DeleteMarkedSkin")]
    public async Task<ActionResult> DeleteMarkedSkin(
        DeleteMarkedSkinRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _skinService.DeleteMarkedSkinAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}