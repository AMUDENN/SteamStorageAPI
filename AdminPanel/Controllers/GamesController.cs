using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK.ApiClient;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities.ApiControllers;

namespace AdminPanel.Controllers;

public class GamesController : Controller
{
    #region Fields

    private readonly IApiClient _apiClient;
    private readonly ICookiesUserService _cookieUserService;

    #endregion Fields

    #region Construtore

    public GamesController(
        IApiClient apiClient,
        ICookiesUserService cookieUserService)
    {
        _apiClient = apiClient;
        _cookieUserService = cookieUserService;
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
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        await _apiClient.PostAsync(
            ApiConstants.ApiMethods.PostGame,
            new Games.PostGameRequest(request.SteamGameId, request.IconUrlHash),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "games"
        });
    }

    [HttpPost]
    public async Task<IActionResult> PutGame(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        await _apiClient.PutAsync(
            ApiConstants.ApiMethods.PutGameInfo,
            new Games.PutGameRequest(request.GameId, request.IconUrlHash, request.Title),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "games"
        });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGame(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        await _apiClient.DeleteAsync(
            ApiConstants.ApiMethods.DeleteGame,
            new Games.DeleteGameRequest(request.GameId),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "games"
        });
    }

    #endregion Methods
}