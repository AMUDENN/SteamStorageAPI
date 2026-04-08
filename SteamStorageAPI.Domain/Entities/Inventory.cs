namespace SteamStorageAPI.Domain.Entities;

public class Inventory
{
    #region Properties

    public int Id { get; private set; }
    public int UserId { get; private set; }
    public int SkinId { get; private set; }
    public int Count { get; private set; }

    #endregion

    #region Constructors

    private Inventory() { }

    public Inventory(int userId, int skinId, int count)
    {
        UserId = userId;
        SkinId = skinId;
        Count = count;
    }

    #endregion

    #region Methods

    public void UpdateCount(int count) => Count = count;

    public decimal CalculateCurrentSum(decimal currentPrice) => currentPrice * Count;

    #endregion
}
