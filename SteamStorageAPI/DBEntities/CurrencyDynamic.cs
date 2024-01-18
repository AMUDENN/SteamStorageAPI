namespace SteamStorageAPI.DBEntities;

public class CurrencyDynamic
{
    public int Id { get; set; }

    public int CurrencyId { get; set; }

    public DateTime DateUpdate { get; set; }

    public double Price { get; set; }

    public virtual Currency Currency { get; set; } = null!;
}
