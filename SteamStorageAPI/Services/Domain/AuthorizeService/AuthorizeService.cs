using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;

namespace SteamStorageAPI.Services.Domain.AuthorizeService;

public class AuthorizeService : IAuthorizeService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public AuthorizeService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        IHttpClientFactory httpClientFactory,
        SteamStorageContext context)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _httpClientFactory = httpClientFactory;
        _context = context;
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
        HttpClient client = _httpClientFactory.CreateClient();

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

    private async Task UpdateSteamProfileAsync(User user, CancellationToken cancellationToken)
    {
        HttpClient client = _httpClientFactory.CreateClient();
        SteamUserResult? result =
            await client.GetFromJsonAsync<SteamUserResult>(
                _steamApiUrlBuilder.GetUserInfoUrl(user.SteamId), cancellationToken);

        SteamUser? steamUser = result?.response.players.FirstOrDefault();
        if (steamUser is null) return;

        user.Username = steamUser.personaname;
        user.IconUrl = steamUser.avatar.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.IconUrlMedium = steamUser.avatarmedium.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.IconUrlFull = steamUser.avatarfull.Replace("https://avatars.steamstatic.com/", string.Empty);
        user.DateUpdate = DateTime.UtcNow;
    }

    #endregion Methods
}