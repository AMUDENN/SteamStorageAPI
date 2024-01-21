using System.Text;
using System.Security.Cryptography;

namespace SteamStorageAPI.Services.CryptographyService;

public class CryptographyService : ICryptographyService
{
    public string Sha512(object input)
    {
        return string.Concat(SHA512.HashData(Encoding.UTF8.GetBytes($"{input}")).Select(x => x.ToString("X2")));
    }
}