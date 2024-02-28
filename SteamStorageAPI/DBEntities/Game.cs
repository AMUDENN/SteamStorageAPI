namespace SteamStorageAPI.DBEntities;

public class Game
{
    #region Constanst

    public const int BASE_GAME_ID = 3;
    
    #endregion Constants
    
    #region Properties

    public int Id { get; set; }

    public int SteamGameId { get; set; }

    public string Title { get; set; } = null!;

    public string GameIconUrl { get; set; } = null!;

    public virtual ICollection<Skin> Skins { get; set; } = new List<Skin>();

    #endregion Properties
}
