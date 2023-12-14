using System.Security.Claims;

namespace SteamStorageAPI.Utilities
{
    public static class UserContext
    {
        public static int? GetUserId(HttpContext httpContext)
        {
            string? nameIdentifier = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return nameIdentifier is null ? null : int.Parse(nameIdentifier);
        }
    }
}
