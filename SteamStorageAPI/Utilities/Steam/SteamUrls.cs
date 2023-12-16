namespace SteamStorageAPI.Utilities.Steam
{
    public static class SteamUrls
    {
        public static string GetGameIconUrl(int appId, string urlHash) => 
            $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{appId}/{urlHash}.jpg";
        public static string GetGameInfoUrl(int appId) => 
            $"https://store.steampowered.com/api/libraryappdetails/?appid={appId}";
        public static string GetSkinIconUrl(string urlHash) => 
            $"https://community.cloudflare.steamstatic.com/economy/image/{urlHash}";
        public static string GetSkinMarketUrl(int appId, string marketHashName) => 
            $"https://steamcommunity.com/market/listings/{appId}/{marketHashName}";
        public static string GetSkinsUrl(int appId, int count, int start) =>
            $"https://steamcommunity.com/market/search/render?q=&norender=1&search_descriptions=0&l=russian&appid={appId}&count={count}&start={start}";
        public static string GetSkinInfo(string marketHashName) =>
            $"https://steamcommunity.com/market/search/render?norender=1&l=russian&start=0&count=1&query={marketHashName}";
        public static string GetInventoryUrl(long steamProfileId, int appId, int count) =>
            @$"https://steamcommunity.com/inventory/{steamProfileId}/{appId}/2?l=russian&count={count}";
        public static string GetPriceOverviewUrl(int appId, string marketHashName, int steamCurrencyId) =>
            $@"https://steamcommunity.com/market/priceoverview/?appid={appId}&market_hash_name={marketHashName}&currency={steamCurrencyId}";
        public static string GetAuthUrl(string returnTo, string realm) =>
            "https://steamcommunity.com/openid/login" +
            "?openid.ns=http://specs.openid.net/auth/2.0" +
            "&openid.mode=checkid_setup" +
            $"&openid.return_to={returnTo}" +
            $"&openid.realm={realm}" +
            "&openid.identity=http://specs.openid.net/auth/2.0/identifier_select" +
            "&openid.claimed_id=http://specs.openid.net/auth/2.0/identifier_select";
        public static string GetAuthCkeckUrl(string opEndpoint, string claimedId, string identity, string returnTo, string responseNonce, string assocHandle, string signed, string sig) =>
            "https://steamcommunity.com/openid/login" +
            "?openid.ns=http://specs.openid.net/auth/2.0" +
            "&openid.mode=check_authentication" +
            $"&openid.op_endpoint={opEndpoint}" +
            $"&openid.claimed_id={claimedId}" +
            $"&openid.identity={identity}" +
            $"&openid.return_to={returnTo}" +
            $"&openid.response_nonce={responseNonce}" +
            $"&openid.assoc_handle={assocHandle}" +
            $"&openid.signed={signed}" +
            $"&openid.sig={sig}";
    }
}
