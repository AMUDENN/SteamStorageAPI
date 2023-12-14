namespace SteamStorageAPI.DBEntities;

public partial class Active
{
    public int Id { get; set; }

    public int GroupId { get; set; }

    public int SkinId { get; set; }

    public string? Description { get; set; }

    public DateTime BuyDate { get; set; }

    public int Count { get; set; }

    public decimal BuyPrice { get; set; }

    public decimal? GoalPrice { get; set; }

    public virtual ActiveGroup Group { get; set; } = null!;

    public virtual Skin Skin { get; set; } = null!;
}
