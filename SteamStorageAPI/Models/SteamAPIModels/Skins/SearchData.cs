namespace SteamStorageAPI.Models.SteamAPIModels.Skins;

public class SearchData
{
    public string query { get; set; }
    public bool search_descriptions { get; set; }
    public int total_count { get; set; }
    public int pagesize { get; set; }
    public string prefix { get; set; }
    public string class_prefix { get; set; }
}
