namespace SteamStorageAPI.Services.CryptographyService;

public interface ICryptographyService
{
    public string Sha512(object input);
}