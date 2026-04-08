namespace SteamStorageAPI.Application.Interfaces.Services;

public interface ICryptographyService
{
    string Sha512(long value);
}
