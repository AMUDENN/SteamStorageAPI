using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;
using System.Linq;
using static SteamStorageAPI.Controllers.RolesController;

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
        public StatisticsController(ILogger<StatisticsController> logger, IUserService userService, SteamStorageContext context)
        {
            _logger = logger;
            _userService = userService;
            _context = context;
        }
        #endregion Constructor

        #region Records
        public record InvestmentSumResponse(double TotalSum, double PercentageGrowth);
        public record FinancialGoalResponse(double FinancialGoal, double PercentageCompletion);
        public record ActiveStatisticResponse(int Count, double CurrentSum, double PercentageGrowth);
        public record ArchiveStatisticResponse(int Count, double SoldSum, double PercentageGrowth);
        public record InventoryStatisticResponse(int Count, double Sum, IEnumerable<InventoryGameStatisticResponse> Games);
        public record InventoryGameStatisticResponse(string GameTitle, double Percentage);
        #endregion Records

        #region GET
        [HttpGet(Name = "GetInvestmentSum")]
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

                double investedSum = (double)(actives.Sum(y => y.BuyPrice * y.Count) + archives.Sum(y => y.BuyPrice * y.Count));

                double currentSum = (double)
                (
                    actives.Sum(y => (y.Skin.SkinsDynamics.Count == 0 ? 0 : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count)
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

        [HttpGet(Name = "GetFinancialGoal")]
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
                    actives.Sum(y => (y.Skin.SkinsDynamics.Count == 0 ? 0 : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count)
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

        [HttpGet(Name = "GetActiveStatistic")]
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

                double currentSum = (double)actives.Sum(y => (y.Skin.SkinsDynamics.Count == 0 ? 0 : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count);

                double percentage = investedSum == 0 ? 1 : (currentSum - investedSum) / investedSum;

                return Ok(new ActiveStatisticResponse(count, currentSum, percentage));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return BadRequest(ex.Message);
            }
        }

        [HttpGet(Name = "GetArchiveStatistic")]
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

        [HttpGet(Name = "GetInventoryStatistic")]
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


                double sum = (double)inventories.Sum(y => (y.Skin.SkinsDynamics.Count == 0 ? 0 : y.Skin.SkinsDynamics.OrderBy(x => x.DateUpdate).Last().Price) * y.Count);

                List<Game> games = inventories.Select(x => x.Skin.Game)
                                            .Distinct()
                                            .ToList();

                List<InventoryGameStatisticResponse> gamesResponse = [];
                foreach (var item in games)
                {
                    gamesResponse.Add(new InventoryGameStatisticResponse(item.Title,
                        (double)inventories.Count(x => x.Skin.Game.Id == item.Id) / count));
                }

                return Ok(new InventoryStatisticResponse(count, sum, gamesResponse));
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
