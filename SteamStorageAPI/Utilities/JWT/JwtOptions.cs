using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SteamStorageAPI.Utilities.JWT
{
    public static class JwtOptions
    {
        #region Constants

        public const int EXPIRES_DAYS = 1;

        #endregion Constants

        #region Properties

        private static string Key { get; set; } = string.Empty;

        public static string Issuer { get; private set; } = string.Empty;

        public static string Audience { get; private set; } = string.Empty;

        #endregion Properties

        #region Methods

        public static void InitializeConfig(IConfiguration configuration)
        {
            IConfigurationSection jwtOptionsSection = configuration.GetSection(nameof(JwtOptions));

            const string jwtOptions = nameof(JwtOptions);

            Key = jwtOptionsSection.GetValue<string>(nameof(Key)) ??
                    throw new ArgumentNullException($"{jwtOptions} {nameof(Key)}");
            Issuer = jwtOptionsSection.GetValue<string>(nameof(Issuer)) ??
                    throw new ArgumentNullException($"{jwtOptions} {nameof(Issuer)}");
            Audience = jwtOptionsSection.GetValue<string>(nameof(Audience)) ??
                    throw new ArgumentNullException($"{jwtOptions} {nameof(Audience)}");
        }
        
        public static void InitializeEnvironmentVariables()
        {
            Key = Environment.GetEnvironmentVariable("JwtOptionsKey") ??
                  throw new ArgumentNullException($"{nameof(JwtOptions)} {nameof(Key)}");
            Issuer = Environment.GetEnvironmentVariable("JwtOptionsIssuer") ??
                     throw new ArgumentNullException($"{nameof(JwtOptions)} {nameof(Issuer)}");
            Audience = Environment.GetEnvironmentVariable("JwtOptionsAudience") ??
                       throw new ArgumentNullException($"{nameof(JwtOptions)} {nameof(Audience)}");
        }

        public static SymmetricSecurityKey GetSymmetricSecurityKey()
        {
            return new(Encoding.UTF8.GetBytes(Key));
        }

        #endregion Methods
    }
}
