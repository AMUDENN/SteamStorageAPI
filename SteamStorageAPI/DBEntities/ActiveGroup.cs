namespace SteamStorageAPI.DBEntities;

public partial class ActiveGroup
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Colour { get; set; }

    public decimal? GoalSum { get; set; }

    public virtual ICollection<ActiveGroupsDynamic> ActiveGroupsDynamics { get; set; } = new List<ActiveGroupsDynamic>();

    public virtual ICollection<Active> Actives { get; set; } = new List<Active>();

    public virtual User User { get; set; } = null!;
}
