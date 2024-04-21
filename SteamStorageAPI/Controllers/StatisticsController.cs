using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities.Exceptions;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class StatisticsController : ControllerBase
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public StatisticsController(
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

        public record InvestmentSumResponse(
            double TotalSum,
            double PercentageGrowth);

        public record FinancialGoalResponse(
            double FinancialGoal,
            double PercentageCompletion);

        public record ActiveStatisticResponse(
            int Count,
            double CurrentSum,
            double PercentageGrowth);

        public record ArchiveStatisticResponse(
            int Count,
            double SoldSum,
            double PercentageGrowth);

        public record InventoryStatisticResponse(
            int Count,
            double Sum,
            IEnumerable<InventoryGameStatisticResponse> Games);

        public record InventoryGameStatisticResponse(
            string GameTitle,
            double Percentage,
            int Count);

        public record ItemsCountResponse(
            int Count);

        #endregion Records

        #region GET

        /// <summary>
        /// Получение суммы инвестиций
        /// </summary>
        /// <remarks>
        /// Текущая сумма инвестиций рассчитывается по формуле: "Сумма текущей стоимости активов" + "Сумма продажи элементов архива".<br/> 
        /// Процент роста инвестиций рассчитывается по формуле: ("Текущая сумма" - ("Сумма покупки активов" + "Сумма покупки элементов архива")) / ("Сумма покупки активов" + "Сумма покупки элементов архива")
        /// </remarks>
        /// <response code="200">Возвращает сумму инвестиций</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetInvestmentSum")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InvestmentSumResponse>> GetInvestmentSum(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(u => u.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives);

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(u => u.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives);

            double investedSum =
                (double)(actives.Sum(y => y.BuyPrice * y.Count) + archives.Sum(y => y.BuyPrice * y.Count));

            double currentSum =
                (double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate +
                (double)archives.Sum(y => y.SoldPrice * y.Count);

            double percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

            return Ok(new InvestmentSumResponse(
                currentSum,
                percentage));
        }

        /// <summary>
        /// Получение информации о финансовой цели
        /// </summary>
        /// <remarks>
        /// Процент достижения финансовой целе рассчитывается по формуле: ("Сумма текущей стоимости активов" + "Сумма продажи элементов архива") / "Финансовая цель"
        /// </remarks>
        /// <response code="200">Возвращает информацию о финансовой цели</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetFinancialGoal")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<FinancialGoalResponse>> GetFinancialGoal(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            double financialGoal = (double)(user.GoalSum ?? 0);

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(u => u.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives);

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(u => u.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives);

            double currentSum =
                (double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate +
                (double)archives.Sum(y => y.SoldPrice * y.Count);

            double percentage = financialGoal == 0 ? 1 : currentSum / financialGoal;

            return Ok(new FinancialGoalResponse(
                financialGoal,
                percentage));
        }

        /// <summary>
        /// Получение информации об активах
        /// </summary>
        /// <remarks>
        /// Процент роста стоимости активов рассчитывается по формуле: ("Сумма текущей стоимости активов" - "Сумма покупки активов") / "Сумма покупки активов"
        /// </remarks>
        /// <response code="200">Возвращает информацию об активах</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetActiveStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ActiveStatisticResponse>> GetActiveStatistic(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(u => u.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .ThenInclude(x => x.Skin)
                .SelectMany(x => x.Actives);

            int count = actives.Sum(x => x.Count);

            double investedSum = (double)actives.Sum(y => y.BuyPrice * y.Count);

            double currentSum = (double)actives.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate;

            double percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

            return Ok(new ActiveStatisticResponse(
                count,
                currentSum,
                percentage));
        }

        /// <summary>
        /// Получение информации об архива
        /// </summary>
        /// <remarks>
        /// Процент роста стоимости элементов архива рассчитывается по формуле: ("Сумма продажи элементов арихва" - "Сумма покупки элементов архива") / "Сумма покупки элементов архива"
        /// </remarks>
        /// <response code="200">Возвращает информацию об архиве</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetArchiveStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ArchiveStatisticResponse>> GetArchiveStatistic(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(u => u.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives);

            int count = archives.Sum(x => x.Count);

            double investedSum = (double)archives.Sum(y => y.BuyPrice * y.Count);

            double soldSum = (double)archives.Sum(y => y.SoldPrice * y.Count);

            double percentage = investedSum == 0 ? 1 : (soldSum - investedSum) / investedSum;

            return Ok(new ArchiveStatisticResponse(
                count,
                soldSum,
                percentage));
        }

        /// <summary>
        /// Получение информации об инветаре
        /// </summary>
        /// <response code="200">Возвращает информацию об инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetInventoryStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<InventoryStatisticResponse>> GetInventoryStatistic(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            IQueryable<Inventory> inventories = _context.Entry(user)
                .Collection(u => u.Inventories)
                .Query()
                .AsNoTracking()
                .Include(x => x.Skin)
                .ThenInclude(x => x.Game)
                .AsQueryable();

            int count = inventories.Sum(x => x.Count);

            double sum = (double)inventories.Sum(x => x.Skin.CurrentPrice * x.Count) * currencyExchangeRate;

            List<Game> games = inventories.Select(x => x.Skin.Game)
                .GroupBy(x => x.Id)
                .Select(g => g.First())
                .ToList();

            List<InventoryGameStatisticResponse> gamesResponse = [];
            gamesResponse.AddRange(
                games.Select(item =>
                    new InventoryGameStatisticResponse(
                        item.Title,
                        (double)inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count) / count,
                        inventories.Where(x => x.Skin.GameId == item.Id).Sum(x => x.Count)))
            );

            return Ok(new InventoryStatisticResponse(count, sum, gamesResponse));
        }

        /// <summary>
        /// Получение общего количества предметов
        /// </summary>
        /// <remarks>
        /// Количество предметов рассчитывается по формуле: "Количество активов" + "Количество элементов архива" + "Количество предметов в инвентаре"
        /// </remarks>
        /// <response code="200">Возвращает общее количество предметов</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetItemsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult<ItemsCountResponse>> GetItemsCount(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(u => u.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .SelectMany(x => x.Actives);

            int activesCount = actives.Sum(x => x.Count);

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(u => u.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives);

            int archivesCount = archives.Sum(x => x.Count);

            IQueryable<Inventory> inventories = _context.Entry(user)
                .Collection(u => u.Inventories)
                .Query()
                .AsNoTracking();

            int inventoriesCount = inventories.Sum(x => x.Count);

            return Ok(new ItemsCountResponse(
                activesCount + archivesCount + inventoriesCount));
        }

        #endregion GET
    }
}
