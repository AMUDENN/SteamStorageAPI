using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Utilities.JWT;

namespace SteamStorageAPI.Services.Infrastructure.JwtProvider;

public class JwtProvider : IJwtProvider
{
    #region Fields

    private readonly JwtOptions _jwtOptions;

    #endregion Fields

    #region Constructor

    public JwtProvider(JwtOptions jwtOptions)
    {
        _jwtOptions = jwtOptions;
    }

    #endregion Constructor

    #region Methods

    public string Generate(User tokenOwner)
    {
        List<Claim> claims =
        [
            new(ClaimTypes.Name, tokenOwner.SteamId.ToString()),
            new(ClaimTypes.NameIdentifier, tokenOwner.Id.ToString()),
            new(ClaimTypes.Role, tokenOwner.Role.Title)
        ];

        JwtSecurityToken jwt = new(
            _jwtOptions.Issuer,
            _jwtOptions.Audience,
            claims,
            expires: DateTime.UtcNow.Add(TimeSpan.FromDays(_jwtOptions.ExpiresDays)),
            signingCredentials: new SigningCredentials(_jwtOptions.GetSymmetricSecurityKey(),
                SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    #endregion Methods
}