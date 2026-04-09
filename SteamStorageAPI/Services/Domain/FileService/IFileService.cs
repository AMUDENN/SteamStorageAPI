using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Domain.FileService;

public interface IFileService
{
    Task<byte[]> GetExcelFileAsync(User user, CancellationToken cancellationToken = default);
}