﻿using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class StatisticsController : ControllerBase
    {
        #region Fields

        private readonly ILogger<StatisticsController> _logger;
        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public StatisticsController(ILogger<StatisticsController> logger, IUserService userService,
            SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
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
        [HttpGet(Name = "GetInvestmentSum")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<InvestmentSumResponse> GetInvestmentSum()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<Active> actives = _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin.SkinsDynamics)
                    .SelectMany(x => x.Actives)
                    .ToList();

                List<Archive> archives = _context.Entry(user)
                    .Collection(u => u.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .SelectMany(x => x.Archives)
                    .ToList();

                double investedSum =
                    (double)(actives.Sum(y => y.BuyPrice * y.Count) + archives.Sum(y => y.BuyPrice * y.Count));

                double currentSum = (double)
                (
                    actives.Sum(y =>
                        (y.Skin.SkinsDynamics.Count == 0
                            ? 0
                            : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count)
                    + archives.Sum(y => y.SoldPrice * y.Count)
                );

                double percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

                return Ok(new InvestmentSumResponse(currentSum, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpGet(Name = "GetFinancialGoal")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<FinancialGoalResponse> GetFinancialGoal()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                double financialGoal = (double)(user.GoalSum ?? 0);

                List<Active> actives = _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin.SkinsDynamics)
                    .SelectMany(x => x.Actives)
                    .ToList();

                List<Archive> archives = _context.Entry(user)
                    .Collection(u => u.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .SelectMany(x => x.Archives)
                    .ToList();

                double currentSum = (double)
                (
                    actives.Sum(y =>
                        (y.Skin.SkinsDynamics.Count == 0
                            ? 0
                            : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count)
                    + archives.Sum(y => y.SoldPrice * y.Count)
                );

                double percentage = financialGoal == 0 ? 1 : currentSum / financialGoal;

                return Ok(new FinancialGoalResponse(financialGoal, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpGet(Name = "GetActiveStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<ActiveStatisticResponse> GetActiveStatistic()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<Active> actives = _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .ThenInclude(x => x.Skin.SkinsDynamics)
                    .SelectMany(x => x.Actives)
                    .ToList();

                int count = actives.Sum(x => x.Count);

                double investedSum = (double)actives.Sum(y => y.BuyPrice * y.Count);

                double currentSum = (double)actives.Sum(y =>
                    (y.Skin.SkinsDynamics.Count == 0
                        ? 0
                        : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count);

                double percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

                return Ok(new ActiveStatisticResponse(count, currentSum, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpGet(Name = "GetArchiveStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<ArchiveStatisticResponse> GetArchiveStatistic()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<Archive> archives = _context.Entry(user)
                    .Collection(u => u.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .SelectMany(x => x.Archives)
                    .ToList();

                int count = archives.Sum(x => x.Count);

                double investedSum = (double)archives.Sum(y => y.BuyPrice * y.Count);

                double soldSum = (double)archives.Sum(y => y.SoldPrice * y.Count);

                double percentage = investedSum == 0 ? 1 : (soldSum - investedSum) / investedSum;

                return Ok(new ArchiveStatisticResponse(count, soldSum, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Получение информации об инветаре
        /// </summary>
        /// <response code="200">Возвращает информацию об инвентаре</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="404">Пользователь не найден</response>
        [HttpGet(Name = "GetInventoryStatistic")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<InventoryStatisticResponse> GetInventoryStatistic()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<Inventory> inventories = _context.Entry(user)
                    .Collection(u => u.Inventories)
                    .Query()
                    .Include(x => x.Skin.SkinsDynamics)
                    .Include(x => x.Skin.Game)
                    .ToList();

                int count = inventories.Sum(x => x.Count);


                double sum = (double)inventories.Sum(y =>
                    (y.Skin.SkinsDynamics.Count == 0
                        ? 0
                        : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count);

                List<Game> games = inventories.Select(x => x.Skin.Game)
                    .Distinct()
                    .ToList();

                List<InventoryGameStatisticResponse> gamesResponse = [];
                gamesResponse.AddRange(games.Select(item => new InventoryGameStatisticResponse(item.Title,
                    (double)inventories.Count(x => x.Skin.Game.Id == item.Id) / count,
                    inventories.Count(x => x.Skin.Game.Id == item.Id))));

                return Ok(new InventoryStatisticResponse(count, sum, gamesResponse));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
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
        [HttpGet(Name = "GetItemsCount")]
        [Produces(MediaTypeNames.Application.Json)]
        public ActionResult<ItemsCountResponse> GetItemsCount()
        {
            try
            {
                User? user = _userService.GetCurrentUser();

                if (user is null)
                    return NotFound("Пользователя с таким Id не существует");

                List<Active> actives = _context.Entry(user)
                    .Collection(u => u.ActiveGroups)
                    .Query()
                    .Include(x => x.Actives)
                    .SelectMany(x => x.Actives)
                    .ToList();

                int activesCount = actives.Sum(x => x.Count);

                List<Archive> archives = _context.Entry(user)
                    .Collection(u => u.ArchiveGroups)
                    .Query()
                    .Include(x => x.Archives)
                    .SelectMany(x => x.Archives)
                    .ToList();

                int archivesCount = archives.Sum(x => x.Count);

                List<Inventory> inventories = _context.Entry(user)
                    .Collection(u => u.Inventories)
                    .Query()
                    .ToList();

                int inventoriesCount = inventories.Sum(x => x.Count);

                return Ok(new ItemsCountResponse(activesCount + archivesCount + inventoriesCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        #endregion GET
    }
}
