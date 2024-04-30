using SteamStorageAPI.SDK.ApiEntities;

namespace AdminPanel.Models;

public class AdminPanelViewModel
{
    #region Properties

    public string ProfileImageUrl { get; init; } = string.Empty;

    public string Nickname { get; init; } = string.Empty;

    public string SteamId { get; init; } = string.Empty;

    public string Role { get; init; } = string.Empty;

    public List<Currencies.CurrencyResponse> Currencies { get; init; } = [];
    
    public List<Games.GameResponse> Games { get; init; } = [];
    
    public List<Roles.RoleResponse> Roles { get; init; } = [];

    public int UsersPageNumber { get; init; } = 1;

    public List<Users.UserResponse> Users { get; init; } = [];

    #endregion Properties
}
