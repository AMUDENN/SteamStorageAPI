using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.Archives;
using static SteamStorageAPI.Controllers.SkinsController;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
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
        }

        #endregion Constructor

        #region Records

        public record ArchiveResponse(
            int Id,
            int GroupId,
            BaseSkinResponse Skin,
            DateTime BuyDate,
            DateTime SoldDate,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            decimal SoldSum,
            double Change,
            string? Description);

        public record ArchivesResponse(
            int Count,
            int PagesCount,
            IEnumerable<ArchiveResponse> Archives);
        
        public record ArchivesStatisticResponse(
            int ArchivesCount,
            decimal InvestmentSum,
            decimal SoldSum);

        public record ArchivesPagesCountResponse(
            int Count);

        public record ArchivesCountResponse(
            int Count);

        [Validator<GetArchiveInfoRequestValidator>]
        public record GetArchiveInfoRequest(
            int Id);
        
        [Validator<GetArchivesRequestValidator>]
        public record GetArchivesRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            ArchiveOrderName? OrderName,
            bool? IsAscending,
            int PageNumber,
            int PageSize);
        
        [Validator<GetArchivesStatisticRequestValidator>]
        public record GetArchivesStatisticRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        [Validator<GetArchivesPagesCountRequestValidator>]
        public record GetArchivesPagesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter,
            int PageSize);

        [Validator<GetArchivesCountRequestValidator>]
        public record GetArchivesCountRequest(
            int? GroupId,
            int? GameId,
            string? Filter);

        [Validator<PostArchiveRequestValidator>]
        public record PostArchiveRequest(
            int GroupId,
            int Count,
            decimal BuyPrice,
            decimal SoldPrice,
            int SkinId,
            string? Description,
            DateTime BuyDate,
            DateTime SoldDate);

        [Validator<PutArchiveRequestValidator>]
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

        [Validator<DeleteArchiveRequestValidator>]
        public record DeleteArchiveRequest(
            int Id);

        #endregion Records

        #region Methods

        private async Task<ArchiveResponse> GetArchiveResponseAsync(
            Archive archive,
            CancellationToken cancellationToken = default)
        {
            return new(archive.Id,
                archive.GroupId,
                await _skinService.GetBaseSkinResponseAsync(archive.Skin, cancellationToken),
                archive.BuyDate,
                archive.SoldDate,
                archive.Count,
                archive.BuyPrice,
                archive.SoldPrice,
                archive.SoldPrice * archive.Count,
                (double)((archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice),
                archive.Description);
        }

        private async Task<ArchivesResponse> GetArchivesResponseAsync(
            IQueryable<Archive> archives,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            int archivesCount = await archives.CountAsync(cancellationToken);

            int pagesCount = (int)Math.Ceiling((double)archivesCount / pageSize);

            archives = archives.AsNoTracking()
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize);

            return new(archivesCount,
                pagesCount,
                await Task.WhenAll(archives.AsEnumerable()
                    .Select(async x =>
                        new ArchiveResponse(
                            x.Id,
                            x.GroupId,
                            await _skinService.GetBaseSkinResponseAsync(x.Skin, cancellationToken),
                            x.BuyDate,
                            x.SoldDate,
                            x.Count,
                            x.BuyPrice,
                            x.SoldPrice,
                            x.SoldPrice * x.Count,
                            (double)((x.SoldPrice - x.BuyPrice) / x.BuyPrice),
                            x.Description))).WaitAsync(cancellationToken));
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение информации об элементе архива
        /// </summary>
        /// <response code="200">Возвращает подробную информацию об элементе архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Элемента архива с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetArchiveInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveResponse>> GetArchiveInfo(
            [FromQuery] GetArchiveInfoRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            Archive archive = await _context.Entry(user)
                                  .Collection(x => x.ArchiveGroups)
                                  .Query()
                                  .AsNoTracking()
                                  .SelectMany(x => x.Archives)
                                  .FirstOrDefaultAsync(x => x.Id == request.Id, cancellationToken) ??
                              throw new HttpResponseException(StatusCodes.Status404NotFound,
                                  "Группы активов с таким Id не существует");

            return Ok(await GetArchiveResponseAsync(archive, cancellationToken));
        }

        /// <summary>
        /// Получение списка элементов архива
        /// </summary>
        /// <response code="200">Возвращает список элементов архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetArchives")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesResponse>> GetArchives(
            [FromQuery] GetArchivesRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin.Game)
                .SelectMany(x => x.Archives)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                            && (request.GroupId == null || x.GroupId == request.GroupId));

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ArchiveOrderName.Title:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => x.Skin.Title)
                            : archives.OrderByDescending(x => x.Skin.Title);
                        break;
                    case ArchiveOrderName.Count:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => x.Count)
                            : archives.OrderByDescending(x => x.Count);
                        break;
                    case ArchiveOrderName.BuyPrice:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => x.BuyPrice)
                            : archives.OrderByDescending(x => x.BuyPrice);
                        break;
                    case ArchiveOrderName.SoldPrice:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => x.SoldPrice)
                            : archives.OrderByDescending(x => x.SoldPrice);
                        break;
                    case ArchiveOrderName.SoldSum:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => x.SoldPrice * x.Count)
                            : archives.OrderByDescending(x => x.SoldPrice * x.Count);
                        break;
                    case ArchiveOrderName.Change:
                        archives = request.IsAscending.Value
                            ? archives.OrderBy(x => (x.SoldPrice - x.BuyPrice) / x.BuyPrice)
                            : archives.OrderByDescending(x => (x.SoldPrice - x.BuyPrice) / x.BuyPrice);
                        break;
                }
            else
                archives = archives.OrderBy(x => x.Id);

            return Ok(await GetArchivesResponseAsync(archives, request.PageNumber, request.PageSize, cancellationToken));
        }

        /// <summary>
        /// Получение статистики по выборке элементов архива
        /// </summary>
        /// <response code="200">Возвращает статистику по выборке элементов архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetArchivesStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesStatisticResponse>> GetArchivesStatistic(
            [FromQuery] GetArchivesStatisticRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .Where(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                            && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
                            && (request.GroupId == null || x.GroupId == request.GroupId));

            return Ok(new ArchivesStatisticResponse(
                archives.Sum(x => x.Count),
                archives.Sum(x => x.BuyPrice * x.Count),
                archives.Sum(x => x.SoldPrice * x.Count)));
        }

        /// <summary>
        /// Получение количества страниц архива
        /// </summary>
        /// <response code="200">Возвращает количество страниц архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetArchivesPagesCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchivesPagesCountResponse>> GetArchivesPagesCount(
            [FromQuery] GetArchivesPagesCountRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            int count = await _context
                .Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
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
        [Authorize]
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
                .AsNoTracking()
                .Include(x => x.Archives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Archives)
                .CountAsync(x => (request.GameId == null || x.Skin.GameId == request.GameId)
                                 && (string.IsNullOrEmpty(request.Filter) || x.Skin.Title.Contains(request.Filter))
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
        [Authorize]
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
        [Authorize]
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
        [Authorize]
        [HttpDelete(Name = "DeleteArchive")]
        public async Task<ActionResult> DeleteArchive(
            DeleteArchiveRequest request,
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

            _context.Archives.Remove(archive);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
