﻿using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CryptographyService;
using SteamStorageAPI.Services.JwtProvider;
using SteamStorageAPI.Utilities.Extensions;
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

        public record AuthUrlResponse(
            string Url,
            string Group);

        public record CookieAuthResponse(
            string Token);

        public record SteamAuthRequest(
            [FromQuery(Name = "group")] string Group,
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

        public record CheckCookieAuthRequest(
            long SteamId);

        #endregion Records

        #region Methods

        private async Task<User> CreateUser(long steamId, int currencyId = 1, int startPageId = 1)
        {
            Role role = _context.Roles.First(x => x.Title == nameof(Role.Roles.User));
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

        private (string Url, string Group) GetSteamAuthInfo(string? group = null)
        {
            Random rnd = new();
            group ??= rnd.GenerateString(20);
            string baseUrl =
                $"{_httpContextAccessor.HttpContext?.Request.Scheme}://{_httpContextAccessor.HttpContext?.Request.Host}/";
            return (SteamApi.GetAuthUrl($"{baseUrl}api/Authorize/SteamAuthCallback?group={group}", baseUrl), group);
        }

        private bool CheckCookieEqual(long steamId)
        {
            HttpContext.Request.Cookies.TryGetValue(nameof(SteamAuthRequest), out string? cookie);

            return _cryptographyService.Sha512(steamId) == cookie;
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение ссылки на авторизацию и названия группы SignalR
        /// </summary>
        /// <response code="200">Возвращает ссылку на авторизацию и название группы SignalR</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        [HttpGet(Name = "GetAuthUrl")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<AuthUrlResponse> GetAuthUrl()
        {
            try
            {
                (string Url, string Group) steamAuth = GetSteamAuthInfo();
                return Ok(new AuthUrlResponse(steamAuth.Url, steamAuth.Group));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Callback авторизации в Steam
        /// </summary>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        [HttpGet(Name = "SteamAuthCallback")]
        public async Task<ActionResult> SteamAuthCallback(
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
                    return Redirect(GetSteamAuthInfo(steamAuthRequest.Group).Url);

                if (authResult)
                    HttpContext.Response.Cookies.Append(nameof(SteamAuthRequest), _cryptographyService.Sha512(steamId));

                User user = _context.Users.FirstOrDefault(x => x.SteamId == steamId) ?? await CreateUser(steamId);

                await _context.Entry(user).Reference(u => u.Role).LoadAsync();

                return Redirect(
                    $"{TOKEN_ADRESS}Token/SetToken?Group={steamAuthRequest.Group}&Token={_jwtProvider.Generate(user)}");
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Проверка сохранённых cookie авторизации (только для отладки!)
        /// </summary>
        /// <response code="200">Возвращает новый JWT</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "CheckCookieAuth")]
        [Produces(MediaTypeNames.Application.Json)]
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
        
        #endregion GET
        
        #region POST
        
        /// <summary>
        /// Удаление сохранённых cookie авторизации (только для отладки!)
        /// </summary>
        /// <response code="200">Удаление успешно</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        [HttpPost(Name = "LogOut")]
        public async Task<ActionResult> LogOut()
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
        
        #endregion POST
    }
}
