namespace SteamStorageAPI.Domain.Entities;

public class Active
{
    #region Properties

    public int Id { get; private set; }
    public int GroupId { get; private set; }
    public int SkinId { get; private set; }
    public string? Description { get; private set; }
    public DateTime BuyDate { get; private set; }
    public int Count { get; private set; }
    public decimal BuyPrice { get; private set; }
    public decimal? GoalPrice { get; private set; }

    #endregion

    #region Constructors

    private Active() { }

    public Active(
        int groupId,
        int skinId,
        int count,
        decimal buyPrice,
        decimal? goalPrice,
        DateTime buyDate,
        string? description)
    {
        GroupId = groupId;
        SkinId = skinId;
        Count = count;
        BuyPrice = buyPrice;
        GoalPrice = goalPrice;
        BuyDate = buyDate;
        Description = description;
    }

    #endregion

    #region Methods

    public void Update(
        int groupId,
        int skinId,
        int count,
        decimal buyPrice,
        decimal? goalPrice,
        DateTime buyDate,
        string? description)
    {
        GroupId = groupId;
        SkinId = skinId;
        Count = count;
        BuyPrice = buyPrice;
        GoalPrice = goalPrice;
        BuyDate = buyDate;
        Description = description;
    }
    
    public double CalculateChange(decimal currentPrice)
    {
        if (BuyPrice == 0) return 0;
        return (double)((currentPrice - BuyPrice) / BuyPrice);
    }
    
    public decimal CalculateCurrentSum(decimal currentPrice) =>
        currentPrice * Count;
    
    public double? CalculateGoalPriceCompletion(decimal currentPrice)
    {
        if (GoalPrice is null or 0) return null;
        return (double)(currentPrice / GoalPrice.Value);
    }

    #endregion
}
