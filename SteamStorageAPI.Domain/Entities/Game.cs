namespace SteamStorageAPI.Domain.Entities;

public class Game
{
    #region Properties

    public int Id { get; private set; }
    public int SteamGameId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string GameIconUrl { get; private set; } = string.Empty;

    #endregion

    #region Constructors

    private Game() { }

    public Game(int steamGameId, string title, string gameIconUrl)
    {
        SteamGameId = steamGameId;
        Title = title;
        GameIconUrl = gameIconUrl;
    }

    #endregion

    #region Methods

    public void Update(string title, string gameIconUrl)
    {
        Title = title;
        GameIconUrl = gameIconUrl;
    }

    #endregion
}
