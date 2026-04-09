using System.Text;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Utilities.JWT;

public class JwtOptions
{
    #region Properties

    public string Key { get; init; }
    public string Issuer { get; init; }
    public string Audience { get; init; }
    public int ExpiresDays { get; init; }

    #endregion Properties

    #region Constructor

    public JwtOptions(AppConfig config)
    {
        Key = config.Jwt.Key;
        Issuer = config.Jwt.Issuer;
        Audience = config.Jwt.Audience;
        ExpiresDays = config.Jwt.ExpiresDays;
    }

    #endregion Constructor

    #region Methods

    public SymmetricSecurityKey GetSymmetricSecurityKey() =>
        new(Encoding.UTF8.GetBytes(Key));

    #endregion Methods
}