using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.GameService;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class GamesController : ControllerBase
{
    #region Fields

    private readonly IGameService _gameService;

    #endregion Fields

    #region Constructor

    public GamesController(IGameService gameService)
    {
        _gameService = gameService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get the list of games
    /// </summary>
    /// <response code="200">Returns the list of games</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetGames")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<GamesResponse>> GetGames(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _gameService.GetGamesAsync(cancellationToken));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add a new game
    /// </summary>
    /// <response code="200">The game was successfully added</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPost(Name = "PostGame")]
    public async Task<ActionResult> PostGame(
        PostGameRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gameService.PostGameAsync(request, cancellationToken);
        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Update a game
    /// </summary>
    /// <response code="200">The game was successfully updated</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPut(Name = "PutGameInfo")]
    public async Task<ActionResult> PutGameInfo(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gameService.PutGameAsync(request, cancellationToken);
        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete a game
    /// </summary>
    /// <response code="200">The game was successfully deleted</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No game with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpDelete(Name = "DeleteGame")]
    public async Task<ActionResult> DeleteGame(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        await _gameService.DeleteGameAsync(request, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}