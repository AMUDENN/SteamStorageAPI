using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Models.SteamAPIModels.User;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Exceptions;

namespace SteamStorageAPI.Services.Domain.UserService;

public class UserService : IUserService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public UserService(
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

    private UserResponse GetUserResponse(User user)
    {
        return new UserResponse(user.Id,
            user.SteamId.ToString(),
            _steamApiUrlBuilder.GetUserUrl(user.SteamId),
            user.IconUrl is null ? null : _steamApiUrlBuilder.GetUserIconUrl(user.IconUrl),
            user.IconUrlMedium is null ? null : _steamApiUrlBuilder.GetUserIconUrl(user.IconUrlMedium),
            user.IconUrlFull is null ? null : _steamApiUrlBuilder.GetUserIconUrl(user.IconUrlFull),
            user.Username?.Trim([' ']),
            user.RoleId,
            user.Role.Title,
            user.StartPageId,
            user.StartPage.Title,
            user.CurrencyId,
            user.DateRegistration,
            user.GoalSum);
    }

    private async Task<UserResponse> RefreshAndGetUserResponseAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        if (user.Username is null
            || user.IconUrl is null
            || user.IconUrlMedium is null
            || user.IconUrlFull is null
            || user.DateUpdate.HasValue && user.DateUpdate.Value < DateTime.Now.AddDays(-1))
        {
            HttpClient client = _httpClientFactory.CreateClient();
            SteamUserResult? steamUserResult =
                await client.GetFromJsonAsync<SteamUserResult>(
                    _steamApiUrlBuilder.GetUserInfoUrl(user.SteamId), cancellationToken);

            if (steamUserResult is not null)
            {
                SteamUser? steamUser = steamUserResult.response?.players?.FirstOrDefault();
                user.Username = steamUser?.personaname;
                user.IconUrl = steamUser?.avatar?.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlMedium = steamUser?.avatarmedium?.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.IconUrlFull = steamUser?.avatarfull?.Replace("https://avatars.steamstatic.com/", string.Empty);
                user.DateUpdate = DateTime.Now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return GetUserResponse(user);
    }

    public async Task<UsersResponse> GetUsersAsync(
        GetUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> users = _context.Users.AsNoTracking();
        int usersCount = await users.CountAsync(cancellationToken);
        int pagesCount = (int)Math.Ceiling((double)usersCount / request.PageSize);

        users = users
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Include(x => x.Role)
            .Include(x => x.StartPage);

        return new UsersResponse(
            usersCount,
            pagesCount == 0 ? 1 : pagesCount,
            users.Select(x => GetUserResponse(x)));
    }

    public async Task<int> GetUsersCountAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Users.CountAsync(cancellationToken);
    }

    public async Task<UserResponse> GetUserInfoAsync(
        GetUserRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _context.Users
                        .Include(x => x.Role)
                        .Include(x => x.StartPage)
                        .FirstOrDefaultAsync(x => x.Id == request.UserId, cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "A user with this Id does not exist");

        return await RefreshAndGetUserResponseAsync(user, cancellationToken);
    }

    public async Task<UserResponse> GetCurrentUserInfoAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);
        await _context.Entry(user).Reference(x => x.StartPage).LoadAsync(cancellationToken);

        return await RefreshAndGetUserResponseAsync(user, cancellationToken);
    }

    public GoalSumResponse GetCurrentUserGoalSum(
        User user)
    {
        return new GoalSumResponse(user.GoalSum);
    }

    public async Task<HasAccessToAdminPanelResponse> GetHasAccessToAdminPanelAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Entry(user).Reference(x => x.Role).LoadAsync(cancellationToken);

        return new HasAccessToAdminPanelResponse(user.Role.Title == nameof(Role.Roles.Admin));
    }

    public async Task PutGoalSumAsync(
        User user,
        PutGoalSumRequest request,
        CancellationToken cancellationToken = default)
    {
        user.GoalSum = request.GoalSum;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteUserAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
    }

    #endregion Methods
}