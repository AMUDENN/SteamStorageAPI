using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.JWT;

namespace SteamStorageAPI.Services.JwtProvider
{
    public class JwtProvider : IJwtProvider
    {
        public string Generate(User tokenOwner)
        {

            List<Claim> claims =
            [
                new(ClaimTypes.Name, tokenOwner.SteamId.ToString()),
                new(ClaimTypes.NameIdentifier, tokenOwner.Id.ToString()),
                new(ClaimTypes.Role, tokenOwner.Role.Title),
            ];

            JwtSecurityToken jwt = new(
                issuer: JwtOptions.ISSUER,
                audience: JwtOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(JwtOptions.EXPIRES_DAYS)),
                signingCredentials: new(JwtOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
