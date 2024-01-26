using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SteamStorageAPI.Utilities.JWT
{
    public static class JwtOptions
    {
        #region Constants

        private const string KEY = "I6IjIiLCJleHAiOjE3MDIyOTQ3ODUsIm"; // ключ для шифрации
        public const int EXPIRES_DAYS = 1;
        public const string ISSUER = "AMUDENN"; // издатель токена
        public const string AUDIENCE = "SteamStorageUser"; // потребитель токена

        #endregion Constants

        #region Methods

        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new(Encoding.UTF8.GetBytes(KEY));

        #endregion Methods
    }
}
