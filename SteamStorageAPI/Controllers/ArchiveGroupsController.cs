using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;

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

        private readonly ILogger<ArchiveGroupsController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        private readonly Dictionary<ArchiveGroupOrderName, Func<ArchiveGroup, object>> _orderNames;

        #endregion Fields

        #region Constructor

        public ArchiveGroupsController(
            ILogger<ArchiveGroupsController> logger, 
            IUserService userService,
            SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;


            _orderNames = new()
            {
                [ArchiveGroupOrderName.Title] = x => x.Title,
                [ArchiveGroupOrderName.Count] = x => _context.Entry(x).Collection(y => y.Archives).Query().Count(),
                [ArchiveGroupOrderName.BuySum] = x =>
                    _context.Entry(x).Collection(y => y.Archives).Query().Sum(z => z.Count * z.BuyPrice),
                [ArchiveGroupOrderName.SoldSum] = x =>
                    _context.Entry(x).Collection(y => y.Archives).Query().Sum(z => z.Count * z.SoldPrice),
                [ArchiveGroupOrderName.Change] = x =>
                {
                    decimal buySum = _context.Entry(x).Collection(y => y.Archives).Query()
                        .Sum(z => z.Count * z.BuyPrice);
                    decimal soldSum = _context.Entry(x).Collection(y => y.Archives).Query()
                        .Sum(z => z.Count * z.SoldPrice);

                    return buySum == 0 ? 0 : (soldSum - buySum) / buySum;
                }
            };
        }

        #endregion Constructor

        #region Records

        public record ArchiveGroupsResponse(
            int Id,
            string Title,
            string Description,
            string Colour);

        public record ArchiveGroupsCountResponse(
            int Count);

        public record GetArchiveGroupsRequest(
            ArchiveGroupOrderName? OrderName,
            bool? IsAscending);

        public record PostArchiveGroupRequest(
            string Title,
            string? Description,
            string? Colour);

        public record PutArchiveGroupRequest(
            int GroupId,
            string Title,
            string? Description,
            string? Colour);

        public record DeleteArchiveGroupRequest(
            int GroupId);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение списка групп архива
        /// </summary>
        /// <response code="200">Возвращает список групп архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetArchiveGroups")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<IEnumerable<ArchiveGroupsResponse>>> GetArchiveGroups(
            [FromQuery] GetArchiveGroupsRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                IEnumerable<ArchiveGroup> groups = _context.Entry(user).Collection(x => x.ArchiveGroups).Query();

                if (request is { OrderName: not null, IsAscending: not null })
                    groups = (bool)request.IsAscending
                        ? groups.OrderBy(_orderNames[(ArchiveGroupOrderName)request.OrderName])
                        : groups.OrderByDescending(_orderNames[(ArchiveGroupOrderName)request.OrderName]);

                return Ok(groups.Select(x =>
                    new ArchiveGroupsResponse(x.Id,
                        x.Title,
                        x.Description ?? string.Empty,
                        $"#{x.Colour ?? ProgramConstants.BASE_ARCHIVE_GROUP_COLOUR}")));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение количества групп архива
        /// </summary>
        /// <response code="200">Возвращает количество групп архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetArchiveGroupsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveGroupsCountResponse>> GetArchiveGroupsCount(
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                return Ok(new ArchiveGroupsCountResponse(await _context.Entry(user).Collection(x => x.ArchiveGroups)
                    .Query().CountAsync(cancellationToken)));
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
        /// Добавление новой группы архива
        /// </summary>
        /// <response code="200">Группа успешно добавлена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpPost(Name = "PostArchiveGroup")]
        public async Task<ActionResult> PostArchiveGroup(
            PostArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                await _context.ArchiveGroups.AddAsync(new()
                {
                    UserId = user.Id,
                    Title = request.Title,
                    Description = request.Description,
                    Colour = request.Colour
                }, cancellationToken);

                await _context.SaveChangesAsync(cancellationToken);

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
        /// Изменение группы архива
        /// </summary>
        /// <response code="200">Группа успешно изменена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        [HttpPut(Name = "PutArchiveGroup")]
        public async Task<ActionResult> PutArchiveGroup(
            PutArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = await _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                group.Title = request.Title;
                group.Description = request.Description;
                group.Colour = request.Colour;

                await _context.SaveChangesAsync(cancellationToken);

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
        /// Удаление группы архива
        /// </summary>
        /// <response code="200">Группа успешно удалена</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        [HttpDelete(Name = "DeleteArchiveGroup")]
        public async Task<ActionResult> DeleteArchiveGroup(
            DeleteArchiveGroupRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                User? user = await _userService.GetCurrentUserAsync(cancellationToken);

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                ArchiveGroup? group = await _context.Entry(user).Collection(u => u.ArchiveGroups).Query()
                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken);

                if (group is null)
                    return NotFound("У вас нет доступа к изменению этой группы или группы с таким Id не существует");

                _context.ArchiveGroups.Remove(group);

                await _context.SaveChangesAsync(cancellationToken);

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
