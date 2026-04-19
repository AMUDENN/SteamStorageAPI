using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Domain.AuthorizeService;

public interface IAuthorizeService
{
    Task<User> GetOrCreateUserAsync(long steamId, CancellationToken cancellationToken = default);

    (string Url, string Group) GetSteamAuthInfo(string scheme, string host, string? returnTo = null);

    Task<bool> ValidateSteamAuthAsync(
        string ns,
        string opEndpoint,
        string claimedId,
        string identity,
        string returnTo,
        string responseNonce,
        string assocHandle,
        string signed,
        string sig,
        CancellationToken cancellationToken = default);

    Task<string> DeliverTokenViaSignalRAsync(string group, string jwt, CancellationToken cancellationToken = default);

    string DeliverTokenViaAuthCode(string returnTo, string jwt);

    string? ExchangeAuthCode(string authCode);
}