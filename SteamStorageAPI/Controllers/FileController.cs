using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.CurrencyService;
using SteamStorageAPI.Services.UserService;
using SteamStorageAPI.Utilities;
using SteamStorageAPI.Utilities.Exceptions;
using SteamStorageAPI.Utilities.Steam;
// ReSharper disable NotAccessedPositionalProperty.Global

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]/[action]")]
    public class FileController : ControllerBase
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly ICurrencyService _currencyService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public FileController(
            IUserService userService,
            ICurrencyService currencyService,
            SteamStorageContext context)
        {
            _userService = userService;
            _currencyService = currencyService;
            _context = context;
        }

        #endregion Constructor

        #region GET

        /// <summary>
        /// Получение информации об инвестициях в Excel файле
        /// </summary>
        /// <response code="200">Возвращает информации об инвестициях в Excel файле</response>
        /// <response code="400">Ошибка во время выполнения метода (см. описание)</response>
        /// <response code="401">Пользователь не прошёл авторизацию</response>
        /// <response code="499">Операция отменена</response>
        [HttpGet(Name = "GetExcelFile")]
        [Produces(MediaTypeNames.Application.Octet)]
        public async Task<ActionResult> GetExcelFile(
            CancellationToken cancellationToken = default)
        {
            User user = await _userService.GetCurrentUserAsync(cancellationToken) ??
                        throw new HttpResponseException(StatusCodes.Status404NotFound,
                            "Пользователя с таким Id не существует");

            IQueryable<Active> actives = _context.Entry(user)
                .Collection(x => x.ActiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Actives)
                .SelectMany(x => x.Actives)
                .Include(x => x.Skin)
                .ThenInclude(x => x.Game)
                .AsQueryable();

            IQueryable<Archive> archives = _context.Entry(user)
                .Collection(x => x.ArchiveGroups)
                .Query()
                .AsNoTracking()
                .Include(x => x.Archives)
                .SelectMany(x => x.Archives)
                .Include(x => x.Skin)
                .ThenInclude(x => x.Game)
                .AsQueryable();

            double currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

            using ExcelPackage package = new();

            ExcelWorksheet? activesWorksheet = package.Workbook.Worksheets.Add("Активы");

            activesWorksheet.Cells["A1"].Value = "Название";
            activesWorksheet.Cells["B1"].Value = "Количество";
            activesWorksheet.Cells["C1"].Value = "Цена покупки";
            activesWorksheet.Cells["D1"].Value = "Сумма покупки";
            activesWorksheet.Cells["E1"].Value = "Дата покупки";
            activesWorksheet.Cells["F1"].Value = "Текущая цена";
            activesWorksheet.Cells["G1"].Value = "Текущая сумма";
            activesWorksheet.Cells["H1"].Value = "Изменение (%)";
            activesWorksheet.Cells["I1"].Value = "Ссылка";
            activesWorksheet.Cells["A1:I1"].Style.Font.Bold = true;
            activesWorksheet.Cells["A1:I1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            activesWorksheet.Cells["A1:I1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int i = 2;
            foreach (Active active in actives)
            {
                activesWorksheet.Cells[i, 1].Value = active.Skin.Title;
                activesWorksheet.Cells[i, 2].Value = active.Count;
                activesWorksheet.Cells[i, 3].Value = active.BuyPrice;
                activesWorksheet.Cells[i, 4].Value = active.BuyPrice * active.Count;
                activesWorksheet.Cells[i, 5].Value = active.BuyDate.ToString(ProgramConstants.DATE_FORMAT);
                activesWorksheet.Cells[i, 6].Value = (double)active.Skin.CurrentPrice * currencyExchangeRate;
                activesWorksheet.Cells[i, 7].Value =
                    (double)active.Skin.CurrentPrice * active.Count * currencyExchangeRate;
                activesWorksheet.Cells[i, 8].Value =
                    $"{((double)active.Skin.CurrentPrice * currencyExchangeRate - (double)active.BuyPrice) / (double)active.BuyPrice * 100:N2}%";
                activesWorksheet.Cells[i, 9].Value =
                    SteamApi.GetSkinMarketUrl(active.Skin.Game.SteamGameId, active.Skin.MarketHashName);
                i++;
            }

            activesWorksheet.Cells[i, 1, i + 1, 1].Merge = true;
            activesWorksheet.Cells[i, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            activesWorksheet.Cells[i, 1].Value = "Итого";
            activesWorksheet.Cells[i, 2].Value = "Общее количество";
            activesWorksheet.Cells[i, 3].Value = "Средняя цена покупки";
            activesWorksheet.Cells[i, 4].Value = "Общая сумма покупки";
            activesWorksheet.Cells[i, 6].Value = "Средняя текущая цена";
            activesWorksheet.Cells[i, 7].Value = "Общая текущая стоимость";
            activesWorksheet.Cells[i, 8].Value = "Общее изменение";
            activesWorksheet.Cells[i, 2, i, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int activesTotalCount = await actives.SumAsync(x => x.Count, cancellationToken);
            decimal activesAverageBuyPrice = activesTotalCount == 0
                ? 0
                : await actives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken) / activesTotalCount;
            decimal activesBuySum = await actives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken);
            decimal activesAverageCurrentPrice = activesTotalCount == 0
                ? 0
                : (decimal)((double)await actives.SumAsync(x => x.Skin.CurrentPrice * x.Count, cancellationToken) *
                            currencyExchangeRate) / activesTotalCount;
            decimal activesCurrentSum =
                (decimal)((double)await actives.SumAsync(x => x.Skin.CurrentPrice * x.Count, cancellationToken) *
                          currencyExchangeRate);
            decimal activesTotalChange = activesBuySum == 0 ? 1 : (activesCurrentSum - activesBuySum) / activesBuySum;

            activesWorksheet.Cells[i + 1, 2].Value = activesTotalCount;
            activesWorksheet.Cells[i + 1, 3].Value = Math.Round(activesAverageBuyPrice, 2);
            activesWorksheet.Cells[i + 1, 4].Value = Math.Round(activesBuySum, 2);
            activesWorksheet.Cells[i + 1, 6].Value = Math.Round(activesAverageCurrentPrice, 2);
            activesWorksheet.Cells[i + 1, 7].Value = Math.Round(activesCurrentSum, 2);
            activesWorksheet.Cells[i + 1, 8].Value = $"{activesTotalChange * 100:N2}%";

            activesWorksheet.Cells[i, 1, i + 1, 8].Style.Font.Bold = true;
            activesWorksheet.Cells[i, 1, i + 1, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            activesWorksheet.Cells[1, 1, i + 1, 8].AutoFitColumns();

            ExcelWorksheet? archiveWorksheet = package.Workbook.Worksheets.Add("Архив");

            archiveWorksheet.Cells["A1"].Value = "Название";
            archiveWorksheet.Cells["B1"].Value = "Количество";
            archiveWorksheet.Cells["C1"].Value = "Цена покупки";
            archiveWorksheet.Cells["D1"].Value = "Сумма покупки";
            archiveWorksheet.Cells["E1"].Value = "Дата покупки";
            archiveWorksheet.Cells["F1"].Value = "Цена продажи";
            archiveWorksheet.Cells["G1"].Value = "Сумма продажи";
            archiveWorksheet.Cells["H1"].Value = "Дата продажи";
            archiveWorksheet.Cells["I1"].Value = "Изменение (%)";
            archiveWorksheet.Cells["J1"].Value = "Ссылка";
            archiveWorksheet.Cells["A1:J1"].Style.Font.Bold = true;
            archiveWorksheet.Cells["A1:J1"].Style.Border.BorderAround(ExcelBorderStyle.Thin);
            archiveWorksheet.Cells["A1:J1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int j = 2;
            foreach (Archive archive in archives)
            {
                archiveWorksheet.Cells[j, 1].Value = archive.Skin.Title;
                archiveWorksheet.Cells[j, 2].Value = archive.Count;
                archiveWorksheet.Cells[j, 3].Value = archive.BuyPrice;
                archiveWorksheet.Cells[j, 4].Value = archive.BuyPrice * archive.Count;
                archiveWorksheet.Cells[j, 5].Value = archive.BuyDate.ToString(ProgramConstants.DATE_FORMAT);
                archiveWorksheet.Cells[j, 6].Value = archive.SoldPrice;
                archiveWorksheet.Cells[j, 7].Value = archive.SoldPrice * archive.Count;
                archiveWorksheet.Cells[j, 8].Value = archive.SoldDate.ToString(ProgramConstants.DATE_FORMAT);
                archiveWorksheet.Cells[j, 9].Value =
                    $"{(archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice * 100:N2}%";
                archiveWorksheet.Cells[j, 10].Value =
                    SteamApi.GetSkinMarketUrl(archive.Skin.Game.SteamGameId, archive.Skin.MarketHashName);
                j++;
            }

            archiveWorksheet.Cells[j, 1, j + 1, 1].Merge = true;
            archiveWorksheet.Cells[j, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
            archiveWorksheet.Cells[j, 1].Value = "Итого";
            archiveWorksheet.Cells[j, 2].Value = "Общее количество";
            archiveWorksheet.Cells[j, 3].Value = "Средняя цена покупки";
            archiveWorksheet.Cells[j, 4].Value = "Общая сумма покупки";
            archiveWorksheet.Cells[j, 6].Value = "Средняя цена продажи";
            archiveWorksheet.Cells[j, 7].Value = "Общая сумма продажи";
            archiveWorksheet.Cells[j, 9].Value = "Общее изменение";
            archiveWorksheet.Cells[j, 2, j, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            int archivesTotalCount = await archives.SumAsync(x => x.Count, cancellationToken);
            decimal archivesAverageBuyPrice = archivesTotalCount == 0
                ? 0
                : await archives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken) / archivesTotalCount;
            decimal archivesBuySum = await archives.SumAsync(x => x.BuyPrice * x.Count, cancellationToken);
            decimal archivesAverageSoldPrice = archivesTotalCount == 0
                ? 0
                : await archives.SumAsync(x => x.SoldPrice * x.Count, cancellationToken) / archivesTotalCount;
            decimal archivesSoldSum = await archives.SumAsync(x => x.SoldPrice * x.Count, cancellationToken);
            decimal archivesTotalChange = archivesBuySum == 0 ? 1 : (archivesSoldSum - archivesBuySum) / archivesBuySum;

            archiveWorksheet.Cells[j + 1, 2].Value = archivesTotalCount;
            archiveWorksheet.Cells[j + 1, 3].Value = Math.Round(archivesAverageBuyPrice, 2);
            archiveWorksheet.Cells[j + 1, 4].Value = Math.Round(archivesBuySum, 2);
            archiveWorksheet.Cells[j + 1, 6].Value = Math.Round(archivesAverageSoldPrice, 2);
            archiveWorksheet.Cells[j + 1, 7].Value = Math.Round(archivesSoldSum, 2);
            archiveWorksheet.Cells[j + 1, 9].Value = $"{archivesTotalChange * 100:N2}%";

            archiveWorksheet.Cells[j, 1, j + 1, 9].Style.Font.Bold = true;
            archiveWorksheet.Cells[j, 1, j + 1, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);

            archiveWorksheet.Cells[1, 1, j + 1, 9].AutoFitColumns();

            return File(await package.GetAsByteArrayAsync(cancellationToken),
                "application/octet-stream",
                $"{DateTime.Now:dd.MM.yyyy#hh.mm}.xlsx");
        }

        #endregion GET
    }
}
