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

        return View();
    }

    #endregion Methods
}
