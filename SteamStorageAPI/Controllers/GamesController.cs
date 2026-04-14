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
    /// Получение списка игр
    /// </summary>
    /// <response code="200">Возвращает список игр</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
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
    /// Добавление новой игры
    /// </summary>
    /// <response code="200">Игра успешно добавлена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
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
    /// Изменение игры
    /// </summary>
    /// <response code="200">Игра успешно изменена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Игры с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
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
    /// Удаление игры
    /// </summary>
    /// <response code="200">Игра успешно удалена</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Игры с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
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