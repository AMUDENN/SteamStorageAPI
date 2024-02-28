namespace SteamStorageAPI.DBEntities;

public class ActiveGroup
{
    #region Constants

    public const string BASE_ACTIVE_GROUP_COLOUR = "000000";

    #endregion Constants
    
    #region Properties

    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Colour { get; set; }

    public decimal? GoalSum { get; set; }

    public virtual ICollection<ActiveGroupsDynamic> ActiveGroupsDynamics { get; set; } =
        new List<ActiveGroupsDynamic>();

    public virtual ICollection<Active> Actives { get; set; } = new List<Active>();

    public virtual User User { get; set; } = null!;

    #endregion Properties
}
