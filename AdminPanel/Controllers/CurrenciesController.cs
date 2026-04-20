using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK.ApiClient;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities.ApiControllers;

namespace AdminPanel.Controllers;

public class CurrenciesController : Controller
{
    #region Fields

    private readonly IApiClient _apiClient;
    private readonly ICookiesUserService _cookieUserService;

    #endregion Fields

    #region Construtore

    public CurrenciesController(
        IApiClient apiClient,
        ICookiesUserService cookieUserService)
    {
        _apiClient = apiClient;
        _cookieUserService = cookieUserService;
    }

    #endregion Constructor

    #region Records

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

    #endregion Records

    #region Methods

    [HttpPost]
    public async Task<IActionResult> AddCurrency(
        AddCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;

        await _apiClient.PostAsync(
            ApiConstants.ApiMethods.PostCurrency,
            new Currencies.PostCurrencyRequest(
                request.SteamCurrencyId,
                request.Title,
                request.Mark,
                request.CultureInfo),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "currencies"
        });
    }

    [HttpPost]
    public async Task<IActionResult> PutCurrency(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;

        await _apiClient.PutAsync(
            ApiConstants.ApiMethods.PutCurrencyInfo,
            new Currencies.PutCurrencyRequest(
                request.CurrencyId,
                request.Title,
                request.Mark,
                request.CultureInfo),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "currencies"
        });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCurrency(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken() ?? string.Empty;

        await _apiClient.DeleteAsync(
            ApiConstants.ApiMethods.DeleteCurrency,
            new Currencies.DeleteCurrencyRequest(request.CurrencyId),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanelController.AdminPanel), "AdminPanel", new
        {
            tab = "currencies"
        });
    }

    #endregion Methods
}