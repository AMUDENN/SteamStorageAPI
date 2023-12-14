namespace SteamStorageAPI.DBEntities;

public partial class Currency
{
    public int Id { get; set; }

    public int SteamCurrencyId { get; set; }

    public string Title { get; set; } = null!;

    public string Mark { get; set; } = null!;

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
