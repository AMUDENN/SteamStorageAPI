using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Utilities.JWT;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class AuthorizeController : ControllerBase
    {
        #region Fields
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtProvider _jwtProvider;
        private readonly ILogger<AuthorizeController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public AuthorizeController(IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor, IJwtProvider jwtProvider, ILogger<AuthorizeController> logger, SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _jwtProvider = jwtProvider;
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record AuthUrlResponse(string Url);
        public record AuthResponse(string Token);
        public record SteamAuthRequest(
            [FromQuery(Name = "openid.ns")] string Ns,
            [FromQuery(Name = "openid.mode")] string Mode,
            [FromQuery(Name = "openid.op_endpoint")] string OpEndpoint,
            [FromQuery(Name = "openid.claimed_id")] string ClaimedId,
            [FromQuery(Name = "openid.identity")] string Identity,
            [FromQuery(Name = "openid.return_to")] string ReturnTo,
            [FromQuery(Name = "openid.response_nonce")] string ResponseNonce,
            [FromQuery(Name = "openid.assoc_handle")] string AssocHandle,
            [FromQuery(Name = "openid.signed")] string Signed,
            [FromQuery(Name = "openid.sig")] string Sig);
        #endregion Records

        #region Methods
        private User? FindUser(long steamID)
        {
            return _context.Users.FirstOrDefault(x => x.SteamId == steamID);
        }

        private async Task<User> CreateUser(long steamID, int currencyID = 1)
        {
            Role role = _context.Roles.Where(x => x.Title == nameof(Roles.User)).First();
            User user = new()
            {
                SteamId = steamID,
                CurrencyId = currencyID,
                DateRegistration = DateTime.Now,
                RoleId = role.Id
            };
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        private bool CheckCookieEqual(SteamAuthRequest steamAuthRequest)
        {
            SteamAuthRequest? steamAuthRequestCookie = null;
            if (HttpContext.Request.Cookies.TryGetValue(nameof(SteamAuthRequest), out var cookie))
                steamAuthRequestCookie = JsonConvert.DeserializeObject<SteamAuthRequest>(cookie);

            return steamAuthRequestCookie is not null && steamAuthRequest == steamAuthRequestCookie;
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetAuthUrl")]
        public ActionResult<AuthUrlResponse> GetAuthUrl()
        {
            try
            {
                string baseUrl = $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/";
                return Ok(new AuthUrlResponse(SteamUrls.GetAuthUrl($"{baseUrl}api/Authorize/SteamAuthCallback", baseUrl)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "SteamAuthCallback")]
        public async Task<ActionResult<AuthResponse>> SteamAuthCallback([FromQuery] SteamAuthRequest steamAuthRequest)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient();
                string response = await client.GetStringAsync(
                    SteamUrls.GetAuthCkeckUrl(steamAuthRequest.OpEndpoint,
                                              steamAuthRequest.ClaimedId,
                                              steamAuthRequest.Identity,
                                              steamAuthRequest.ReturnTo,
                                              steamAuthRequest.ResponseNonce,
                                              steamAuthRequest.AssocHandle,
                                              steamAuthRequest.Signed,
                                              steamAuthRequest.Sig));
                bool authResult = Convert.ToBoolean(response[(response.LastIndexOf(':') + 1)..]);

                if (!(authResult || CheckCookieEqual(steamAuthRequest)))
                    return BadRequest("Авторизация не удалась");


                long steamID = Convert.ToInt64(steamAuthRequest.ClaimedId[(steamAuthRequest.ClaimedId.LastIndexOf('/') + 1)..]);

                User user = FindUser(steamID) ?? await CreateUser(steamID);

                HttpContext.Response.Cookies.Append(nameof(SteamAuthRequest), JsonConvert.SerializeObject(steamAuthRequest));

                _context.Entry(user).Reference(u => u.Role).Load();

                return Ok(new AuthResponse(_jwtProvider.Generate(user)));
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }
        #endregion GET
    }
}
