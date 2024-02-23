using SteamStorageAPI.DBEntities;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace SteamStorageAPI.Services.UserService
{
    public class UserService : IUserService
    {
        #region Fields

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public UserService(IHttpContextAccessor httpContextAccessor, SteamStorageContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        #endregion Constructor

        #region Methods

        public async Task<User?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
        {
            string? nameIdentifier = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return nameIdentifier is null
                ? null
                : await _context.Users.FirstOrDefaultAsync(x => x.Id == int.Parse(nameIdentifier), cancellationToken);
        }

        #endregion Methods
    }
}
