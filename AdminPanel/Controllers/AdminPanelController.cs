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
        [FromForm(Name = "usersPageNumber")] int? UsersPageNumber);

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
        AdminPanelRequest request,
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
            new(request.UsersPageNumber ?? 1, 7),
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
            UsersPagesCount = users?.PagesCount ?? 1,
            Users = users?.Users?.ToList() ?? []
        });
    }

    #endregion Methods
}
