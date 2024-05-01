using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;

namespace AdminPanel.Controllers;

public class UsersController : Controller
{
    #region Fields

    private readonly ApiClient _apiClient;

    #endregion Fields

    #region Construtore

    public UsersController(
        ApiClient apiClient)
    {
        _apiClient = apiClient;
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
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    #endregion Methods
}
