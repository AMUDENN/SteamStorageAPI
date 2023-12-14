namespace SteamStorageAPI.Models.SteamAPIModels.Skins;

public class SteamSkinResponse
{
    public bool success { get; set; }
    public int start { get; set; }
    public int pagesize { get; set; }
    public int total_count { get; set; }
    public SearchData searchdata { get; set; }
    public SkinResult[] results { get; set; }
}
