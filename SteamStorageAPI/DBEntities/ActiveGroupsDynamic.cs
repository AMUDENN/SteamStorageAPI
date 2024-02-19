namespace SteamStorageAPI.DBEntities;

public class ActiveGroupsDynamic
{
    #region Properties

    public int Id { get; set; }

    public int GroupId { get; set; }

    public DateTime DateUpdate { get; set; }

    public decimal Sum { get; set; }

    public virtual ActiveGroup Group { get; set; } = null!;

    #endregion Properties
}
