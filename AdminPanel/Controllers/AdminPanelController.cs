using AdminPanel.Models;
using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities;

namespace AdminPanel.Controllers;

public class AdminPanelController : Controller
{
    #region Fields

    private readonly ApiClient _apiClient;

    #endregion Fields

    #region Construtore

    public AdminPanelController(
        ApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    #endregion Constructor

    #region Records

    public record TokenRequest(
        string Group,
        string Token);

    public record AdminPanelRequest(
        int? UsersPageNumber);

    public record AddCurrencyRequest(
        [FromForm(Name = "steamCurrencyId")] int SteamCurrencyId,
        [FromForm(Name = "mark")] string Mark,
        [FromForm(Name = "title")] string Title,
        [FromForm(Name = "cultureInfo")] string CultureInfo);

    public record PutCurrencyRequest(
        [FromForm(Name = "currencyId")] int CurrencyId,
        [FromForm(Name = "mark")] string Mark,
        [FromForm(Name = "title")] string Title,
        [FromForm(Name = "cultureInfo")] string CultureInfo);

    public record DeleteCurrencyRequest(
        [FromForm(Name = "currencyId")] int CurrencyId);

    public record AddGameRequest(
        [FromForm(Name = "steamGameId")] int SteamGameId,
        [FromForm(Name = "iconUrlHash")] string IconUrlHash);

    public record PutGameRequest(
        [FromForm(Name = "gameId")] int GameId,
        [FromForm(Name = "title")] string Title,
        [FromForm(Name = "iconUrlHash")] string IconUrlHash);

    public record DeleteGameRequest(
        [FromForm(Name = "gameId")] int GameId);

    public record SetRoleRequest(
        [FromForm(Name = "userId")] int UserId,
        [FromForm(Name = "roleId")] int RoleId);

    #endregion Records

    #region Methods

    public IActionResult CheckAdmin(
        [FromQuery] TokenRequest request)
    {
        HttpContext.Response.Cookies.Append(ProgramConstants.JWT_COOKIES, request.Token);
        return RedirectToAction(nameof(AdminPanel));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public async Task<IActionResult> AdminPanel(
        [FromQuery] AdminPanelRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);

        _apiClient.Token = token ?? string.Empty;
        Users.HasAccessToAdminPanelResponse? hasAccess = await _apiClient.GetAsync<Users.HasAccessToAdminPanelResponse>(
            ApiConstants.ApiMethods.GetHasAccessToAdminPanel,
            cancellationToken);

        if (hasAccess is null || !hasAccess.HasAccess)
            return RedirectToAction(nameof(AccessDenied));

        Users.UserResponse? user = await _apiClient.GetAsync<Users.UserResponse>(
            ApiConstants.ApiMethods.GetCurrentUserInfo,
            cancellationToken);

        Currencies.CurrenciesResponse? currencies = await _apiClient.GetAsync<Currencies.CurrenciesResponse>(
            ApiConstants.ApiMethods.GetCurrencies,
            cancellationToken);

        Games.GamesResponse? games = await _apiClient.GetAsync<Games.GamesResponse>(
            ApiConstants.ApiMethods.GetGames,
            cancellationToken);

        Roles.RolesResponse? roles = await _apiClient.GetAsync<Roles.RolesResponse>(
            ApiConstants.ApiMethods.GetRoles,
            cancellationToken);

        Users.UsersResponse? users = await _apiClient.GetAsync<Users.UsersResponse, Users.GetUsersRequest>(
            ApiConstants.ApiMethods.GetUsers,
            new (request.UsersPageNumber ?? 1, 10),
            cancellationToken);

        if (user is null)
            return View();

        return View(new AdminPanelViewModel
        {
            ProfileImageUrl = user.ImageUrlFull ?? string.Empty,
            Nickname = user.Nickname ?? string.Empty,
            SteamId = user.SteamId,
            Role = user.Role,
            Currencies = currencies?.Currencies?.ToList() ?? [],
            Games = games?.Games?.ToList() ?? [],
            Roles = roles?.Roles?.ToList() ?? [],
            UsersPageNumber = request.UsersPageNumber ?? 1,
            Users = users?.Users?.ToList() ?? []
        });
    }

    [HttpPost]
    public async Task<IActionResult> AddCurrency(
        AddCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutCurrency(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCurrency(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> AddGame(
        AddGameRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutGame(
        PutGameRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteGame(
        DeleteGameRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(
        SetRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        return RedirectToAction(nameof(AdminPanel));
    }

    #endregion Methods
}
