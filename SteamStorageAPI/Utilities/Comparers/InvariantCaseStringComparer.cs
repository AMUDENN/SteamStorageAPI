namespace SteamStorageAPI.Utilities.Comparers;

public class InvariantCaseStringComparer : IEqualityComparer<string>
{
    #region Methods

    public bool Equals(string? x, string? y)
    {
        if (x is null || y is null) return false;
        return string.Equals(x, y, StringComparison.CurrentCultureIgnoreCase);
    }

    public int GetHashCode(string obj)
    {
        return obj.ToLower().GetHashCode();
    }

    #endregion Methods
}
