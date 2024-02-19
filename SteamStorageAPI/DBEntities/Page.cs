namespace SteamStorageAPI.DBEntities;

public class Page
{
    #region Properties

    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    #endregion Properties
}
