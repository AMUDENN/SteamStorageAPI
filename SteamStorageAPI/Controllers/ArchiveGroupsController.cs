using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ArchiveGroupsController : ControllerBase
    {
        #region Enums

        public enum ArchiveGroupOrderName
        {
            Title,
            Count,
            BuySum,
            SoldSum,
            Change
        }

        #endregion Enums

        #region Fields

        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public ArchiveGroupsController(
            IUserService userService,
            ICurrencyService currencyService,
            SteamStorageContext context)
        {
            _userService = userService;
            _currencyService = currencyService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record ArchiveGroupResponse(
            int Id,
            string Title,
            string? Description,
            string Colour,
            int Count,
            decimal BuySum,
            decimal SoldSum,
            double Change,
            DateTime DateCreation);

        public record ArchiveGroupsResponse(
            int Count,
            IEnumerable<ArchiveGroupResponse> ArchiveGroups);

        public record ArchiveGroupsCountResponse(
            int Count);

        [Validator<GetArchiveGroupsRequestValidator>]
        public record GetArchiveGroupsRequest(
            ArchiveGroupOrderName? OrderName,
            bool? IsAscending);

        [Validator<PostArchiveGroupRequestValidator>]
        public record PostArchiveGroupRequest(
            string Title,
            string? Description,
            string? Colour);

        [Validator<PutArchiveGroupRequestValidator>]
        public record PutArchiveGroupRequest(
            int GroupId,
            string Title,
            string? Description,
            string? Colour);

        [Validator<DeleteArchiveGroupRequestValidator>]
        public record DeleteArchiveGroupRequest(
            int GroupId);

        #endregion Records

        #region Methods

        private async Task<IEnumerable<ArchiveGroupResponse>> GetArchiveGroupsResponsesAsync(
            IQueryable<ArchiveGroup> groups,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            groups = groups.AsNoTracking().Include(x => x.Archives);

            List<ArchiveGroupResponse> result = await groups.Select(x =>
                new ArchiveGroupResponse(
                    x.Id,
                    x.Title,
                    x.Description,
                    $"#{x.Colour ?? ArchiveGroup.BASE_ARCHIVE_GROUP_COLOUR}",
                    x.Archives.Sum(y => y.Count),
                    (decimal)((double)x.Archives.Sum(y => y.BuyPrice * y.Count) * currencyExchangeRate),
                    (decimal)((double)x.Archives.Sum(y => y.SoldPrice * y.Count) * currencyExchangeRate),
                    x.Archives.Sum(y => y.BuyPrice) != 0
                        ? ((double)x.Archives.Sum(y => y.SoldPrice) - (double)x.Archives.Sum(y => y.BuyPrice)) /
                          (double)x.Archives.Sum(y => y.BuyPrice)
                        : 1,
                    x.DateCreation))
                .ToListAsync(cancellationToken);

            return result;
        }

        #endregion Methods
        
        #region GET

        /// <summary>
        /// Получение списка групп архива
        /// </summary>
        /// <response code="200">Возвращает список групп архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchiveGroups")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveGroupsResponse>> GetArchiveGroups(
            [FromQuery] GetArchiveGroupsRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<ArchiveGroup> groups = _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives);

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ArchiveGroupOrderName.Title:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Title)
                            : groups.OrderByDescending(x => x.Title);
                        break;
                    case ArchiveGroupOrderName.Count:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Archives.Sum(y => y.Count))
                            : groups.OrderByDescending(x => x.Archives.Sum(y => y.Count));
                        break;
                    case ArchiveGroupOrderName.BuySum:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Archives.Sum(y => y.BuyPrice * y.Count))
                            : groups.OrderByDescending(x => x.Archives.Sum(y => y.BuyPrice * y.Count));
                        break;
                    case ArchiveGroupOrderName.SoldSum:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Archives.Sum(y => y.SoldPrice * y.Count))
                            : groups.OrderByDescending(x => x.Archives.Sum(y => y.SoldPrice * y.Count));
                        break;
                    case ArchiveGroupOrderName.Change:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x =>
                                (x.Archives.Sum(y => y.SoldPrice) -
                                 x.Archives.Sum(y => y.BuyPrice)) /
                                x.Archives.Sum(y => y.BuyPrice))
                            : groups.OrderByDescending(x =>
                                (x.Archives.Sum(y => y.SoldPrice) -
                                 x.Archives.Sum(y => y.BuyPrice)) /
                                x.Archives.Sum(y => y.BuyPrice));
                        break;
                }

            return Ok(new ArchiveGroupsResponse(await groups.CountAsync(cancellationToken),
                await GetArchiveGroupsResponsesAsync(groups, user, cancellationToken)));
        }

        /// <summary>
        /// Получение количества групп архива
        /// </summary>
        /// <response code="200">Возвращает количество групп архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchiveGroupsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveGroupsCountResponse>> GetArchiveGroupsCount(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new ArchiveGroupsCountResponse(await _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .CountAsync(cancellationToken)));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление новой группы архива
        /// </summary>
        /// <response code="200">Группа успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostArchiveGroup")]
        public async Task<ActionResult> PostArchiveGroup(
            PostArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            await _context.ArchiveGroups.AddAsync(new()
            {
                UserId = user.Id,
                Title = request.Title,
                Description = request.Description,
                Colour = request.Colour,
                DateCreation = DateTime.Now
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение группы архива
        /// </summary>
        /// <response code="200">Группа успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutArchiveGroup")]
        public async Task<ActionResult> PutArchiveGroup(
            PutArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ArchiveGroup group = await _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                                     .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                 throw new HttpResponseException(StatusCodes.Status404NotFound,
                                     "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            group.Title = request.Title;
            group.Description = request.Description;
            group.Colour = request.Colour;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление группы архива
        /// </summary>
        /// <response code="200">Группа успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteArchiveGroup")]
        public async Task<ActionResult> DeleteArchiveGroup(
            DeleteArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ArchiveGroup group = await _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                                     .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                 throw new HttpResponseException(StatusCodes.Status404NotFound,
                                     "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            _context.ArchiveGroups.Remove(group);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
