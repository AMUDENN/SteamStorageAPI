using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.Models.DBEntities;

namespace SteamStorageAPI.Services.Infrastructure.ContextUserService;

public class ContextUserService : IContextUserService
{
    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SteamStorageContext _context;

    private User? _cachedUser;
    private bool _loaded;

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
        if (_loaded)
            return _cachedUser;

        string? nameIdentifier = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        _cachedUser = nameIdentifier is null || !int.TryParse(nameIdentifier, out int userId)
            ? null
            : await _context.Users.FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
        _loaded = true;

        return _cachedUser;
    }

    #endregion Methods
}