using SteamStorageAPI.Application.DTOs.Auth;
using SteamStorageAPI.Application.Interfaces.Repositories;
using SteamStorageAPI.Application.Interfaces.Services;
using SteamStorageAPI.Domain.Entities;
using SteamStorageAPI.Domain.Enums;
using SteamStorageAPI.Domain.Exceptions;

namespace SteamStorageAPI.Application.UseCases.Auth;

public sealed class AuthService
{
    #region Fields

    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly ISteamApiClient _steamApiClient;
    private readonly IJwtProvider _jwtProvider;
    private readonly ICryptographyService _cryptographyService;

    #endregion

    #region Constructor

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        ISteamApiClient steamApiClient,
        IJwtProvider jwtProvider,
        ICryptographyService cryptographyService)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _steamApiClient = steamApiClient;
        _jwtProvider = jwtProvider;
        _cryptographyService = cryptographyService;
    }

    #endregion

    #region Methods

    public AuthUrlDto GetAuthUrl(string baseUrl, string? returnTo)
    {
        string group = Guid.NewGuid().ToString();

        string callbackUrl = returnTo is null
            ? $"{baseUrl}api/Authorize/SteamAuthCallback?requestInfo={group}"
            : $"{baseUrl}api/Authorize/SteamAuthCallback?requestInfo={group}_{returnTo}";

        return new AuthUrlDto(_steamApiClient.GetAuthUrl(callbackUrl, baseUrl), group);
    }

    public async Task<(string RedirectUrl, string CookieHash)> HandleCallbackAsync(
        SteamCallbackDto callback,
        string tokenReceiverUrl,
        string? storedCookieHash,
        CancellationToken ct = default)
    {
        long steamId = _steamApiClient.ParseSteamId(callback.ClaimedId);
        string expectedHash = _cryptographyService.Sha512(steamId);

        bool steamVerified = await _steamApiClient.ValidateAuthCallbackAsync(callback, ct);
        bool cookieValid = storedCookieHash == expectedHash;

        if (!steamVerified && !cookieValid)
            throw new DomainValidationException("Authorization failed Steam verification.");

        string[] parts = callback.RequestInfo.Split('_', 2);
        string group = parts[0];
        string? returnTo = parts.Length > 1 ? parts[1] : null;

        User user = await _userRepository.GetBySteamIdAsync(steamId, ct)
                    ?? await CreateUserAsync(steamId, ct);

        if (steamVerified)
        {
            SteamUserProfileDto? profile = await _steamApiClient.GetUserProfileAsync(steamId, ct);

            if (profile is not null)
            {
                user.UpdateSteamProfile(
                    profile.Username,
                    profile.IconUrl,
                    profile.IconUrlMedium,
                    profile.IconUrlFull);

                await _userRepository.UpdateAsync(user, ct);
            }
        }

        string token = _jwtProvider.Generate(user);

        string redirectUrl = returnTo is not null
            ? $"{returnTo}?Group={group}&Token={token}"
            : $"{tokenReceiverUrl}SetToken?Group={group}&Token={token}";

        return (redirectUrl, expectedHash);
    }

    public async Task<AuthTokenDto> RefreshTokenFromCookieAsync(
        long steamId, string cookieHash, CancellationToken ct = default)
    {
        string expectedHash = _cryptographyService.Sha512(steamId);

        if (cookieHash != expectedHash)
            throw new DomainValidationException("Re-authorization via Steam is required.");

        User user = await _userRepository.GetBySteamIdAsync(steamId, ct)
            ?? throw new NotFoundException("User with this SteamId was not found.");

        return new AuthTokenDto(_jwtProvider.Generate(user));
    }

    #endregion

    #region Private helpers

    private async Task<User> CreateUserAsync(long steamId, CancellationToken ct)
    {
        Role role = await _roleRepository.GetByEnumAsync(UserRole.User, ct);
        User user = new(steamId, role.Id);
        await _userRepository.AddAsync(user, ct);
        return user;
    }

    #endregion
}
