using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.AuthorizeService;
using SteamStorageAPI.Services.Infrastructure.JwtProvider;
using SteamStorageAPI.Utilities.Config;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthorizeController : ControllerBase
{
    #region Fields

    private readonly IAuthorizeService _authorizeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtProvider _jwtProvider;

    private readonly string _tokenAddress;

    #endregion Fields

    #region Constructor

    public AuthorizeController(
        IAuthorizeService authorizeService,
        IHttpContextAccessor httpContextAccessor,
        IJwtProvider jwtProvider,
        AppConfig appConfig)
    {
        _authorizeService = authorizeService;
        _httpContextAccessor = httpContextAccessor;
        _jwtProvider = jwtProvider;
        _tokenAddress = appConfig.App.TokenAddress;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение ссылки на авторизацию и названия группы SignalR
    /// </summary>
    /// <response code="200">Возвращает ссылку на авторизацию и название группы SignalR</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="499">Операция отменена</response>
    [HttpGet(Name = "GetAuthUrl")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<AuthUrlResponse> GetAuthUrl(
        [FromQuery] GetAuthUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        string scheme = _httpContextAccessor.HttpContext?.Request.Scheme ?? "https";
        string host = _httpContextAccessor.HttpContext?.Request.Host.ToString() ?? string.Empty;

        (string Url, string Group) steamAuth = _authorizeService.GetSteamAuthInfo(scheme, host, request.ReturnTo);

        return Ok(new AuthUrlResponse(steamAuth.Url, steamAuth.Group));
    }

    /// <summary>
    /// Callback авторизации в Steam
    /// </summary>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="499">Операция отменена</response>
    [HttpGet(Name = "SteamAuthCallback")]
    public async Task<ActionResult> SteamAuthCallback(
        [FromQuery] SteamAuthRequest steamAuthRequest,
        CancellationToken cancellationToken = default)
    {
        string[] info = steamAuthRequest.RequestInfo.Split('_');
        string group = info.First();
        string? returnTo = info.Length > 1 ? info[1] : null;

        bool authResult = await _authorizeService.ValidateSteamAuthAsync(
            steamAuthRequest.Ns,
            steamAuthRequest.OpEndpoint,
            steamAuthRequest.ClaimedId,
            steamAuthRequest.Identity,
            steamAuthRequest.ReturnTo,
            steamAuthRequest.ResponseNonce,
            steamAuthRequest.AssocHandle,
            steamAuthRequest.Signed,
            steamAuthRequest.Sig,
            cancellationToken);

        if (!authResult)
            return BadRequest("Steam auth failed");

        long steamId =
            Convert.ToInt64(steamAuthRequest.ClaimedId[(steamAuthRequest.ClaimedId.LastIndexOf('/') + 1)..]);

        User user = await _authorizeService.GetOrCreateUserAsync(steamId, cancellationToken);

        return Redirect(returnTo is not null
            ? $"{returnTo}?Group={group}&Token={_jwtProvider.Generate(user)}"
            : $"{_tokenAddress}SetToken?Group={group}&Token={_jwtProvider.Generate(user)}");
    }

    #endregion GET
}