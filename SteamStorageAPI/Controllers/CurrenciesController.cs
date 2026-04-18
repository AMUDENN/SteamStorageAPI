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
    /// Get the list of currencies
    /// </summary>
    /// <response code="200">Returns the list of currencies</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetCurrencies")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<CurrenciesResponse>> GetCurrencies(
        CancellationToken cancellationToken = default)
    {
        return Ok(await _currencyService.GetCurrenciesAsync(cancellationToken));
    }

    /// <summary>
    /// Get information about a currency
    /// </summary>
    /// <response code="200">Returns information about the currency</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No currency with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
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
    /// Get the current currency of the user
    /// </summary>
    /// <response code="200">Returns the current currency of the user</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No currency with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpGet(Name = "GetCurrentCurrency")]
    [Produces(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<CurrencyResponse>> GetCurrentCurrency(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        return Ok(await _currencyService.GetCurrentCurrencyAsync(user, cancellationToken));
    }

    #endregion GET

    #region POST

    /// <summary>
    /// Add a new currency
    /// </summary>
    /// <response code="200">The currency was successfully added</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="499">The operation was cancelled</response>
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
    /// Update a currency
    /// </summary>
    /// <response code="200">The currency was successfully updated</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No currency with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
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
    /// Set the user's currency
    /// </summary>
    /// <response code="200">The currency was successfully set</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No currency with the given Id exists, or the user was not found</response>
    /// <response code="499">The operation was cancelled</response>
    [Authorize]
    [HttpPut(Name = "SetCurrency")]
    public async Task<ActionResult> SetCurrency(
        SetCurrencyRequest request,
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "No user with the given Id exists");

        await _currencyService.SetCurrencyAsync(user, request, cancellationToken);
        return Ok();
    }

    #endregion PUT

    #region DELETE

    /// <summary>
    /// Delete a currency
    /// </summary>
    /// <response code="200">The currency was successfully deleted</response>
    /// <response code="401">The user is not authorized</response>
    /// <response code="404">No currency with the given Id exists</response>
    /// <response code="499">The operation was cancelled</response>
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