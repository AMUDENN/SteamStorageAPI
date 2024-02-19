namespace SteamStorageAPI.DBEntities;

public class ArchiveGroup
{
    #region Properties

    public int Id { get; set; }

    public int UserId { get; set; }

    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? Colour { get; set; }

    public virtual ICollection<Archive> Archives { get; set; } = new List<Archive>();

    public virtual User User { get; set; } = null!;

    #endregion Properties
}
