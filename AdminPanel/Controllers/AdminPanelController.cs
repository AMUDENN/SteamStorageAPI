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

    public record TokenRequest(string Group, string Token);

    #endregion Records

    #region Methods

    public IActionResult CheckAdmin([FromQuery] TokenRequest request)
    {
        HttpContext.Response.Cookies.Append(ProgramConstants.JWT_COOKIES, request.Token);
        return RedirectToAction(nameof(AdminPanel));
    }

    public IActionResult AccessDenied()
    {
        return View();
    }

    public async Task<IActionResult> AdminPanel()
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);

        _apiClient.Token = token ?? string.Empty;
        Users.HasAccessToAdminPanelResponse? hasAccess = await _apiClient.GetAsync<Users.HasAccessToAdminPanelResponse>(
            ApiConstants.ApiMethods.GetHasAccessToAdminPanel);

        if (hasAccess is null || !hasAccess.HasAccess)
            return RedirectToAction(nameof(AccessDenied));

        Users.UserResponse? user = await _apiClient.GetAsync<Users.UserResponse>(
            ApiConstants.ApiMethods.GetCurrentUserInfo);

        Currencies.CurrenciesResponse? currencies = await _apiClient.GetAsync<Currencies.CurrenciesResponse>(
            ApiConstants.ApiMethods.GetCurrencies);
        
        Games.GamesResponse? games = await _apiClient.GetAsync<Games.GamesResponse>(
            ApiConstants.ApiMethods.GetGames);
        
        Roles.RolesResponse? roles = await _apiClient.GetAsync<Roles.RolesResponse>(
            ApiConstants.ApiMethods.GetRoles);

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
            Roles = roles?.Roles?.ToList() ?? []
        });
    }

    #endregion Methods
}
