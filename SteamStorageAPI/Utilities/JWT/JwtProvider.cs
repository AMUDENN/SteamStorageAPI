using Microsoft.IdentityModel.Tokens;
using SteamStorageAPI.DBEntities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SteamStorageAPI.Utilities.JWT
{
    public class JwtProvider : IJwtProvider
    {
        public string Generate(User tokenOwner)
        {

            List<Claim> claims =
            [
                new(ClaimTypes.Name, tokenOwner.SteamId.ToString()),
                new(ClaimTypes.NameIdentifier, tokenOwner.Id.ToString()),
                new(ClaimTypes.Role, tokenOwner.Role.Title.ToString()),
            ];

            JwtSecurityToken jwt = new(
                issuer: JwtOptions.ISSUER,
                audience: JwtOptions.AUDIENCE,
                claims: claims,
                expires: DateTime.UtcNow.Add(TimeSpan.FromDays(1)),
                signingCredentials: new(JwtOptions.GetSymmetricSecurityKey(),
                    SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }
    }
}
