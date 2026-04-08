namespace SteamStorageAPI.Domain.Entities;

public class CurrencyDynamic
{
    #region Properties

    public int Id { get; private set; }
    public int CurrencyId { get; private set; }
    public DateTime DateUpdate { get; private set; }
    public double Price { get; private set; }

    #endregion

    #region Constructors

    private CurrencyDynamic() { }

    public CurrencyDynamic(int currencyId, double price, DateTime dateUpdate)
    {
        CurrencyId = currencyId;
        Price = price;
        DateUpdate = dateUpdate;
    }

    #endregion
}
