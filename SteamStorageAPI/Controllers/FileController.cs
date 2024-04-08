using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OfficeOpenXml;
using SteamStorageAPI.DBEntities;
using SteamStorageAPI.Services.UserService;

namespace SteamStorageAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]/[action]")]
    public class FileController : ControllerBase
    {
        #region Fields

        private readonly IUserService _userService;
        private readonly SteamStorageContext _context;

        #endregion Fields

        #region Constructor

        public FileController(
            IUserService userService,
            SteamStorageContext context)
        {
            _userService = userService;
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
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<ActionResult> GetExcelFile(
            CancellationToken cancellationToken = default)
        {
            using ExcelPackage package = new();

            ExcelWorksheet? activesWorksheet = package.Workbook.Worksheets.Add("Активы");

            activesWorksheet.Cells["A1"].Value = "Название";
            activesWorksheet.Cells["B1"].Value = "Количество";
            activesWorksheet.Cells["C1"].Value = "Цена покупки";
            activesWorksheet.Cells["D1"].Value = "Сумма";
            activesWorksheet.Cells["E1"].Value = "Дата покупки";
            activesWorksheet.Cells["F1"].Value = "Текущая цена";
            activesWorksheet.Cells["G1"].Value = "Дата обновления";
            activesWorksheet.Cells["H1"].Value = "Изменение (%)";
            activesWorksheet.Cells["I1"].Value = "Ссылка";
            activesWorksheet.Cells["A1:I1"].Style.Font.Bold = true;
            activesWorksheet.Cells["A1:I1"].Style.Border.BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);


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
            archiveWorksheet.Cells["A1:J1"].Style.Border
                .BorderAround(OfficeOpenXml.Style.ExcelBorderStyle.Thin);

            return File(await package.GetAsByteArrayAsync(cancellationToken), 
                "application/octet-stream",
                $"{DateTime.Now:dd.MM.yyyy#hh:mm}.xlsx");
        }

        #endregion GET
    }
}
