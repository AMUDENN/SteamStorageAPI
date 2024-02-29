namespace SteamStorageAPI.DBEntities;

public class Page
{
    #region Constanst

    public const int BASE_START_PAGE_ID = 1;
    
    #endregion Constants
    
    #region Properties

    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    #endregion Properties
}
