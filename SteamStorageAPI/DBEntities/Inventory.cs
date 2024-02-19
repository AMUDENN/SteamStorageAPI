namespace SteamStorageAPI.DBEntities;

public class Inventory
{
    #region Properties

    public int Id { get; set; }

    public int UserId { get; set; }

    public int SkinId { get; set; }

    public int Count { get; set; }

    public virtual Skin Skin { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    #endregion Properties
}
