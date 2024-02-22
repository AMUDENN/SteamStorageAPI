using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Price;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Steam;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CurrenciesController : ControllerBase
    {
        #region Fields

        private readonly ILogger<CurrenciesController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public CurrenciesController(ILogger<CurrenciesController> logger, IHttpClientFactory httpClientFactory,
            IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record CurrencyResponse(
            int Id,
            int SteamCurrencyId,
            string Title,
            string Mark,
            double Price);

        public record GetCurrencyRequest(
            int Id);

        public record PostCurrencyRequest(
            int SteamCurrencyId,
            string Title,
            string Mark);

        public record RefreshCurrencyRequest(
            string MarketHashName);

        public record PutCurrencyRequest(
            int CurrencyId,
            string Title,
            string Mark);

        public record SetCurrencyRequest(
            int CurrencyId);

        public record DeleteCurrencyRequest(
            int CurrencyId);

        #endregion Records

        #region Methods

        private CurrencyResponse? GetCurrencyResponse(Currency? currency)
        {
            if (currency is null)
                return null;

            List<CurrencyDynamic> currencyDynamics =
                _context.Entry(currency).Collection(s => s.CurrencyDynamics).Query().ToList();

            return new(currency.Id,
                currency.SteamCurrencyId,
                currency.Title,
                currency.Mark,
                currencyDynamics.Count == 0 ? 0 : currencyDynamics.OrderBy(x => x.DateUpdate).Last().Price);
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка валют
        /// </summary>
        /// <response code="200">Возвращает список валют</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        [HttpGet(Name = "GetCurrencies")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<CurrencyResponse>> GetCurrencies()
        {
            try
            {
                return Ok(_context.Currencies.Select(GetCurrencyResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение информации о валюте
        /// </summary>
        /// <response code="200">Возвращает информацию о валюте</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует</response>
        [HttpGet(Name = "GetCurrency")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<CurrencyResponse> GetCurrency([FromQuery] GetCurrencyRequest request)
        {
            try
            {
                Currency? currency = _context.Currencies.FirstOrDefault(x => x.Id == request.Id);

                if (currency is null)
                    return NotFound("Валюты с таким Id не существует");

                return Ok(GetCurrencyResponse(currency));
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
        /// Добавление новой валюты
        /// </summary>
        /// <response code="200">Валюта успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        [HttpPost(Name = "PostCurrency")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PostCurrency(PostCurrencyRequest request)
        {
            try
            {
                
                //TODO: Проверка на существование валюты с таким Id в Steam
                
                _context.Currencies.Add(new()
                {
                    SteamCurrencyId = request.SteamCurrencyId,
                    Title = request.Title,
                    Mark = request.Mark
                });

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Обновление курса валют
        /// </summary>
        /// <response code="200">Курс валют успешно обновлён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Базовая валюта (американский доллар) не найдена или предмета с таким MarketHashName не существует</response>
        [HttpPost(Name = "RefreshCurrenciesExchangeRates")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> RefreshCurrenciesExchangeRates(RefreshCurrencyRequest request)
        {
            try
            {
                IEnumerable<Currency> currencies = _context.Currencies.ToList();

                Currency? dollar = _context.Currencies.FirstOrDefault(x => x.SteamCurrencyId == 1);
                if (dollar is null)
                    return NotFound("В базе данных отсутствует базовая валюта (американский доллар)");

                Skin? skin = _context.Skins.FirstOrDefault(x => x.MarketHashName == request.MarketHashName);
                if (skin is null)
                    return NotFound("В базе данных отсутствует скин с таким MarketHashName");

                await _context.Entry(skin).Reference(s => s.Game).LoadAsync();

                HttpClient client = _httpClientFactory.CreateClient();
                SteamPriceResponse? response = await client.GetFromJsonAsync<SteamPriceResponse>(
                    SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName, dollar.SteamCurrencyId));
                if (response?.lowest_price is null)
                    throw new("При получении данных с сервера Steam произошла ошибка");

                double dollarPrice =
                    Convert.ToDouble(response.lowest_price.Replace(dollar.Mark, string.Empty).Replace('.', ','));

                foreach (Currency currency in currencies)
                {
                    response = await client.GetFromJsonAsync<SteamPriceResponse>(
                        SteamApi.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName,
                            currency.SteamCurrencyId));

                    if (response is null)
                        continue;

                    double price = Convert.ToDouble(response.lowest_price.Replace(currency.Mark, string.Empty)
                        .Replace('.', ','));

                    _context.CurrencyDynamics.Add(new()
                    {
                        CurrencyId = currency.Id,
                        DateUpdate = DateTime.Now,
                        Price = price / dollarPrice
                    });

                    await Task.Delay(2000);
                }

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение валюты
        /// </summary>
        /// <response code="200">Валюта успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует</response>
        [HttpPut(Name = "PutCurrencyInfo")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PutCurrencyInfo(PutCurrencyRequest request)
        {
            try
            {
                Currency? currency = _context.Currencies.FirstOrDefault(x => x.Id == request.CurrencyId);

                if (currency is null)
                    return NotFound("Валюты с таким Id не существует");

                currency.Title = request.Title;

                currency.Mark = request.Mark;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Установка валюты пользователя
        /// </summary>
        /// <response code="200">Валюта успешно установлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "SetCurrency")]
        public async Task<ActionResult> SetCurrency(SetCurrencyRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                if (!_context.Currencies.Any(x => x.Id == request.CurrencyId))
                    return NotFound("Валюты с таким Id не существует");

                user.CurrencyId = request.CurrencyId;

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление валюты
        /// </summary>
        /// <response code="200">Валюта успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует</response>
        [HttpDelete(Name = "DeleteCurrency")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> DeleteCurrency(DeleteCurrencyRequest request)
        {
            try
            {
                Currency? currency = _context.Currencies.FirstOrDefault(x => x.Id == request.CurrencyId);

                if (currency is null)
                    return NotFound("Валюты с таким Id не существует");

                _context.Currencies.Remove(currency);

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception ex)
            {
                _context.UndoChanges();
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion DELETE
    }
}
