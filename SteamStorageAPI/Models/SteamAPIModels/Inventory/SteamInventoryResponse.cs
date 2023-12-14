namespace SteamStorageAPI.Models.SteamAPIModels.Inventory
{
    public class SteamInventoryResponse
    {
        public InventoryAsset[] assets { get; set; }
        public InventoryDescription[] descriptions { get; set; }
        public int total_inventory_count { get; set; }
        public int success { get; set; }
        public int rwgrsn { get; set; }
    }
}
