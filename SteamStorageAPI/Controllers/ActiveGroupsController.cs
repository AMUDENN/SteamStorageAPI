using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.SkinService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;

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

        private readonly ILogger<ActiveGroupsController> _logger;
        private readonly ISkinService _skinService;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ActiveGroupOrderName, Func<ActiveGroup, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public ActiveGroupsController(ILogger<ActiveGroupsController> logger, ISkinService skinService,
            IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _skinService = skinService;
            _userService = userService;
            _context = context;

            _orderNames = new()
            {
                [ActiveGroupOrderName.Title] = x => x.Title,
                [ActiveGroupOrderName.Count] = x => _context.Entry(x).Collection(y => y.Actives).Query().Count(),
                [ActiveGroupOrderName.BuySum] = x =>
                    _context.Entry(x).Collection(y => y.Actives).Query().Sum(z => z.Count * z.BuyPrice),
                [ActiveGroupOrderName.CurrentSum] = x =>
                    _context.Entry(x).Collection(y => y.Actives).Query().Include(z => z.Skin)
                        .Sum(w => w.Count * _skinService.GetCurrentPrice(w.Skin)),
                [ActiveGroupOrderName.Change] = x =>
                {
                    decimal buySum = _context.Entry(x).Collection(y => y.Actives).Query()
                        .Sum(z => z.Count * z.BuyPrice);
                    decimal currentSum = _context.Entry(x).Collection(y => y.Actives).Query().Include(z => z.Skin)
                        .Sum(w => w.Count * _skinService.GetCurrentPrice(w.Skin));

                    return buySum == 0 ? 0 : (currentSum - buySum) / buySum;
                }
            };
        }

        #endregion Constructor

        #region Records

        public record ActiveGroupsResponse(
            int Id,
            string Title,
            string Description,
            string Colour,
            decimal? GoalSum);

        public record ActiveGroupDynamicsResponse(
            int Id,
            DateTime DateUpdate,
            decimal Sum);

        public record ActiveGroupsCountResponse(
            int Count);

        public record GetActiveGroupsRequest(
            ActiveGroupOrderName? OrderName,
            bool? IsAscending);

        public record GetActiveGroupDynamicRequest(
            int GroupId,
            int DaysDynamic);

        public record PostActiveGroupRequest(
            string Title,
            string? Description,
            string? Colour,
            decimal? GoalSum);

        public record PutActiveGroupRequest(
            int GroupId,
            string Title,
            string? Description,
            string? Colour,
            decimal? GoalSum);

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
        [HttpGet(Name = "GetActiveGroups")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<ActiveGroupsResponse>> GetActiveGroups(
            [FromQuery] GetActiveGroupsRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<ActiveGroup> groups = _context.Entry(user).Collection(x => x.ActiveGroups).Query();

                if (request is { OrderName: not null, IsAscending: not null })
                    groups = (bool)request.IsAscending
                        ? groups.OrderBy(_orderNames[(ActiveGroupOrderName)request.OrderName])
                        : groups.OrderByDescending(_orderNames[(ActiveGroupOrderName)request.OrderName]);

                return Ok(groups.Select(x =>
                    new ActiveGroupsResponse(x.Id,
                        x.Title,
                        x.Description ?? string.Empty,
                        $"#{x.Colour ?? ProgramConstants.BASE_ACTIVE_GROUP_COLOUR}",
                        x.GoalSum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение динамики стоимости группы активов
        /// </summary>
        /// <response code="200">Возвращает динамику группы активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        [HttpGet(Name = "GetActiveGroupDynamics")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<IEnumerable<ActiveGroupDynamicsResponse>> GetActiveGroupDynamics(
            [FromQuery] GetActiveGroupDynamicRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound(
                        "У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

                DateTime startDate = DateTime.Now.AddDays(-request.DaysDynamic);

                return Ok(_context.Entry(group)
                    .Collection(s => s.ActiveGroupsDynamics)
                    .Query()
                    .Where(x => x.DateUpdate > startDate)
                    .Select(x =>
                        new ActiveGroupDynamicsResponse(x.Id,
                            x.DateUpdate,
                            x.Sum)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества групп активов
        /// </summary>
        /// <response code="200">Возвращает количество групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetActiveGroupsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<ActiveGroupsCountResponse> GetActiveGroupsCount()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ActiveGroupsCountResponse(_context.Entry(user).Collection(x => x.ActiveGroups).Query()
                    .Count()));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpPost(Name = "PostActiveGroup")]
        public async Task<ActionResult> PostActiveGroup(PostActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                _context.ActiveGroups.Add(new()
                {
                    UserId = user.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Colour = request.Colour,
                    GoalSum = request.GoalSum
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

        /// <summary>
        /// Изменение группы активов
        /// </summary>
        /// <response code="200">Группа успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroup(PutActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user).Collection(u => u.ActiveGroups).Query()
                    .FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                group.Title = request.Title;
                group.Description = request.Description;
                group.Colour = request.Colour;
                group.GoalSum = request.GoalSum;

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

        /// <summary>
        /// Удаление группы активов
        /// </summary>
        /// <response code="200">Группа успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(DeleteActiveGroupRequest request)
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ActiveGroup? group = _context.Entry(user).Collection(u => u.ActiveGroups).Query()
                    .FirstOrDefault(x => x.Id == request.GroupId);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                _context.ActiveGroups.Remove(group);

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
