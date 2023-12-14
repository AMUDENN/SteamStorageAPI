using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.DBEntities;
using static SteamStorageAPI.Utilities.ProgramConstants;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CurrenciesController : ControllerBase
    {
        #region Fields
        private readonly ILogger<CurrenciesController> _logger;
        private readonly SteamStorageContext _context;
        #endregion Fields

        #region Constructor
        public CurrenciesController(ILogger<CurrenciesController> logger, SteamStorageContext context)
        {
            _logger = logger;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record CurrencyResponse(int Id, int SteamCurrencyId, string Title, string Mark);
        public record CurrencyRequest(int SteamCurrencyId, string Title, string Mark);
        public record EditCurrencyRequest(int CurrencyId, string Title, string Mark);
        public record DeleteCurrencyRequest(int CurrencyId);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetCurrencies")]
        public ActionResult<IEnumerable<CurrencyResponse>> GetCurrencies()
        {
            try
            {
                return Ok(_context.Currencies.ToList().Select(x =>
                    new CurrencyResponse(x.Id, x.SteamCurrencyId, x.Title, x.Mark)));
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
        public async Task<ActionResult> PostCurrency(CurrencyRequest request)
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
        #endregion POST

        #region PUT
        [Authorize(Roles = nameof(Roles.Admin))]
        [HttpPut(Name = "PutCurrencyInfo")]
        public async Task<ActionResult> PutCurrencyInfo(EditCurrencyRequest request)
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
