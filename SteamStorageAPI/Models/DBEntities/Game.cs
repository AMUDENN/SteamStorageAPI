namespace SteamStorageAPI.Models.DBEntities;

public class Game
{
    #region Properties

    public int Id { get; set; }

    public int SteamGameId { get; set; }
    
    public string Title { get; set; } = null!;

    public string GameIconUrl { get; set; } = null!;
    
    public bool IsBase { get; set; }

    public virtual ICollection<Skin> Skins { get; set; } = new List<Skin>();

    #endregion Properties
}