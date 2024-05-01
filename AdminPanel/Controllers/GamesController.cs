using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities;

namespace AdminPanel.Controllers;

public class GamesController : Controller
{
    #region Fields

    private readonly ApiClient _apiClient;

    #endregion Fields

    #region Construtore

    public GamesController(
        ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    #endregion Constructor

    #region Records

    public record AddGameRequest(
        [FromForm(Name = "steamGameId")] int SteamGameId,
        [FromForm(Name = "iconUrlHash")] string IconUrlHash);

    public record PutGameRequest(
        [FromForm(Name = "gameId")] int GameId,
        [FromForm(Name = "title")] string Title,
        [FromForm(Name = "iconUrlHash")] string IconUrlHash);

    public record DeleteGameRequest(
        [FromForm(Name = "gameId")] int GameId);

    #endregion Records

    #region Methods

    [HttpPost]
    public async Task<IActionResult> AddGame(
        AddGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.PostAsync(
            ApiConstants.ApiMethods.PostGame,
            new Games.PostGameRequest(request.SteamGameId, request.IconUrlHash),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutGame(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.PutAsync(
            ApiConstants.ApiMethods.PutGameInfo,
            new Games.PutGameRequest(request.GameId, request.IconUrlHash, request.Title),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGame(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.DeleteAsync(
            ApiConstants.ApiMethods.DeleteGame,
            new Games.DeleteGameRequest(request.GameId),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    #endregion Methods
}
