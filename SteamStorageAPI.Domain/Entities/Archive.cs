namespace SteamStorageAPI.Domain.Entities;

public class Archive
{
    #region Properties

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int SkinId { get; private set; }
    public string? Description { get; private set; }
    public int Count { get; private set; }
    public DateTime BuyDate { get; private set; }
    public decimal BuyPrice { get; private set; }
    public DateTime SoldDate { get; private set; }
    public decimal SoldPrice { get; private set; }

    #endregion

    #region Constructors

    private Archive() { }

    public Archive(
        int groupId,
        int skinId,
        int count,
        decimal buyPrice,
        decimal soldPrice,
        DateTime buyDate,
        DateTime soldDate,
        string? description)
    {
        GroupId = groupId;
        SkinId = skinId;
        Count = count;
        BuyPrice = buyPrice;
        SoldPrice = soldPrice;
        BuyDate = buyDate;
        SoldDate = soldDate;
        Description = description;
    }

    #endregion

    #region Methods

    public void Update(
        int groupId,
        int skinId,
        int count,
        decimal buyPrice,
        decimal soldPrice,
        DateTime buyDate,
        DateTime soldDate,
        string? description)
    {
        GroupId = groupId;
        SkinId = skinId;
        Count = count;
        BuyPrice = buyPrice;
        SoldPrice = soldPrice;
        BuyDate = buyDate;
        SoldDate = soldDate;
        Description = description;
    }
    
    public double CalculateChange()
    {
        if (BuyPrice == 0) return 0;
        return (double)((SoldPrice - BuyPrice) / BuyPrice);
    }
    
    public decimal CalculateSoldSum() => SoldPrice * Count;
    
    public decimal CalculateBuySum() => BuyPrice * Count;

    #endregion
}
