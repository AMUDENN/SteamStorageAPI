using AdminPanel.Utilities;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK;

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
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> PutCurrency(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCurrency(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        HttpContext.Request.Cookies.TryGetValue(ProgramConstants.JWT_COOKIES, out string? token);
        _apiClient.Token = token ?? string.Empty;
        
        return RedirectToAction(nameof(AdminPanel), nameof(AdminPanel));
    }
    
    #endregion Methods
}
