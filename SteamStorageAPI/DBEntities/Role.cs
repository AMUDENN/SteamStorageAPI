namespace SteamStorageAPI.DBEntities;

public class Role
{
    #region Enums

    public enum Roles
    {
        Admin,
        User
    }

    #endregion Enums

    #region Properties

    public int Id { get; set; }

    public string Title { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    #endregion Properties
}
