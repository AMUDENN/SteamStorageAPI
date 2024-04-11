using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Currencies;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class CurrenciesController : ControllerBase
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public CurrenciesController(
            IUserService userService,
            SteamStorageContext context)
        {
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
            double Price,
            DateTime DateUpdate);
        
        public record CurrenciesResponse(
            int Count,
            IEnumerable<CurrencyResponse> Currencies);

        [Validator<GetCurrencyRequestValidator>]
        public record GetCurrencyRequest(
            int Id);

        [Validator<PostCurrencyRequestValidator>]
        public record PostCurrencyRequest(
            int SteamCurrencyId,
            string Title,
            string Mark);

        [Validator<PutCurrencyRequestValidator>]
        public record PutCurrencyRequest(
            int CurrencyId,
            string Title,
            string Mark);

        [Validator<SetCurrencyRequestValidator>]
        public record SetCurrencyRequest(
            int CurrencyId);

        [Validator<DeleteCurrencyRequestValidator>]
        public record DeleteCurrencyRequest(
            int CurrencyId);

        #endregion Records

        #region Methods

        private async Task<CurrencyResponse> GetCurrencyResponseAsync(
            Currency currency,
            CancellationToken cancellationToken = default)
        {
            IQueryable<CurrencyDynamic> currencyDynamics = _context
                .Entry(currency)
                .Collection(s => s.CurrencyDynamics)
                .Query()
                .AsNoTracking()
                .OrderBy(x => x.DateUpdate);

            CurrencyDynamic? lastDynamic = await currencyDynamics.LastOrDefaultAsync(cancellationToken);

            return new(currency.Id,
                currency.SteamCurrencyId,
                currency.Title,
                currency.Mark,
                lastDynamic?.Price ?? 0,
                lastDynamic?.DateUpdate ?? DateTime.Now);
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка валют
        /// </summary>
        /// <response code="200">Возвращает список валют</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetCurrencies")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<CurrenciesResponse>> GetCurrencies(
            CancellationToken cancellationToken = default)
        {
            IQueryable<CurrencyResponse> currencies = _context.Currencies
                .AsNoTracking()
                .Include(x => x.CurrencyDynamics)
                .Select(x => new CurrencyResponse(x.Id,
                    x.SteamCurrencyId,
                    x.Title,
                    x.Mark,
                    x.CurrencyDynamics.Count != 0
                        ? x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).First().Price
                        : 0,
                    x.CurrencyDynamics.Count != 0
                        ? x.CurrencyDynamics.OrderByDescending(y => y.DateUpdate).First().DateUpdate
                        : DateTime.Now));

            return Ok(new CurrenciesResponse(await currencies.CountAsync(cancellationToken), currencies));
        }

        /// <summary>
        /// Получение информации о валюте
        /// </summary>
        /// <response code="200">Возвращает информацию о валюте</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetCurrency")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<CurrencyResponse>> GetCurrency(
            [FromQuery] GetCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            Currency currency =
                await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ??
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Валюты с таким Id не существует");

            return Ok(await GetCurrencyResponseAsync(currency, cancellationToken));
        }
        
        /// <summary>
        /// Получение информации о текущей валюте пользователя
        /// </summary>
        /// <response code="200">Возвращает информацию о текущей валюте пользователя</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetCurrentCurrency")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<CurrencyResponse>> GetCurrentCurrency(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");
            
            Currency currency =
                await _context.Currencies.AsNoTracking().FirstOrDefaultAsync(x => x.Id == user.CurrencyId, cancellationToken) ??
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Валюты с таким Id не существует");

            return Ok(await GetCurrencyResponseAsync(currency, cancellationToken));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление новой валюты
        /// </summary>
        /// <response code="200">Валюта успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostCurrency")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PostCurrency(
            PostCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            //TODO: Проверка на существование валюты с таким Id в Steam

            await _context.Currencies.AddAsync(new()
            {
                SteamCurrencyId = request.SteamCurrencyId,
                Title = request.Title,
                Mark = request.Mark
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
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
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutCurrencyInfo")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> PutCurrencyInfo(
            PutCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            Currency currency =
                await _context.Currencies.FirstOrDefaultAsync(x => x.Id == request.CurrencyId, cancellationToken) ??
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Валюты с таким Id не существует");

            currency.Title = request.Title;
            currency.Mark = request.Mark;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        /// <summary>
        /// Установка валюты пользователя
        /// </summary>
        /// <response code="200">Валюта успешно установлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Валюты с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "SetCurrency")]
        public async Task<ActionResult> SetCurrency(
            SetCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            if (!await _context.Currencies.AnyAsync(x => x.Id == request.CurrencyId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Валюты с таким Id не существует");
            
            //TODO: Менять и стоимости активов и архивов при смене валюты

            user.CurrencyId = request.CurrencyId;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
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
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteCurrency")]
        [Authorize(Roles = nameof(Role.Roles.Admin))]
        public async Task<ActionResult> DeleteCurrency(
            DeleteCurrencyRequest request,
            CancellationToken cancellationToken = default)
        {
            Currency currency =
                await _context.Currencies.FirstOrDefaultAsync(x => x.Id == request.CurrencyId, cancellationToken) ??
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Валюты с таким Id не существует");

            _context.Currencies.Remove(currency);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
