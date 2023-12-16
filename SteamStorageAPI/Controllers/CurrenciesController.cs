using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Models.SteamAPIModels.Price;
using SteamStorageAPI.Utilities.Steam;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CurrenciesController : ControllerBase
    {
        #region Fields
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<CurrenciesController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public CurrenciesController(IHttpClientFactory httpClientFactory, ILogger<CurrenciesController> logger, SteamStorageContext context)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record CurrencyResponse(int Id, int SteamCurrencyId, string Title, string Mark, double Price);
        public record GetCurrencyRequest(int Id);
        public record PostCurrencyRequest(int SteamCurrencyId, string Title, string Mark);
        public record RefreshCurrencyRequest(string MarketHashName);
        public record PutCurrencyRequest(int CurrencyId, string Title, string Mark);
        public record DeleteCurrencyRequest(int CurrencyId);
        #endregion Records

        #region Methods
        private CurrencyResponse? GetCurrencyResponse(Currency? currency)
        {
            if (currency is null)
                return null;
            _context.Entry(currency).Collection(s => s.CurrencyDynamics);
            return new CurrencyResponse(currency.Id,
                                        currency.SteamCurrencyId,
                                        currency.Title,
                                        currency.Mark,
                                        currency.CurrencyDynamics.Count == 0 ? 0 : currency.CurrencyDynamics.OrderBy(x => x.DateUpdate).Last().Price);
        }
        #endregion Methods

        #region GET
        [HttpGet(Name = "GetCurrencies")]
        public ActionResult<IEnumerable<CurrencyResponse>> GetCurrencies()
        {
            try
            {
                return Ok(_context.Currencies.ToList().Select(x => GetCurrencyResponse(x)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetCurrency")]
        public ActionResult<CurrencyResponse> GetCurrency([FromQuery]GetCurrencyRequest request)
        {
            try
            {
                Currency? currency = _context.Currencies.ToList().Where(x => x.Id == request.Id).FirstOrDefault();

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
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost(Name = "PostCurrency")]
        public async Task<ActionResult> PostCurrency(PostCurrencyRequest request)
        {
            try
            {
                _context.Currencies.Add(new Currency()
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

        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPost(Name = "RefreshCurrencyExchangeRates")]
        public async Task<ActionResult> RefreshCurrencyExchangeRates(RefreshCurrencyRequest request)
        {
            try
            {
                IEnumerable<Currency> currencies = _context.Currencies.ToList();

                Currency? dollar = _context.Currencies.Where(x => x.SteamCurrencyId == 1).FirstOrDefault();
                if (dollar is null)
                    return NotFound("В базе данных отсутствует базовая валюта (американский доллар)");

                Skin? skin = _context.Skins.Where(x => x.MarketHashName == request.MarketHashName).FirstOrDefault();
                if (skin is null)
                    return NotFound("В базе данных отсутствует скин с таким MarketHashName");

                _context.Entry(skin).Reference(s => s.Game).Load();

                HttpClient client = _httpClientFactory.CreateClient();
                SteamPriceResponse? response = await client.GetFromJsonAsync<SteamPriceResponse>(SteamUrls.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName, dollar.SteamCurrencyId));
                if (response is null)
                    throw new Exception("При получении данных с сервера Steam произошла ошибка");

                double dollarPrice = Convert.ToDouble(response.lowest_price.Replace(dollar.Mark, string.Empty).Replace('.', ','));

                foreach (Currency currency in currencies)
                {
                    response = await client.GetFromJsonAsync<SteamPriceResponse>(SteamUrls.GetPriceOverviewUrl(skin.Game.SteamGameId, skin.MarketHashName, currency.SteamCurrencyId));

                    if (response is null)
                        continue;

                    double price = Convert.ToDouble(response.lowest_price.Replace(currency.Mark, string.Empty).Replace('.', ','));

                    _context.CurrencyDynamics.Add(new CurrencyDynamic()
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
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPut(Name = "PutCurrencyInfo")]
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
        #endregion PUT

        #region DELETE
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpDelete(Name = "DeleteCurrency")]
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
