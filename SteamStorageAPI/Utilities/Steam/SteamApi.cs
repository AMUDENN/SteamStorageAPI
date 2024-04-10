using SteamStorageAPI.DBEntities;

namespace SteamStorageAPI.Utilities.Steam
{
    public static class SteamApi
    {
        #region Fields

        private static readonly Dictionary<string, string> _replaceChars = new()
        {
            [" "] = "%20",
            ["'"] = "%27"
        };

        #endregion Fields

        #region Properties

        private static string SteamApiKey { get; set; } = string.Empty;

        #endregion Properties

        #region Methods

        public static void Initialize(IConfiguration configuration)
        {
            IConfigurationSection steamSection = configuration.GetSection("Steam");

            SteamApiKey = steamSection.GetValue<string>("ApiKey") ??
                          throw new ArgumentNullException($"{nameof(SteamApi)} {nameof(SteamApiKey)}");
        }

        public static string GetUserInfoUrl(long steamProfileId) =>
            $"https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/?key={SteamApiKey}&steamids={steamProfileId}";

        public static string GetUserUrl(long steamProfileId) =>
            $"https://steamcommunity.com/profiles/{steamProfileId}";

        public static string GetUserIconUrl(string urlHash) =>
            $"https://avatars.steamstatic.com/{urlHash}";

        public static string GetGameIconUrl(int appId, string urlHash) =>
            $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{appId}/{urlHash}.jpg";

        public static string GetGameInfoUrl(int appId) =>
            $"https://store.steampowered.com/api/libraryappdetails/?appid={appId}";

        public static string GetSkinIconUrl(string urlHash) =>
            $"https://community.cloudflare.steamstatic.com/economy/image/{urlHash}";

        public static string GetSkinMarketUrl(int appId, string marketHashName) =>
            $"https://steamcommunity.com/market/listings/{appId}/{ReplaceMarketHashName(marketHashName)}";

        public static string GetSkinsUrl(int appId, int currencyId, int count, int start) =>
            $"https://steamcommunity.com/market/search/render?q=&norender=1&search_descriptions=0&l=russian&appid={appId}&count={count}&start={start}&currency={currencyId}";

        public static string GetMostPopularSkinUrl(int appId) =>
            GetSkinsUrl(appId, Currency.BASE_CURRENCY_ID, 1, 0);

        public static string GetSkinInfoUrl(string marketHashName) =>
            $"https://steamcommunity.com/market/search/render?norender=1&l=russian&start=0&count=1&query={ReplaceMarketHashName(marketHashName)}";

        public static string GetInventoryUrl(long steamProfileId, int appId, int count) =>
            $"https://steamcommunity.com/inventory/{steamProfileId}/{appId}/2?l=russian&count={count}";

        public static string GetPriceOverviewUrl(int appId, string marketHashName, int steamCurrencyId) =>
            $"https://steamcommunity.com/market/priceoverview/?appid={appId}&market_hash_name={ReplaceMarketHashName(marketHashName)}&currency={steamCurrencyId}";

        public static string GetAuthUrl(string returnTo, string realm) =>
            "https://steamcommunity.com/openid/login" +
            "?openid.ns=http://specs.openid.net/auth/2.0" +
            "&openid.mode=checkid_setup" +
            $"&openid.return_to={returnTo}" +
            $"&openid.realm={realm}" +
            "&openid.identity=http://specs.openid.net/auth/2.0/identifier_select" +
            "&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";

        public static string GetAuthCheckUrl() => "https://steamcommunity.com/openid/login";

        public static HttpContent GetAuthCheckContent(string ns, string opEndpoint, string claimedId, string identity,
            string returnTo, string responseNonce, string assocHandle, string signed, string sig)
        {
            Dictionary<string, string> formData = new()
            {
                ["openid.ns"] = ns,
                ["openid.mode"] = "check_authentication",
                ["openid.op_endpoint"] = opEndpoint,
                ["openid.claimed_id"] = claimedId,
                ["openid.identity"] = identity,
                ["openid.return_to"] = returnTo,
                ["openid.response_nonce"] = responseNonce,
                ["openid.assoc_handle"] = assocHandle,
                ["openid.signed"] = signed,
                ["openid.sig"] = sig
            };

            return new FormUrlEncodedContent(formData);
        }

        private static string ReplaceMarketHashName(string marketHashName)
        {
            return _replaceChars.Aggregate(marketHashName,
                (current, replaceChar) => current.Replace(replaceChar.Key, replaceChar.Value));
        }

        #endregion Methods
    }
}
