﻿using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SteamStorageAPI.Utilities.JWT
{
    public class JwtOptions
    {
        public const string ISSUER = "AMUDENN"; // издатель токена
        public const string AUDIENCE = "SteamStorageUser"; // потребитель токена
        public const string KEY = "I6IjIiLCJleHAiOjE3MDIyOTQ3ODUsIm";   // ключ для шифрации
        public static SymmetricSecurityKey GetSymmetricSecurityKey() =>
            new (Encoding.UTF8.GetBytes(KEY));
    }
}
