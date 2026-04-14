using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Models.DTOs;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class CurrenciesController : ControllerBase
{
    #region Fields

    private readonly ICurrencyService _currencyService;
    private readonly IContextUserService _contextUserService;

    #endregion Fields

    #region Constructor

    public CurrenciesController(ICurrencyService currencyService, IContextUserService contextUserService)
    {
        _currencyService = currencyService;
        _contextUserService = contextUserService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение списка валют
    /// </summary>
    /// <response code="200">Возвращает список валют</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrencies")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<CurrenciesResponse>> GetCurrencies(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _currencyService.GetCurrenciesAsync(cancellationToken));
    }

    /// <summary>
    /// Получение информации о валюте
    /// </summary>
    /// <response code="200">Возвращает информацию о валюте</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Валюты с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrency")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<CurrencyResponse>> GetCurrency(
        [FromQuery] GetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        return Ok(await _currencyService.GetCurrencyAsync(request, cancellationToken));
    }

    /// <summary>
    /// Получение текущей валюты пользователя
    /// </summary>
    /// <response code="200">Возвращает текущую валюту пользователя</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Валюты с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentCurrency")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<CurrencyResponse>> GetCurrentCurrency(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        return Ok(await _currencyService.GetCurrentCurrencyAsync(user, cancellationToken));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Добавление новой валюты
    /// </summary>
    /// <response code="200">Валюта успешно добавлена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPost(Name = "PostCurrency")]
    public async Task<ActionResult> PostCurrency(
        PostCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        await _currencyService.PostCurrencyAsync(request, cancellationToken);
        return Ok();
    }

    #endregion POST

    #region PUT

    /// <summary>
    /// Изменение валюты
    /// </summary>
    /// <response code="200">Валюта успешно изменена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Валюты с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpPut(Name = "PutCurrencyInfo")]
    public async Task<ActionResult> PutCurrencyInfo(
        PutCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        await _currencyService.PutCurrencyInfoAsync(request, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Установка валюты пользователя
    /// </summary>
    /// <response code="200">Валюта успешно установлена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Валюты с таким Id не существует или пользователь не найден</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpPut(Name = "SetCurrency")]
    public async Task<ActionResult> SetCurrency(
        SetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        await _currencyService.SetCurrencyAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Удаление валюты
    /// </summary>
    /// <response code="200">Валюта успешно удалена</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="404">Валюты с таким Id не существует</response>
    /// <response code="499">Операция отменена</response>
    [Authorize(Roles = nameof(Role.Roles.Admin))]
    [HttpDelete(Name = "DeleteCurrency")]
    public async Task<ActionResult> DeleteCurrency(
        DeleteCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        await _currencyService.DeleteCurrencyAsync(request, cancellationToken);
        return Ok();
    }

    #endregion DELETE
}