using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CryptographyService;
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

        private readonly ILogger<AuthorizeController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IJwtProvider _jwtProvider;
        private readonly ICryptographyService _cryptographyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public AuthorizeController(ILogger<AuthorizeController> logger, IHttpClientFactory httpClientFactory,
            IHttpContextAccessor httpContextAccessor, IJwtProvider jwtProvider,
            ICryptographyService cryptographyService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
            _jwtProvider = jwtProvider;
            _cryptographyService = cryptographyService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record AuthUrlResponse(string Url);

        public record SteamAuthResponse(string Token);
        
        public record CookieAuthResponse(string Token);

        public record SteamAuthRequest(
            [FromQuery(Name = "openid.ns")] string Ns,
            [FromQuery(Name = "openid.mode")] string Mode,
            [FromQuery(Name = "openid.op_endpoint")]
            string OpEndpoint,
            [FromQuery(Name = "openid.claimed_id")]
            string ClaimedId,
            [FromQuery(Name = "openid.identity")] string Identity,
            [FromQuery(Name = "openid.return_to")] string ReturnTo,
            [FromQuery(Name = "openid.response_nonce")]
            string ResponseNonce,
            [FromQuery(Name = "openid.assoc_handle")]
            string AssocHandle,
            [FromQuery(Name = "openid.signed")] string Signed,
            [FromQuery(Name = "openid.sig")] string Sig);

        public record CheckCookieAuthRequest(long SteamId);

        #endregion Records

        #region Methods

        private async Task<User> CreateUser(long steamId, int currencyId = 1, int startPageId = 1)
        {
            Role role = _context.Roles.First(x => x.Title == nameof(Roles.User));
            User user = new()
            {
                SteamId = steamId,
                RoleId = role.Id,
                StartPageId = startPageId,
                CurrencyId = currencyId,
                DateRegistration = DateTime.Now
            };
            _context.Users.Add(user);

            await _context.SaveChangesAsync();

            return user;
        }

        private string GetSteamAuthUrl()
        {
            string baseUrl =
                $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/";
            return SteamApi.GetAuthUrl($"{baseUrl}api/Authorize/SteamAuthCallback", baseUrl);
        }

        private bool CheckCookieEqual(long steamId)
        {
            HttpContext.Request.Cookies.TryGetValue(nameof(SteamAuthRequest), out string? cookie);

            return _cryptographyService.Sha512(steamId) == cookie;
        }

        #endregion Methods

        #region GET

        [HttpGet(Name = "GetAuthUrl")]
        public ActionResult<AuthUrlResponse> GetAuthUrl()
        {
            try
            {
                return Ok(new AuthUrlResponse(GetSteamAuthUrl()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "SteamAuthCallback")]
        public async Task<ActionResult<SteamAuthResponse>> SteamAuthCallback(
            [FromQuery] SteamAuthRequest steamAuthRequest)
        {
            try
            {
                HttpClient client = _httpClientFactory.CreateClient();

                HttpRequestMessage request = new(HttpMethod.Post, SteamApi.GetAuthCheckUrl())
                {
                    Content = SteamApi.GetAuthCheckContent(steamAuthRequest.Ns, steamAuthRequest.OpEndpoint,
                        steamAuthRequest.ClaimedId, steamAuthRequest.Identity, steamAuthRequest.ReturnTo,
                        steamAuthRequest.ResponseNonce, steamAuthRequest.AssocHandle, steamAuthRequest.Signed,
                        steamAuthRequest.Sig)
                };
                
                HttpResponseMessage response =
                    await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                string strResponse = await response.Content.ReadAsStringAsync();
                bool authResult = Convert.ToBoolean(strResponse[(strResponse.LastIndexOf(':') + 1)..]);

                long steamId =
                    Convert.ToInt64(steamAuthRequest.ClaimedId[(steamAuthRequest.ClaimedId.LastIndexOf('/') + 1)..]);

                if (!(authResult || CheckCookieEqual(steamId)))
                    return Redirect(GetSteamAuthUrl());

                if (authResult)
                    HttpContext.Response.Cookies.Append(nameof(SteamAuthRequest), _cryptographyService.Sha512(steamId));

                User user = _context.Users.FirstOrDefault(x => x.SteamId == steamId) ?? await CreateUser(steamId);

                await _context.Entry(user).Reference(u => u.Role).LoadAsync();

                return Ok(new SteamAuthResponse(_jwtProvider.Generate(user)));
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "CheckCookieAuth")]
        public async Task<ActionResult<CookieAuthResponse>> CheckCookieAuth([FromQuery] CheckCookieAuthRequest request)
        {
            try
            {
                if (!CheckCookieEqual(request.SteamId))
                    return BadRequest("Необходима новая авторизация через Steam");
                
                User? user = _context.Users.FirstOrDefault(x => x.SteamId == request.SteamId);

                if (user is null)
                    return BadRequest("Пользователя с таким Id не существует, пройдите авторизацию через Steam");

                await _context.Entry(user).Reference(u => u.Role).LoadAsync();

                return Ok(new CookieAuthResponse(_jwtProvider.Generate(user)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "LogOut")]
        public ActionResult LogOut()
        {
            try
            {
                HttpContext.Response.Cookies.Delete(nameof(SteamAuthRequest));
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET
    }
}
