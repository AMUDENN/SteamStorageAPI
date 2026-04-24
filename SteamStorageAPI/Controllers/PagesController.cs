using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.PageService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class PagesController : ControllerBase
{
    #region Fields

    private readonly IPageService _pageService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public PagesController(IPageService pageService, IContextUserService contextUserService)
    {
        _pageService = pageService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get the list of pages
    /// </summary>
    /// <response code="200">Returns the list of pages</response>
    /// <response code="499">The operation was cancelled</response>
    [HttpGet(Name = "GetPages")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<PagesResponse>> GetPages(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _pageService.GetPagesAsync(cancellationToken));
    }

    /// <summary>
    /// Get the current start page of the user
    /// </summary>
    /// <response code="200">Returns the current start page of the user</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No page with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentStartPage")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<PageResponse>> GetCurrentStartPage(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _pageService.GetCurrentStartPageAsync(user, cancellationToken));
    }

    #endregion GET

    #region PUT

    /// <summary>
    /// Set the start page
    /// </summary>
    /// <response code="200">The start page was successfully set</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No page with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "SetStartPage")]
    public async Task<ActionResult> SetStartPage(
        SetPageRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _pageService.SetStartPageAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT
}