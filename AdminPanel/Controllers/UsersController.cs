using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK.ApiClient;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities.ApiControllers;

namespace AdminPanel.Controllers;

public class UsersController : Controller
{
    #region Fields

    private readonly IApiClient _apiClient;
    private readonly ICookiesUserService _cookieUserService;

    #endregion Fields

    #region Construtore

    public UsersController(
        IApiClient apiClient,
        ICookiesUserService cookieUserService)
    {
        _apiClient = apiClient;
        _cookieUserService = cookieUserService;
    }

    #endregion Constructor

    #region Records

    public record SetRoleRequest(
        [FromForm(Name = "userId")] int UserId,
        [FromForm(Name = "roleId")] int RoleId);

    #endregion Records

    #region Methods

    [HttpPost]
    public async Task<IActionResult> SetRole(
        SetRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        await _apiClient.PutAsync(
            ApiConstants.ApiMethods.SetRole,
            new Roles.SetRoleRequest(request.UserId, request.RoleId),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "users"
        });
    }

    #endregion Methods
}