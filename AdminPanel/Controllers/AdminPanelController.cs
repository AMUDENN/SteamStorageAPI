using System.Net.Http.Headers;
using AdminPanel.Models;
using AdminPanel.Services.CookiesUserService;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.SDK.ApiClient;
using SteamStorageAPI.SDK.ApiEntities;
using SteamStorageAPI.SDK.Utilities.ApiControllers;

namespace AdminPanel.Controllers;

public class AdminPanelController : Controller
{
    #region Fields

    private readonly IApiClient _apiClient;
    private readonly ICookiesUserService _cookieUserService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AdminPanelOptions _options;

    #endregion Fields

    #region Constructor

    public AdminPanelController(
        IApiClient apiClient,
        ICookiesUserService cookieUserService,
        IHttpClientFactory httpClientFactory,
        AdminPanelOptions options)
    {
        _apiClient = apiClient;
        _cookieUserService = cookieUserService;
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    #endregion Constructor

    #region Records

    public record TokenRequest(
        string Group,
        string Token);

    public record AdminPanelRequest(
        [FromForm(Name = "usersPageNumber")] int? UsersPageNumber,
        [FromQuery(Name = "tab")] string? Tab,
        [FromQuery(Name = "userId")] int? SearchUserId,
        [FromQuery(Name = "nickname")] string? SearchNickname,
        [FromQuery(Name = "steamId")] int? SearchSteamId);

    #endregion Records

    #region Methods

    public IActionResult CheckAdmin(
        [FromQuery] TokenRequest request)
    {
        _cookieUserService.SetCookiesToken(request.Token);
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
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

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
            new Users.GetUsersRequest(request.UsersPageNumber ?? 1, 10, request.SearchUserId, request.SearchNickname, request.SearchSteamId),
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
            UsersCount = users?.Count ?? 0,
            UsersPageNumber = request.UsersPageNumber ?? 1,
            UsersPagesCount = users?.PagesCount ?? 1,
            Users = users?.Users?.ToList() ?? [],
            ActiveTab = request.Tab ?? "currencies",
            SearchUserId = request.SearchUserId,
            SearchNickname = request.SearchNickname,
            SearchSteamId = request.SearchSteamId
        });
    }

    public async Task<IActionResult> CurrencyDynamicsProxy(
        [FromQuery] int currencyId,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        Currencies.CurrencyDynamicsResponse? dynamics =
            await _apiClient.GetAsync<Currencies.CurrencyDynamicsResponse, Currencies.GetCurrencyDynamicsRequest>(
                ApiConstants.ApiMethods.GetCurrencyDynamics,
                new Currencies.GetCurrencyDynamicsRequest(currencyId),
                cancellationToken);

        if (dynamics is null)
            return Content("""{"dynamic":[]}""", "application/json");

        return Json(dynamics);
    }

    public async Task<IActionResult> UsersCountByCurrencyProxy(
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        Statistics.UsersCountByCurrencyResponse? result =
            await _apiClient.GetAsync<Statistics.UsersCountByCurrencyResponse>(
                ApiConstants.ApiMethods.GetUsersCountByCurrency,
                cancellationToken);

        if (result is null)
            return Content("""{"items":[]}""", "application/json");

        return Json(result);
    }

    public async Task<IActionResult> GameStatsProxy(
        [FromQuery] int gameId,
        CancellationToken cancellationToken = default)
    {
        _apiClient.Token = _cookieUserService.GetCookiesToken(cancellationToken) ?? string.Empty;

        Skins.SteamSkinsCountResponse? skinsCountResponse =
            await _apiClient.GetAsync<Skins.SteamSkinsCountResponse, Skins.GetSteamSkinsCountRequest>(
                ApiConstants.ApiMethods.GetSteamSkinsCount,
                new Skins.GetSteamSkinsCountRequest(gameId),
                cancellationToken);

        Statistics.ItemsCountResponse? itemsCountResponse =
            await _apiClient.GetAsync<Statistics.ItemsCountResponse, Statistics.GetItemsCountByGameRequest>(
                ApiConstants.ApiMethods.GetItemsCountByGame,
                new Statistics.GetItemsCountByGameRequest(gameId),
                cancellationToken);

        int? skinsCount = skinsCountResponse?.Count;
        int? itemsCount = itemsCountResponse?.Count;

        return Content(
            $$"""{"skinsCount":{{(skinsCount.HasValue ? skinsCount.ToString() : "null")}},"itemsCount":{{(itemsCount.HasValue ? itemsCount.ToString() : "null")}}}""",
            "application/json");
    }

    public async Task<IActionResult> HealthProxy(CancellationToken cancellationToken = default)
    {
        string? token = _cookieUserService.GetCookiesToken(cancellationToken);

        using HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);

        if (!string.IsNullOrEmpty(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            HttpResponseMessage response = await client.GetAsync(
                $"{_options.ApiAddress}/health-all", cancellationToken);

            string content = await response.Content.ReadAsStringAsync(cancellationToken);
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            return Content(
                $$"""{"status":"Unavailable","totalDuration":"00:00:00","entries":{},"error":"{{ex.Message}}"}""",
                "application/json");
        }
    }

    #endregion Methods
}
