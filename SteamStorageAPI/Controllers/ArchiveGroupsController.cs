using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ArchiveGroups;
// ReSharper disable NotAccessedPositionalProperty.Global

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
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public ArchiveGroupsController(
            IUserService userService,
            SteamStorageContext context)
        {
            _userService = userService;
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
        
        public record ArchiveGroupsGameCountResponse(
            string GameTitle,
            double Percentage,
            int Count);

        public record ArchiveGroupsGameBuySumResponse(
            string GameTitle,
            double Percentage,
            decimal BuySum);
        
        public record ArchiveGroupsGameSoldSumResponse(
            string GameTitle,
            double Percentage,
            decimal SoldSum);

        public record ArchiveGroupsStatisticResponse(
            int ArchivesCount,
            decimal BuySum,
            decimal SoldSum,
            IEnumerable<ArchiveGroupsGameCountResponse> GameCount,
            IEnumerable<ArchiveGroupsGameBuySumResponse> GameBuySum,
            IEnumerable<ArchiveGroupsGameSoldSumResponse> GameSoldSum);

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
            CancellationToken cancellationToken = default)
        {
            groups = groups.AsNoTracking().Include(x => x.Archives);

            List<ArchiveGroupResponse> result = await groups.Select(x =>
                    new ArchiveGroupResponse(
                        x.Id,
                        x.Title,
                        x.Description,
                        $"#{x.Colour ?? ArchiveGroup.BASE_ARCHIVE_GROUP_COLOUR}",
                        x.Archives.Sum(y => y.Count),
                        x.Archives.Sum(y => y.BuyPrice * y.Count),
                        x.Archives.Sum(y => y.SoldPrice * y.Count),
                        x.Archives.Sum(y => y.BuyPrice) != 0
                            ? ((double)x.Archives.Sum(y => y.SoldPrice * y.Count) -
                               (double)x.Archives.Sum(y => y.BuyPrice * y.Count)) /
                              (double)x.Archives.Sum(y => y.BuyPrice * y.Count)
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

            IEnumerable<ArchiveGroupResponse> groupsResponse =
                await GetArchiveGroupsResponsesAsync(groups, cancellationToken);

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ArchiveGroupOrderName.Title:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Title)
                            : groupsResponse.OrderByDescending(x => x.Title);
                        break;
                    case ArchiveGroupOrderName.Count:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Count)
                            : groupsResponse.OrderByDescending(x => x.Count);
                        break;
                    case ArchiveGroupOrderName.BuySum:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.BuySum)
                            : groupsResponse.OrderByDescending(x => x.BuySum);
                        break;
                    case ArchiveGroupOrderName.SoldSum:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.SoldSum)
                            : groupsResponse.OrderByDescending(x => x.SoldSum);
                        break;
                    case ArchiveGroupOrderName.Change:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Change)
                            : groupsResponse.OrderByDescending(x => x.Change);
                        break;
                }
            else
                groupsResponse = groupsResponse.OrderBy(x => x.Id);

            return Ok(new ArchiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
        }

        /// <summary>
        /// Получение статистики групп архива
        /// </summary>
        /// <response code="200">Возвращает статистику групп архива</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchiveGroupsStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveGroupsStatisticResponse>> GetArchiveGroupsStatistic(
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
                .ThenInclude(x => x.Game)
                .SelectMany(x => x.Archives);
            
            List<Game> games = archives
                .Select(x => x.Skin.Game)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            int archivesCount = archives.Sum(x => x.Count);
            decimal buySum = archives.Sum(x => x.BuyPrice * x.Count);
            decimal soldSum = archives.Sum(x => x.SoldPrice * x.Count);
            
            List<ArchiveGroupsGameCountResponse> gamesCountResponse = [];
            gamesCountResponse.AddRange(
                games.Select(item =>
                    new ArchiveGroupsGameCountResponse(
                        item.Title,
                        archivesCount == 0
                            ? 0
                            : (double)archives.Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.Count) / archivesCount,
                        archives.Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.Count)))
            );
            
            List<ArchiveGroupsGameBuySumResponse> gamesBuySumResponse = [];
            gamesBuySumResponse.AddRange(
                games.Select(item =>
                    new ArchiveGroupsGameBuySumResponse(
                        item.Title,
                        buySum == 0
                            ? 0
                            : (double)(archives
                                .Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.BuyPrice * x.Count) / buySum),
                        archives
                            .Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.BuyPrice * x.Count)))
            );
            
            List<ArchiveGroupsGameSoldSumResponse> gamesSoldSumResponse = [];
            gamesSoldSumResponse.AddRange(
                games.Select(item =>
                    new ArchiveGroupsGameSoldSumResponse(
                        item.Title,
                        soldSum == 0
                            ? 0
                            : (double)(archives
                                .Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.SoldPrice * x.Count) / soldSum),
                        archives
                            .Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.SoldPrice * x.Count)))
            );

            return Ok(new ArchiveGroupsStatisticResponse(
                archivesCount,
                buySum,
                soldSum,
                gamesCountResponse,
                gamesBuySumResponse,
                gamesSoldSumResponse));
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

            ArchiveGroup group = await _context.Entry(user)
                                     .Collection(u => u.ArchiveGroups)
                                     .Query()
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

            ArchiveGroup group = await _context.Entry(user)
                                     .Collection(u => u.ArchiveGroups)
                                     .Query()
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
