namespace SteamStorageAPI.Domain.Entities;

public class Currency
{
    #region Properties

    public int Id { get; private set; }
    public int SteamCurrencyId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Mark { get; private set; } = string.Empty;
    public string CultureInfo { get; private set; } = string.Empty;

    #endregion

    #region Constructors

    private Currency() { }

    public Currency(int steamCurrencyId, string title, string mark, string cultureInfo)
    {
        SteamCurrencyId = steamCurrencyId;
        Title = title;
        Mark = mark;
        CultureInfo = cultureInfo;
    }

    #endregion

    #region Methods

    public void Update(string title, string mark, string cultureInfo)
    {
        Title = title;
        Mark = mark;
        CultureInfo = cultureInfo;
    }
    
    public double GetExchangeRate(IEnumerable<CurrencyDynamic> dynamics)
    {
        CurrencyDynamic? latest = dynamics
            .OrderByDescending(d => d.DateUpdate)
            .FirstOrDefault();

        return latest?.Price ?? 1;
    }

    #endregion
}
