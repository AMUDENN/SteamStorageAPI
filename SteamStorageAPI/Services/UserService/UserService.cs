using SteamStorageAPI.DBEntities;
using System.Security.Claims;

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

        public User? GetCurrentUser()
        {
            string? nameIdentifier = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return nameIdentifier is null
                ? null
                : _context.Users.FirstOrDefault(x => x.Id == int.Parse(nameIdentifier));
        }

        #endregion Methods
    }
}
