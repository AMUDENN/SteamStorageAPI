namespace SteamStorageAPI.Models.SteamAPIModels.Inventory
{
    public class InventoryDescription
    {
        public int appid { get; set; }
        public string classid { get; set; }
        public string instanceid { get; set; }
        public int currency { get; set; }
        public string background_color { get; set; }
        public string icon_url { get; set; }
        public string icon_url_large { get; set; }
        public InventoryDescriptionDetailed[] descriptions { get; set; }
        public int tradable { get; set; }
        public SkinAction[] actions { get; set; }
        public string name { get; set; }
        public string name_color { get; set; }
        public string type { get; set; }
        public string market_name { get; set; }
        public string market_hash_name { get; set; }
        public SkinMarketActions[] market_actions { get; set; }
        public int commodity { get; set; }
        public int market_tradable_restriction { get; set; }
        public int marketable { get; set; }
        public SkinTag[] tags { get; set; }
        public string[] fraudwarnings { get; set; }
    }
}
