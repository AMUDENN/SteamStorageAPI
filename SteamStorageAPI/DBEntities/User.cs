namespace SteamStorageAPI.DBEntities;

public partial class User
{
    public int Id { get; set; }

    public long SteamId { get; set; }

    public int RoleId { get; set; }

    public int StartPageId { get; set; }

    public int CurrencyId { get; set; }

    public DateTime DateRegistration { get; set; }

    public double? GoalSum { get; set; }

    public virtual ICollection<ActiveGroup> ActiveGroups { get; set; } = new List<ActiveGroup>();

    public virtual ICollection<ArchiveGroup> ArchiveGroups { get; set; } = new List<ArchiveGroup>();

    public virtual Currency Currency { get; set; } = null!;

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<MarkedSkin> MarkedSkins { get; set; } = new List<MarkedSkin>();

    public virtual Role Role { get; set; } = null!;

    public virtual Page StartPage { get; set; } = null!;
}
