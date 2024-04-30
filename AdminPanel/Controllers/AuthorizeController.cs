using System.Diagnostics;
using AdminPanel.Models;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities;

namespace AdminPanel.Controllers;

public class AuthorizeController : Controller
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ApiClient _apiClient;

    #endregion Fields

    #region Construtore

    public AuthorizeController(
        IHttpContextAccessor httpContextAccessor,
        ApiClient apiClient)
    {
        _httpContextAccessor = httpContextAccessor;
        _apiClient = apiClient;
    }

    #endregion Constructor

    #region Methods

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> LogIn(
        CancellationToken cancellationToken = default)
    {
        string baseUrl =
            $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/admin";

        Authorize.AuthUrlResponse? authUrlResponse =
            await _apiClient.GetAsync<Authorize.AuthUrlResponse, Authorize.GetAuthUrlRequest>(
                ApiConstants.ApiMethods.GetAuthUrl,
                new($"{baseUrl}/AdminPanel/CheckAdmin"), 
                cancellationToken);

        if (authUrlResponse is null)
            return RedirectToAction(nameof(LogIn));

        return Redirect(authUrlResponse.Url);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }

    #endregion Methods
}
