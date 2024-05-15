using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Validation.Tools;
using SteamStorageAPI.Utilities.Validation.Validators.ActiveGroups;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
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
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public ActiveGroupsController(
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

        public record ActiveGroupResponse(
            int Id,
            string Title,
            string? Description,
            string Colour,
            decimal? GoalSum,
            double? GoalSumCompletion,
            int Count,
            decimal BuySum,
            decimal CurrentSum,
            double Change,
            DateTime DateCreation);

        public record ActiveGroupsResponse(
            int Count,
            IEnumerable<ActiveGroupResponse> ActiveGroups);
        
        public record ActiveGroupsGameCountResponse(
            string GameTitle,
            double Percentage,
            int Count);

        public record ActiveGroupsGameInvestmentSumResponse(
            string GameTitle,
            double Percentage,
            decimal InvestmentSum);
        
        public record ActiveGroupsGameCurrentSumResponse(
            string GameTitle,
            double Percentage,
            decimal CurrentSum);

        public record ActiveGroupsStatisticResponse(
            int ActivesCount,
            decimal InvestmentSum,
            decimal CurrentSum,
            IEnumerable<ActiveGroupsGameCountResponse> GameCount,
            IEnumerable<ActiveGroupsGameInvestmentSumResponse> GameInvestmentSum,
            IEnumerable<ActiveGroupsGameCurrentSumResponse> GameCurrentSum);
        
        public record ActiveGroupDynamicResponse(
            int Id,
            DateTime DateUpdate,
            decimal Sum);
        
        public record ActiveGroupDynamicStatsResponse(
            double ChangePeriod,
            IEnumerable<ActiveGroupDynamicResponse> Dynamic);

        public record ActiveGroupsCountResponse(
            int Count);
        
        [Validator<GetActiveGroupInfoRequestValidator>]
        public record GetActiveGroupInfoRequest(
            int GroupId);

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
        
        #region Methods

        private async Task<ActiveGroupResponse> GetActiveGroupResponseAsync(
            ActiveGroup group,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            return new(group.Id,
                group.Title,
                group.Description,
                $"#{group.Colour ?? ActiveGroup.BASE_ACTIVE_GROUP_COLOUR}",
                group.GoalSum,
                group.GoalSum == null
                    ? null
                    : (double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate /
                      (double)group.GoalSum,
                group.Actives.Sum(y => y.Count),
                group.Actives.Sum(y => y.BuyPrice * y.Count),
                (decimal)((double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate),
                group.Actives.Sum(y => y.BuyPrice) != 0
                    ? ((double)group.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate -
                       (double)group.Actives.Sum(y => y.BuyPrice * y.Count)) /
                      (double)group.Actives.Sum(y => y.BuyPrice * y.Count)
                    : 0,
                group.DateCreation);
        }
        
        private async Task<IEnumerable<ActiveGroupResponse>> GetActiveGroupsResponseAsync(
            IQueryable<ActiveGroup> groups,
            User user,
            CancellationToken cancellationToken = default)
        {
            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            List<ActiveGroupResponse> result = await groups
                .Select(x =>
                    new ActiveGroupResponse(
                        x.Id,
                        x.Title,
                        x.Description,
                        $"#{x.Colour ?? ActiveGroup.BASE_ACTIVE_GROUP_COLOUR}",
                        x.GoalSum,
                        x.GoalSum == null
                            ? null
                            : (double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate /
                              (double)x.GoalSum,
                        x.Actives.Sum(y => y.Count),
                        x.Actives.Sum(y => y.BuyPrice * y.Count),
                        (decimal)((double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate),
                        x.Actives.Sum(y => y.BuyPrice) != 0
                            ? ((double)x.Actives.Sum(y => y.Skin.CurrentPrice * y.Count) * currencyExchangeRate -
                               (double)x.Actives.Sum(y => y.BuyPrice * y.Count)) /
                              (double)x.Actives.Sum(y => y.BuyPrice * y.Count)
                            : 0,
                        x.DateCreation))
                .ToListAsync(cancellationToken);

            return result;
        }

        #endregion Methods

        #region GET

        /// <summary>
        /// Получение информации об одной группе активов
        /// </summary>
        /// <response code="200">Возвращает подробную информацию о группе активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы активов с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActiveGroupInfo")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupResponse>> GetActiveGroupInfo(
            [FromQuery] GetActiveGroupInfoRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ActiveGroup group = await _context.Entry(user)
                                    .Collection(x => x.ActiveGroups)
                                    .Query()
                                    .AsNoTracking()
                                    .Include(x => x.Actives)
                                    .ThenInclude(x => x.Skin)
                                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                    "Группы активов с таким Id не существует");

            return Ok(await GetActiveGroupResponseAsync(group, user, cancellationToken));
        }

        /// <summary>
        /// Получение списка групп активов
        /// </summary>
        /// <response code="200">Возвращает список групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActiveGroups")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupsResponse>> GetActiveGroups(
            [FromQuery] GetActiveGroupsRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<ActiveGroup> groups = _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin);

            IEnumerable<ActiveGroupResponse> groupsResponse =
                await GetActiveGroupsResponseAsync(groups, user, cancellationToken);

            if (request is { OrderName: not null, IsAscending: not null })
                switch (request.OrderName)
                {
                    case ActiveGroupOrderName.Title:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Title)
                            : groupsResponse.OrderByDescending(x => x.Title);
                        break;
                    case ActiveGroupOrderName.Count:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Count)
                            : groupsResponse.OrderByDescending(x => x.Count);
                        break;
                    case ActiveGroupOrderName.BuySum:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.BuySum)
                            : groupsResponse.OrderByDescending(x => x.BuySum);
                        break;
                    case ActiveGroupOrderName.CurrentSum:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.CurrentSum)
                            : groupsResponse.OrderByDescending(x => x.CurrentSum);
                        break;
                    case ActiveGroupOrderName.Change:
                        groupsResponse = request.IsAscending.Value
                            ? groupsResponse.OrderBy(x => x.Change)
                            : groupsResponse.OrderByDescending(x => x.Change);
                        break;
                }
            else
                groupsResponse = groupsResponse.OrderBy(x => x.Id);

            return Ok(new ActiveGroupsResponse(await groups.CountAsync(cancellationToken), groupsResponse));
        }

        /// <summary>
        /// Получение статистики групп активов
        /// </summary>
        /// <response code="200">Возвращает статистику групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActiveGroupsStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupsStatisticResponse>> GetActiveGroupsStatistic(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<ActiveGroup> groups = _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .ThenInclude(x => x.Game);

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            IQueryable<Active> actives = groups.SelectMany(x => x.Actives);
            
            List<Game> games = actives
                .Select(x => x.Skin.Game)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            int activesCount = actives.Sum(x => x.Count);
            decimal buyPriceSum = actives.Sum(x => x.BuyPrice * x.Count);
            decimal latestPriceSum = (decimal)((double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate);
            
            List<ActiveGroupsGameCountResponse> gamesCountResponse = [];
            gamesCountResponse.AddRange(
                games.Select(item =>
                    new ActiveGroupsGameCountResponse(
                        item.Title,
                        activesCount == 0
                            ? 0
                            : (double)actives.Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.Count) / activesCount,
                        actives.Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.Count)))
            );
            
            List<ActiveGroupsGameInvestmentSumResponse> gamesInvestmentSumResponse = [];
            gamesInvestmentSumResponse.AddRange(
                games.Select(item =>
                    new ActiveGroupsGameInvestmentSumResponse(
                        item.Title,
                        buyPriceSum == 0
                            ? 0
                            : (double)(actives
                                .Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.BuyPrice * x.Count) / buyPriceSum),
                        actives
                            .Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.BuyPrice * x.Count)))
            );
            
            List<ActiveGroupsGameCurrentSumResponse> gamesCurrentSumResponse = [];
            gamesCurrentSumResponse.AddRange(
                games.Select(item =>
                    new ActiveGroupsGameCurrentSumResponse(
                        item.Title,
                        latestPriceSum == 0
                            ? 0
                            : (double)actives
                                .Where(x => x.Skin.GameId == item.Id)
                                .Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate / (double)latestPriceSum,
                        (decimal)((double)actives
                            .Where(x => x.Skin.GameId == item.Id)
                            .Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate)))
            );


            return Ok(new ActiveGroupsStatisticResponse(
                activesCount,
                buyPriceSum,
                latestPriceSum,
                gamesCountResponse,
                gamesInvestmentSumResponse,
                gamesCurrentSumResponse));
        }

        /// <summary>
        /// Получение динамики стоимости группы активов
        /// </summary>
        /// <response code="200">Возвращает динамику группы активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Группы с таким Id не существует или пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActiveGroupDynamics")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupDynamicStatsResponse>> GetActiveGroupDynamics(
            [FromQuery] GetActiveGroupDynamicRequest request,
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            ActiveGroup group = await _context.Entry(user)
                                    .Collection(u => u.ActiveGroups)
                                    .Query()
                                    .AsNoTracking()
                                    .FirstOrDefaultAsync(x => x.Id == request.GroupId, cancellationToken) ??
                                throw new HttpResponseException(StatusCodes.Status404NotFound,
                                    "У вас нет доступа к информации о группе с таким Id или группы с таким Id не существует");

            DateTime startDate = request.StartDate.Date;

            DateTime endDate = request.EndDate.Date.AddHours(23).AddMinutes(59).AddSeconds(59);

            List<ActiveGroupDynamicResponse> dynamic = await _context.Entry(group)
                .Collection(s => s.ActiveGroupsDynamics)
                .Query()
                .AsNoTracking()
                .Where(x => x.DateUpdate >= startDate && x.DateUpdate <= endDate)
                .Select(x => new ActiveGroupDynamicResponse(x.Id, x.DateUpdate, x.Sum))
                .ToListAsync(cancellationToken);

            double changePeriod = (double)(dynamic.Count == 0 || dynamic.First().Sum == 0
                ? 0
                : (dynamic.Last().Sum - dynamic.First().Sum) / dynamic.First().Sum);

            return Ok(new ActiveGroupDynamicStatsResponse(changePeriod, dynamic));
        }

        /// <summary>
        /// Получение количества групп активов
        /// </summary>
        /// <response code="200">Возвращает количество групп активов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [Authorize]
        [HttpGet(Name = "GetActiveGroupsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveGroupsCountResponse>> GetActiveGroupsCount(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            return Ok(new ActiveGroupsCountResponse(await _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .CountAsync(cancellationToken)));
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
        [Authorize]
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
                GoalSum = request.GoalSum,
                DateCreation = DateTime.Now
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
        [Authorize]
        [HttpPut(Name = "PutActiveGroup")]
        public async Task<ActionResult> PutActiveGroup(
            PutActiveGroupRequest request,
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
        [Authorize]
        [HttpDelete(Name = "DeleteActiveGroup")]
        public async Task<ActionResult> DeleteActiveGroup(
            DeleteActiveGroupRequest request,
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
                                    "У вас нет доступа к изменению этой группы или группы с таким Id не существует");

            _context.ActiveGroups.Remove(group);

            await _context.SaveChangesAsync(cancellationToken);

            return Ok();
        }

        #endregion DELETE
    }
}
