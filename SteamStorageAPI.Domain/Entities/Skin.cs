namespace SteamStorageAPI.Domain.Entities;

public class Skin
{
    #region Properties

    public int Id { get; private set; }
    public int GameId { get; private set; }
    public string MarketHashName { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string SkinIconUrl { get; private set; } = string.Empty;
    public decimal CurrentPrice { get; private set; }

    #endregion

    #region Constructors

    private Skin() { }

    public Skin(int gameId, string marketHashName, string title, string skinIconUrl)
    {
        GameId = gameId;
        MarketHashName = marketHashName;
        Title = title;
        SkinIconUrl = skinIconUrl;
        CurrentPrice = 0;
    }

    #endregion

    #region Methods

    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(newPrice), "Цена не может быть отрицательной.");
        CurrentPrice = newPrice;
    }

    public void UpdateInfo(string title, string skinIconUrl)
    {
        Title = title;
        SkinIconUrl = skinIconUrl;
    }

    #endregion
}
