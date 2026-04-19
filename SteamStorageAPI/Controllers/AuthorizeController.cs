using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
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
    #region Constants

    private const string InternalApiKeyHeader = "X-Internal-Api-Key";
    private static readonly TimeSpan AuthCodeTtl = TimeSpan.FromSeconds(60);

    #endregion Constants

    #region Fields

    private readonly IAuthorizeService _authorizeService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtProvider _jwtProvider;
    private readonly IMemoryCache _memoryCache;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly string _tokenAddress;
    private readonly string? _publicHost;
    private readonly string _internalApiKey;

    #endregion Fields

    #region Constructor

    public AuthorizeController(
        IAuthorizeService authorizeService,
        IHttpContextAccessor httpContextAccessor,
        IJwtProvider jwtProvider,
        IMemoryCache memoryCache,
        IHttpClientFactory httpClientFactory,
        AppConfig appConfig)
    {
        _authorizeService = authorizeService;
        _httpContextAccessor = httpContextAccessor;
        _jwtProvider = jwtProvider;
        _memoryCache = memoryCache;
        _httpClientFactory = httpClientFactory;
        _tokenAddress = appConfig.App.TokenAddress;
        _publicHost = appConfig.App.PublicHost;
        _internalApiKey = appConfig.App.InternalApiKey;
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
            return Redirect(await DeliverTokenViaAuthCodeAsync(returnTo, jwt));

        return Redirect(await DeliverTokenViaSignalRAsync(group, jwt, cancellationToken));
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
        if (!_memoryCache.TryGetValue<string>(authCode, out string? jwt) || jwt is null)
            return BadRequest("Invalid or expired auth code");

        _memoryCache.Remove(authCode);

        return Ok(new ExchangeTokenResponse(jwt));
    }

    #endregion GET

    #region Private methods

    private Task<string> DeliverTokenViaAuthCodeAsync(string returnTo, string jwt)
    {
        string authCode = Guid.NewGuid().ToString("N");
        _memoryCache.Set(authCode, jwt, AuthCodeTtl);
        return Task.FromResult($"{returnTo}?authCode={authCode}");
    }

    private async Task<string> DeliverTokenViaSignalRAsync(
        string group,
        string jwt,
        CancellationToken cancellationToken)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add(InternalApiKeyHeader, _internalApiKey);

        await client.PostAsJsonAsync(
            $"{_tokenAddress}SetToken",
            new { Group = group, Token = jwt },
            cancellationToken);

        return $"{_tokenAddress}Token";
    }

    #endregion Private methods
}
