namespace SteamStorageAPI.Utilities.Steam
{
    public static class SteamUrls
    {
        public static string GetGameIconUrl(int appId, string urlHash) => 
            $"https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/{appId}/{urlHash}.jpg";
        public static string GetSkinIconUrl(string urlHash) => 
            $"https://community.cloudflare.steamstatic.com/economy/image/{urlHash}";
        public static string GetSkinMarketUrl(int appId, string marketHashName) => 
            $"https://steamcommunity.com/market/listings/{appId}/{marketHashName}";
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
