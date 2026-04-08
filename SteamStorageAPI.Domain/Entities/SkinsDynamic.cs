namespace SteamStorageAPI.Domain.Entities;

public class SkinsDynamic
{
    #region Properties

    public int Id { get; private set; }
    public int SkinId { get; private set; }
    public DateTime DateUpdate { get; private set; }
    public decimal Price { get; private set; }

    #endregion

    #region Constructors

    private SkinsDynamic() { }

    public SkinsDynamic(int skinId, decimal price, DateTime dateUpdate)
    {
        SkinId = skinId;
        Price = price;
        DateUpdate = dateUpdate;
    }

    #endregion
}
