namespace SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;

public interface ISteamApiUrlBuilder
{
    string GetUserInfoUrl(long steamProfileId);
    string GetUserUrl(long steamProfileId);
    string GetUserIconUrl(string urlHash);
    string GetGameIconUrl(int appId, string urlHash);
    string GetGameInfoUrl(int appId);
    string GetSkinIconUrl(string urlHash);
    string GetSkinMarketUrl(int appId, string marketHashName);
    string GetSkinsUrl(int appId, int currencyId, int count, int start);
    string GetMostPopularSkinUrl(int appId, int steamCurrencyId);
    string GetSkinInfoUrl(string marketHashName);
    string GetInventoryUrl(long steamProfileId, int appId, int count);
    string GetPriceOverviewUrl(int appId, string marketHashName, int steamCurrencyId);
    string GetAuthUrl(string returnTo, string realm);
    string GetAuthCheckUrl();

    HttpContent GetAuthCheckContent(
        string ns,
        string opEndpoint,
        string claimedId,
        string identity,
        string returnTo,
        string responseNonce,
        string assocHandle,
        string signed,
        string sig);
}