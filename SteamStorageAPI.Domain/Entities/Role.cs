using SteamStorageAPI.Domain.Enums;

namespace SteamStorageAPI.Domain.Entities;

public class Role
{
    #region Properties

    public int Id { get; private set; }
    public string Title { get; private set; } = string.Empty;

    #endregion

    #region Constructors

    private Role() { }

    public Role(string title)
    {
        Title = title;
    }

    #endregion

    #region Methods

    public UserRole ToEnum() =>
        Enum.TryParse<UserRole>(Title, out UserRole role)
            ? role
            : UserRole.User;

    public bool IsAdmin() => ToEnum() == UserRole.Admin;

    #endregion
}
