using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;

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
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutGame(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGame(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }
    
    #endregion Methods
}
