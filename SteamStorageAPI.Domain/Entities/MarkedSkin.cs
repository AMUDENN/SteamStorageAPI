namespace SteamStorageAPI.Domain.Entities;

public class MarkedSkin
{
    #region Properties

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int SkinId { get; private set; }

    #endregion

    #region Constructors

    private MarkedSkin() { }

    public MarkedSkin(int userId, int skinId)
    {
        UserId = userId;
        SkinId = skinId;
    }

    #endregion
}
