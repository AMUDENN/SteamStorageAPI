using System.Text;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Utilities.JWT;

public class JwtOptions
{
    #region Properties

    private string Key { get; }
    public string Issuer { get; }
    public string Audience { get; }
    public int ExpiresDays { get; }

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

    public SymmetricSecurityKey GetSymmetricSecurityKey()
    {
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key));
    }

    #endregion Methods
}