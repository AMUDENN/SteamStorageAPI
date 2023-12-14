namespace SteamStorageAPI.Models.SteamAPIModels.Skins;

public class SkinResult
{
    public string name { get; set; }
    public string hash_name { get; set; }
    public int sell_listings { get; set; }
    public int sell_price { get; set; }
    public string sell_price_text { get; set; }
    public string app_icon { get; set; }
    public string app_name { get; set; }
    public AssetDescription asset_description { get; set; }
    public string sale_price_text { get; set; }
}
