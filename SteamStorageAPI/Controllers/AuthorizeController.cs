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

    private readonly string? _publicHost;

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
        _publicHost = appConfig.App.PublicHost;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Get the authorization URL and the SignalR group name
    /// </summary>
    /// <response code="200">Returns the authorization URL and the SignalR group name</response>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="499">The operation was cancelled</response>
    [HttpGet(Name = "GetAuthUrl")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<AuthUrlResponse> GetAuthUrl(
        [FromQuery] GetAuthUrlRequest request,
        CancellationToken cancellationToken = default)
    {
        string scheme = _httpContextAccessor.HttpContext?.Request.Scheme ?? "https";
        string host = _publicHost ?? _httpContextAccessor.HttpContext?.Request.Host.ToString() ?? string.Empty;

        (string Url, string Group) steamAuth = _authorizeService.GetSteamAuthInfo(scheme, host, request.ReturnTo);

        return Ok(new AuthUrlResponse(steamAuth.Url, steamAuth.Group));
    }

    /// <summary>
    /// Steam authorization callback
    /// </summary>
    /// <response code="400">An error occurred during method execution (see description)</response>
    /// <response code="499">The operation was cancelled</response>
    [HttpGet(Name = "SteamAuthCallback")]
    public async Task<ActionResult> SteamAuthCallback(
        [FromQuery] SteamAuthRequest steamAuthRequest,
        CancellationToken cancellationToken = default)
    {
        int separatorIndex = steamAuthRequest.RequestInfo.IndexOf('_');
        string group = separatorIndex < 0
            ? steamAuthRequest.RequestInfo
            : steamAuthRequest.RequestInfo[..separatorIndex];
        string? returnTo = separatorIndex < 0
            ? null
            : steamAuthRequest.RequestInfo[(separatorIndex + 1)..];

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
        string jwt = _jwtProvider.Generate(user);

        if (returnTo is not null)
            return Redirect(_authorizeService.DeliverTokenViaAuthCode(returnTo, jwt));

        return Redirect(await _authorizeService.DeliverTokenViaSignalRAsync(group, jwt, cancellationToken));
    }

    /// <summary>
    /// Exchange a short-lived auth code for a JWT (one-time use, expires in 60 seconds)
    /// </summary>
    /// <response code="200">Returns the JWT</response>
    /// <response code="400">Invalid or expired auth code</response>
    [HttpGet(Name = "ExchangeToken")]
    [Produces(MediaTypeNames.Application.Json)]
    public ActionResult<ExchangeTokenResponse> ExchangeToken([FromQuery] string authCode)
    {
        string? jwt = _authorizeService.ExchangeAuthCode(authCode);
        if (jwt is null)
            return BadRequest("Invalid or expired auth code");

        return Ok(new ExchangeTokenResponse(jwt));
    }

    #endregion GET
}