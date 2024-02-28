namespace SteamStorageAPI.DBEntities;

public class Currency
{
    #region Constanst

    public const int BASE_CURRENCY_ID = 1;
    
    #endregion Constants
    
    #region Properties

    public int Id { get; set; }

    public int SteamCurrencyId { get; set; }

    public string Title { get; set; } = null!;

    public string Mark { get; set; } = null!;

    public virtual ICollection<CurrencyDynamic> CurrencyDynamics { get; set; } = new List<CurrencyDynamic>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    #endregion Properties
}
