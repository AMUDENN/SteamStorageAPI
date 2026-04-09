using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Domain.FileService;
using SteamStorageAPI.Services.Infrastructure.ContextUserService;
using SteamStorageAPI.Utilities.Exceptions;

// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FileController : ControllerBase
{
    #region Fields

    private readonly IContextUserService _contextUserService;
    private readonly IFileService _fileService;

    #endregion Fields

    #region Constructor

    public FileController(
        IContextUserService contextUserService,
        IFileService fileService)
    {
        _contextUserService = contextUserService;
        _fileService = fileService;
    }

    #endregion Constructor

    #region GET

    /// <summary>
    /// Получение информации об инвестициях в Excel файле
    /// </summary>
    /// <response code="200">Возвращает информации об инвестициях в Excel файле</response>
    /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
    /// <response code="401">Пользователь не прошёл авторизацию</response>
    /// <response code="499">Операция отменена</response>
    [Authorize]
    [HttpGet(Name = "GetExcelFile")]
    [Produces(MediaTypeNames.Application.Octet)]
    public async Task<ActionResult> GetExcelFile(
        CancellationToken cancellationToken = default)
    {
        User user = await _contextUserService.GetContextUserAsync(cancellationToken)
                    ?? throw new HttpResponseException(StatusCodes.Status404NotFound,
                        "Пользователя с таким Id не существует");

        byte[] fileBytes = await _fileService.GetExcelFileAsync(user, cancellationToken);

        return File(fileBytes,
            "application/octet-stream",
            $"{DateTime.Now:dd.MM.yyyy#hh.mm}.xlsx");
    }

    #endregion GET
}