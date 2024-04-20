using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
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
    [Route("api/[controller]/[action]")]
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
            activesWorksheet.Cells["D1"].Value = "Сумма";
            activesWorksheet.Cells["E1"].Value = "Дата покупки";
            activesWorksheet.Cells["F1"].Value = "Текущая цена";
            activesWorksheet.Cells["G1"].Value = "Изменение (%)";
            activesWorksheet.Cells["H1"].Value = "Ссылка";
            activesWorksheet.Cells["A1:H1"].Style.Font.Bold = true;
            activesWorksheet.Cells["A1:H1"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

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
                    Math.Round(
                        ((double)active.Skin.CurrentPrice * currencyExchangeRate - (double)active.BuyPrice) /
                        (double)active.BuyPrice * 100, 2);
                activesWorksheet.Cells[i, 8].Value =
                    SteamApi.GetSkinMarketUrl(active.Skin.Game.SteamGameId, active.Skin.MarketHashName);
                i++;
            }

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
            archiveWorksheet.Cells["A1:J1"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

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
                    Math.Round((archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice * 100, 2);
                archiveWorksheet.Cells[j, 10].Value =
                    SteamApi.GetSkinMarketUrl(archive.Skin.Game.SteamGameId, archive.Skin.MarketHashName);
                j++;
            }

            //TODO: Statistics
            
            return File(await package.GetAsByteArrayAsync(cancellationToken),
                "application/octet-stream",
                $"{DateTime.Now:dd.MM.yyyy#hh.mm}.xlsx");
        }

        #endregion GET
    }
}
