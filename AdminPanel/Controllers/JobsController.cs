using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK.ApiClient;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities.ApiControllers;

namespace AdminPanel.Controllers;

public class JobsController : Controller
{
    #region Fields

    private readonly IApiClient _apiClient;
    private readonly ICookiesUserService _cookieUserService;

    #endregion Fields

    #region Constructor

    public JobsController(
        IApiClient apiClient,
        ICookiesUserService cookieUserService)
    {
        _apiClient = apiClient;
        _cookieUserService = cookieUserService;
    }

    #endregion Constructor

    #region Methods

    [HttpPost]
    public async Task<IActionResult> TriggerRefreshSkinDynamics(CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;
        try
        {
            await _apiClient.PostAsync(
                ApiConstants.ApiMethods.TriggerJob,
                new Jobs.TriggerJobRequest(Jobs.JobName.RefreshSkinDynamicsJob),
                cancellationToken);
            return Json(new { ok = true, message = "RefreshSkinDynamics triggered" });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TriggerRefreshCurrencies(CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;
        try
        {
            await _apiClient.PostAsync(
                ApiConstants.ApiMethods.TriggerJob,
                new Jobs.TriggerJobRequest(Jobs.JobName.RefreshCurrenciesJob),
                cancellationToken);
            return Json(new { ok = true, message = "RefreshCurrencies triggered" });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> TriggerRefreshActiveGroupsDynamics(CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;
        try
        {
            await _apiClient.PostAsync(
                ApiConstants.ApiMethods.TriggerJob,
                new Jobs.TriggerJobRequest(Jobs.JobName.RefreshActiveGroupsDynamicsJob),
                cancellationToken);
            return Json(new { ok = true, message = "RefreshActiveGroupsDynamics triggered" });
        }
        catch (Exception ex)
        {
            return Json(new { ok = false, error = ex.Message });
        }
    }

    #endregion Methods
}