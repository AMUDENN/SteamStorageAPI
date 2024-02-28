using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class ActiveGroupsController : ControllerBase
    {
        #region Enums

        public enum ActiveGroupOrderName
        {
            Title,
            Count,
            BuySum,
            CurrentSum,
            Change
        }

        #endregion Enums

        #region Fields

        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public ActiveGroupsController(
            IUserService userService,
            SteamStorageContext context)
        {
            _userService = userService;
            _context = context;
        }

        #endregion Constructor

        #region Records

        public record ActiveGroupResponse(
            int Id,
            string Title,
            string Description,
            string Colour,
            decimal? GoalSum,
            double? GoalSumCompletion,
            decimal BuySum,
            decimal CurrentSum,
            double Change,
            DateTime DateCreation);

        public record ActiveGroupsResponse(
            int Count,
            IEnumerable<ActiveGroupResponse> ActiveGroups);

        public record ActiveGroupDynamicsResponse(
            int Id,
            DateTime DateUpdate,
            decimal Sum);

        public record ActiveGroupsCountResponse(
            int Count);

        [Validator<GetActiveGroupsRequestValidator>]
        public record GetActiveGroupsRequest(
            ActiveGroupOrderName? OrderName,
            bool? IsAscending);

        [Validator<GetActiveGroupDynamicRequestValidator>]
        public record GetActiveGroupDynamicRequest(
            int GroupId,
            DateTime StartDate,
            DateTime EndDate);

        [Validator<PostActiveGroupRequestValidator>]
        public record PostActiveGroupRequest(
            string Title,
            string? Description,
            string? Colour,
            decimal? GoalSum);

        [Validator<PutActiveGroupRequestValidator>]
        public record PutActiveGroupRequest(
            int GroupId,
            string Title,
            string? Description,
            string? Colour,
            decimal? GoalSum);

        [Validator<DeleteActiveGroupRequestValidator>]
        public record DeleteActiveGroupRequest(
            int GroupId);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение списка групп активов
        /// </summary>
        /// <response code="200">Возвращает список групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetActiveGroups")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupsResponse>> GetActiveGroups(
            [FromQuery] GetActiveGroupsRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<ActiveGroup> groups = _context.Entry(user).Collection(x => x.ActiveGroups).Query()
                .Include(x => x.Actives).ThenInclude(x => x.Skin).ThenInclude(x => x.SkinsDynamics);

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ActiveGroupOrderName.Title:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Title)
                            : groups.OrderByDescending(x => x.Title);
                        break;
                    case ActiveGroupOrderName.Count:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Actives.Sum(y => y.Count))
                            : groups.OrderByDescending(x => x.Actives.Sum(y => y.Count));
                        break;
                    case ActiveGroupOrderName.BuySum:
                        groups = request.IsAscending.Value
                            ? groups.OrderBy(x => x.Actives.Sum(y => y.BuyPrice * y.Count))
                            : groups.OrderByDescending(x => x.Actives.Sum(y => y.BuyPrice * y.Count));
                        break;
                    case ActiveGroupOrderName.CurrentSum:
                        //TODO: сортирока
                        break;
                    case ActiveGroupOrderName.Change:
                        //TODO: сортирока
                        break;
                }

            List<ActiveGroup> groupsList = await groups.ToListAsync(cancellationToken);
            IEnumerable<ActiveGroupResponse> activeGroups = groupsList.Select(x => new ActiveGroupResponse(
                x.Id,
                x.Title,
                x.Description ?? string.Empty,
                $"#{x.Colour ?? ActiveGroup.BASE_ACTIVE_GROUP_COLOUR}",
                x.GoalSum,
                x.GoalSum.HasValue
                    ? x.GoalSum.Value == 0
                        ? 1
                        : (double)(x.Actives.Sum(y =>
                            y.Skin.SkinsDynamics.Count != 0
                                ? y.Skin.SkinsDynamics.MaxBy(z => z.DateUpdate)!.Price * y.Count
                                : 0) / x.GoalSum.Value)
                    : null,
                x.Actives.Sum(y => y.BuyPrice * y.Count),
                x.Actives.Sum(y =>
                    y.Skin.SkinsDynamics.Count != 0
                        ? y.Skin.SkinsDynamics.MaxBy(z => z.DateUpdate)!.Price * y.Count
                        : 0),
                1, //TODO:
                DateTime.Now));

            //TODO: Добавить дату создания группы в бд

            return Ok(new ActiveGroupsResponse(await groups.CountAsync(cancellationToken), activeGroups));
        }

        /// <summary>
        /// Получение динамики стоимости группы активов
        /// </summary>
        /// <response code="200">Возвращает динамику группы активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetActiveGroupDynamics")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<ActiveGroupDynamicsResponse>>> GetActiveGroupDynamics(
            [FromQuery] GetActiveGroupDynamicRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ActiveGroup group = await _context.Entry(user)
                                    .Collection(u => u.ActiveGroups)
                                    .Query()
                                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                    "У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

            DateTime startDate = request.StartDate.Date;

            DateTime endDate = request.EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            return Ok(_context.Entry(group)
                .Collection(s => s.ActiveGroupsDynamics)
                .Query()
                .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
                .Select(x =>
                    new ActiveGroupDynamicsResponse(x.Id, x.DateUpdate, x.Sum)));
        }

        /// <summary>
        /// Получение количества групп активов
        /// </summary>
        /// <response code="200">Возвращает количество групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetActiveGroupsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupsCountResponse>> GetActiveGroupsCount(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new ActiveGroupsCountResponse(await _context.Entry(user).Collection(x => x.ActiveGroups)
                .Query().CountAsync(cancellationToken)));
        }

        #endregion GET

        #region POST

        /// <summary>
        /// Добавление новой группы активов
        /// </summary>
        /// <response code="200">Группа успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPost(Name = "PostActiveGroup")]
        public async Task<ActionResult> PostActiveGroup(
            PostActiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            await _context.ActiveGroups.AddAsync(new()
            {
                UserId = user.Id,
                Title = request.Title,
                Description = request.Description,
                Colour = request.Colour,
                GoalSum = request.GoalSum
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion POST

        #region PUT

        /// <summary>
        /// Изменение группы активов
        /// </summary>
        /// <response code="200">Группа успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroup(
            PutActiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ActiveGroup group = await _context.Entry(user).Collection(u => u.ActiveGroups).Query()
                                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            group.Title = request.Title;
            group.Description = request.Description;
            group.Colour = request.Colour;
            group.GoalSum = request.GoalSum;

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion PUT

        #region DELETE

        /// <summary>
        /// Удаление группы активов
        /// </summary>
        /// <response code="200">Группа успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(
            DeleteActiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ActiveGroup group = await _context.Entry(user).Collection(u => u.ActiveGroups).Query()
                                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            _context.ActiveGroups.Remove(group);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
