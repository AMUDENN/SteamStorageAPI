using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Config;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.AuthorizeService;

public class AuthorizeService : IAuthorizeService
{
    #region Constants

    private static readonly TimeSpan AuthCodeTtl = TimeSpan.FromSeconds(60);

    #endregion Constants

    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _memoryCache;
    private readonly SteamStorageContext _context;

    private readonly string _tokenAddress;
    private readonly string _internalApiKey;

    private const string InternalApiKeyHeader = "X-Internal-Api-Key";

    #endregion Fields

    #region Constructor

    public AuthorizeService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        IMemoryCache memoryCache,
        SteamStorageContext context,
        AppConfig appConfig)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _memoryCache = memoryCache;
        _context = context;
        _tokenAddress = appConfig.App.TokenAddress;
        _internalApiKey = appConfig.App.InternalApiKey;
    }

    #endregion Constructor

    #region Methods

    public async Task<User> GetOrCreateUserAsync(long steamId, CancellationToken cancellationToken = default)
    {
        User? user = await _context.Users
            .Include(x => x.Role)
            .FirstOrDefaultAsync(x => x.SteamId == steamId, cancellationToken);

        if (user is not null)
            return user;

        Role role = await _context.Roles.FirstAsync(x => x.Title == nameof(Role.Roles.User), cancellationToken);

        user = new User
        {
            SteamId = steamId,
            RoleId = role.Id,
            Role = role,
            StartPageId = Page.BASE_START_PAGE_ID,
            CurrencyId = Currency.BASE_CURRENCY_ID,
            DateRegistration = DateTime.UtcNow
        };

        await UpdateSteamProfileAsync(user, cancellationToken);

        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return user;
    }

    public (string Url, string Group) GetSteamAuthInfo(string scheme, string host, string? returnTo = null)
    {
        if (returnTo is not null
            && (!Uri.TryCreate(returnTo, UriKind.Absolute, out Uri? returnUri)
                || returnUri.Scheme != Uri.UriSchemeHttp && returnUri.Scheme != Uri.UriSchemeHttps))
            throw new HttpResponseException(StatusCodes.Status400BadRequest,
                "returnTo must be an absolute http/https URL");

        string group = Guid.NewGuid().ToString();
        string baseUrl = $"{scheme}://{host}/";
        string callbackParam = returnTo is null
            ? group
            : $"{group}_{returnTo}";

        string url = _steamApiUrlBuilder.GetAuthUrl(
            $"{baseUrl}api/Authorize/SteamAuthCallback?requestInfo={callbackParam}",
            baseUrl);

        return (url, group);
    }

    public async Task<bool> ValidateSteamAuthAsync(
        string ns,
        string opEndpoint,
        string claimedId,
        string identity,
        string returnTo,
        string responseNonce,
        string assocHandle,
        string signed,
        string sig,
        CancellationToken cancellationToken = default)
    {
        using HttpClient client = _httpClientFactory.CreateClient();

        HttpRequestMessage request = new(HttpMethod.Post, _steamApiUrlBuilder.GetAuthCheckUrl())
        {
            Content = _steamApiUrlBuilder.GetAuthCheckContent(
                ns, opEndpoint, claimedId, identity,
                returnTo, responseNonce, assocHandle, signed, sig)
        };

        HttpResponseMessage response =
            await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        string strResponse = await response.Content.ReadAsStringAsync(cancellationToken);

        return Convert.ToBoolean(strResponse[(strResponse.LastIndexOf(':') + 1)..]);
    }

    public async Task<string> DeliverTokenViaSignalRAsync(string group, string jwt, CancellationToken cancellationToken = default)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add(InternalApiKeyHeader, _internalApiKey);

        await client.PostAsJsonAsync(
            $"{_tokenAddress}SetToken",
            new
            {
                Group = group,
                Token = jwt
            },
            cancellationToken);

        return $"{_tokenAddress}Token";
    }

    public string DeliverTokenViaAuthCode(string returnTo, string jwt)
    {
        string authCode = Guid.NewGuid().ToString("N");
        _memoryCache.Set(authCode, jwt, AuthCodeTtl);
        return $"{returnTo}?authCode={authCode}";
    }

    public string? ExchangeAuthCode(string authCode)
    {
        if (!_memoryCache.TryGetValue<string>(authCode, out string? jwt))
            return null;
        _memoryCache.Remove(authCode);
        return jwt;
    }

    private async Task UpdateSteamProfileAsync(User user, CancellationToken cancellationToken)
    {
        using HttpClient client = _httpClientFactory.CreateClient();
        SteamUserResult? result =
            await client.GetFromJsonAsync<SteamUserResult>(
                _steamApiUrlBuilder.GetUserInfoUrl(user.SteamId), cancellationToken);

        SteamUser? steamUser = result?.response?.players?.FirstOrDefault();
        if (steamUser is null) return;

        user.Username = steamUser.personaname;
        user.IconUrl = steamUser.avatar?.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.IconUrlMedium = steamUser.avatarmedium?.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.IconUrlFull = steamUser.avatarfull?.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.DateUpdate = DateTime.UtcNow;
    }

    #endregion Methods
}