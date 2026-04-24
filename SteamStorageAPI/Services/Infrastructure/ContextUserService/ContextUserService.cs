using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.ContextUserService;

public class ContextUserService : IContextUserService
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SteamStorageContext _context;

    #endregion Fields

    #region Constructor

    public ContextUserService(IHttpContextAccessor httpContextAccessor, SteamStorageContext context)
    {
        _httpContextAccessor = httpContextAccessor;
        _context = context;
    }

    #endregion Constructor

    #region Methods

    public async Task<User?> GetContextUserAsync(CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out int userId))
            return null;

        return await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    #endregion Methods
}