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
            return nameIdentifier is null ? null : FindUser(int.Parse(nameIdentifier));
        }
        public User? FindUser(int Id)
        {
            return _context.Users.FirstOrDefault(x => x.Id == Id);
        }
        public User? FindUser(long steamId)
        {
            return _context.Users.FirstOrDefault(x => x.SteamId == steamId);
        }
        #endregion Methods
    }
}
