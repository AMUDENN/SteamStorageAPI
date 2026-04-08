namespace SteamStorageAPI.Domain.Entities;

public class Page
{
    #region Properties

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;

    #endregion

    #region Constructors

    private Page() { }

    public Page(string title)
    {
        Title = title;
    }

    #endregion
}
