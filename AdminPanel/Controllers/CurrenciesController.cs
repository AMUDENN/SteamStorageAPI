using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities;

namespace AdminPanel.Controllers;

public class CurrenciesController : Controller
{
    #region Fields

    private readonly ApiClient _apiClient;

    #endregion Fields

    #region Construtore

    public CurrenciesController(
        ApiClient apiClient)
    {
        _apiClient = apiClient;
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
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.PostAsync(
            ApiConstants.ApiMethods.PostCurrency,
            new Currencies.PostCurrencyRequest(
                request.SteamCurrencyId,
                request.Title,
                request.Mark,
                request.CultureInfo),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutCurrency(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.PutAsync(
            ApiConstants.ApiMethods.PutCurrencyInfo,
            new Currencies.PutCurrencyRequest(
                request.CurrencyId,
                request.Title,
                request.Mark,
                request.CultureInfo),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCurrency(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;

        await _apiClient.DeleteAsync(
            ApiConstants.ApiMethods.DeleteCurrency,
            new Currencies.DeleteCurrencyRequest(request.CurrencyId),
            cancellationToken);

        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    #endregion Methods
}
