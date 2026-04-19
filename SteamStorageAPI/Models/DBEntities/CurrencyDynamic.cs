namespace SteamStorageAPI.Models.DBEntities;

public class CurrencyDynamic
{
    #region Properties

    public int Id { get; set; }

    public int CurrencyId { get; set; }

    public DateTime DateUpdate { get; set; }

    public decimal Price { get; set; }

    public virtual Currency Currency { get; set; } = null!;

    #endregion Properties
}