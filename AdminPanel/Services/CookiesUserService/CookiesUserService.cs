namespace AdminPanel.Services.CookiesUserService;

public class CookiesUserService : ICookiesUserService
{
    #region Constants

    private const string JWT_COOKIES = "ASP_NET_JWT_COOKIES";

    #endregion Constants

    #region Fields

    private readonly IHttpContextAccessor _httpContextAccessor;

    #endregion Fields

    #region Constructor

    public CookiesUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    #endregion Constructor

    #region Methods

    public void SetCookiesToken(string? token, CancellationToken cancellationToken = default)
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Append(JWT_COOKIES, token ?? string.Empty);
    }
    public string? GetCookiesToken(CancellationToken cancellationToken = default)
    {
        string? token = null;
        _httpContextAccessor.HttpContext?.Request.Cookies.TryGetValue(JWT_COOKIES, out token);
        return token;
    }

    #endregion Methods
}