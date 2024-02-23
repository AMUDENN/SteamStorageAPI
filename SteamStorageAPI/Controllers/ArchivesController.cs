using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using static SteamStorageAPI.Controllers.SkinsController;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ArchivesController : ControllerBase
    {
        #region Enums

        public enum ArchiveOrderName
        {
            Title,
            Count,
            BuyPrice,
            SoldPrice,
            SoldSum,
            Change
        }

        #endregion Enums

        #region Fields

        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ArchiveOrderName, Func<Archive, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public ArchivesController(
            ISkinService skinService,
            IUserService userService,
            SteamStorageContext context)
        {
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                // TODO: Сортировка по параметрам!
            };
        }

        #endregion Constructor

        #region Records

        public record ArchiveResponse(
            int Id,
            BaseSkinResponse Skin,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            decimal SoldSum,
            double Change);

        public record ArchivesResponse(
            int ArchivesCount,
            int PagesCount,
            IEnumerable<ArchiveResponse> Skins);

        public record ArchivesPagesCountResponse(
            int Count);

        public record ArchivesCountResponse(
            int Count);

        public record GetArchivesRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            ArchiveOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);

        public record GetArchivesPagesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            int PageSize);

        public record GetArchivesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        public record PostArchiveRequest(
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate,
            DateTime SoldDate);

        public record PutArchiveRequest(
            int Id,
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate,
            DateTime SoldDate);

        public record DeleteArchiveRequest(
            int Id);

        #endregion Records

        #region Methods

        private async Task<ArchiveResponse> GetArchiveResponseAsync(
            Archive archive,
            CancellationToken cancellationToken = default)
        {
            return new(archive.Id,
                await _skinService.GetBaseSkinResponseAsync(archive.Skin, cancellationToken),
                archive.Count,
                archive.BuyPrice,
                archive.SoldPrice,
                archive.SoldPrice * archive.Count,
                (double)(archive.BuyPrice == 0 ? 0 : (archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice));
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение списка элементов архива
        /// </summary>
        /// <response code="200">Возвращает список элементов архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchives")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesResponse>> GetArchives(
            [FromQuery] GetArchivesRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.PageNumber <= 0 || request.PageSize <= 0)
                throw new("Размер и номер страницы не могут быть меньше или равны нулю.");

            if (request.PageSize > 200)
                throw new("Размер страницы не может превышать 200 предметов");

            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IEnumerable<Archive> archives = _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                            && (request.GroupId == null || x.GroupId == request.GroupId));

            if (request is { OrderName: not null, IsAscending: not null })
                archives = (bool)request.IsAscending
                    ? archives.OrderBy(_orderNames[(ArchiveOrderName)request.OrderName])
                    : archives.OrderByDescending(_orderNames[(ArchiveOrderName)request.OrderName]);

            archives = archives.Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize);

            int archivesCount = await _context
                .Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                 && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)archivesCount / request.PageSize);

            return Ok(new ArchivesResponse(archivesCount, pagesCount,
                archives.Select(x => GetArchiveResponseAsync(x, cancellationToken).Result)));
        }

        /// <summary>
        /// Получение количества страниц архива
        /// </summary>
        /// <response code="200">Возвращает количество страниц архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchivesPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesPagesCountResponse>> GetArchivesPagesCount(
            [FromQuery] GetArchivesPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request.PageSize <= 0)
                throw new("Размер страницы не может быть меньше или равен нулю.");

            if (request.PageSize > 200)
                throw new("Размер страницы не может превышать 200 предметов");

            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            int count = await _context
                .Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                 && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)count / request.PageSize);

            return Ok(new ArchivesPagesCountResponse(pagesCount == 0 ? 1 : pagesCount));
        }

        /// <summary>
        /// Получение количества элементов архива
        /// </summary>
        /// <response code="200">Возвращает количество элементов архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchivesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesCountResponse>> GetArchivesCount(
            [FromQuery] GetArchivesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new ArchivesCountResponse(await _context
                .Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter!))
                                 && (request.GroupId == null || x.GroupId == request.GroupId), cancellationToken)));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление элемента архива
        /// </summary>
        /// <response code="200">Элемент архива успешно добавлен</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostArchive")]
        public async Task<ActionResult> PostArchive(
            PostArchiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            if (!await _context.Entry(user).Collection(x => x.ArchiveGroups).Query()
                    .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Предмета с таким Id не существует");

            await _context.Archives.AddAsync(new()
            {
                GroupId = request.GroupId,
                Count = request.Count,
                BuyPrice = request.BuyPrice,
                SoldPrice = request.SoldPrice,
                SkinId = request.SkinId,
                Description = request.Description,
                BuyDate = request.BuyDate,
                SoldDate = request.SoldDate
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение элемента архива
        /// </summary>
        /// <response code="200">Элемент архива успешно изменён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Элемента архива с таким Id не существует, группы с таким Id не существует, предмета с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutArchive")]
        public async Task<ActionResult> PutArchive(
            PutArchiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Archive archive = await _context.Entry(user)
                .Collection(u => u.ArchiveGroups)
                .Query()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ?? throw new HttpResponseException(
                StatusCodes.Status404NotFound,
                "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

            if (!await _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                    .AnyAsync(x => x.Id == request.GroupId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound,
                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            if (!await _context.Skins.AnyAsync(x => x.Id == request.SkinId, cancellationToken))
                throw new HttpResponseException(StatusCodes.Status404NotFound, "Предмета с таким Id не существует");

            archive.GroupId = request.GroupId;
            archive.Count = request.Count;
            archive.BuyPrice = request.BuyPrice;
            archive.SoldPrice = request.SoldPrice;
            archive.SkinId = request.SkinId;
            archive.Description = request.Description;
            archive.BuyDate = request.BuyDate;
            archive.SoldDate = request.SoldDate;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление элемента архива
        /// </summary>
        /// <response code="200">Элемент архива успешно удалён</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Элемента архива с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteArchive")]
        public async Task<ActionResult> DeleteArchive(
            DeleteArchiveRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Archive archive = await _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                .Include(x => x.Archives).SelectMany(x => x.Archives)
                .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ?? throw new HttpResponseException(
                StatusCodes.Status404NotFound,
                "У вас нет доступа к изменению этого актива или актива с таким Id не существует");

            _context.Archives.Remove(archive);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
