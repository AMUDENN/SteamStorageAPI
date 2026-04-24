using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using SteamStorageAPI.Models.DBEntities;
using SteamStorageAPI.Services.Domain.CurrencyService;
using SteamStorageAPI.Services.Infrastructure.SteamApiUrlBuilder;
using SteamStorageAPI.Utilities.Config;

namespace SteamStorageAPI.Services.Domain.FileService;

public class FileService : IFileService
{
    #region Fields

    private readonly ISteamApiUrlBuilder _steamApiUrlBuilder;
    private readonly ICurrencyService _currencyService;
    private readonly SteamStorageContext _context;
    private readonly string _dateFormat;

    #endregion Fields

    #region Constructor

    public FileService(
        ISteamApiUrlBuilder steamApiUrlBuilder,
        ICurrencyService currencyService,
        SteamStorageContext context,
        AppConfig appConfig)
    {
        _steamApiUrlBuilder = steamApiUrlBuilder;
        _currencyService = currencyService;
        _context = context;
        _dateFormat = appConfig.App.DateFormat;
    }

    #endregion Constructor

    #region Methods

    public async Task<byte[]> GetExcelFileAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        List<Active> actives = await _context.Entry(user)
            .Collection(x => x.ActiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Actives).ThenInclude(x => x.Skin).ThenInclude(x => x.Game)
            .SelectMany(x => x.Actives)
            .ToListAsync(cancellationToken);

        List<Archive> archives = await _context.Entry(user)
            .Collection(x => x.ArchiveGroups)
            .Query()
            .AsNoTracking()
            .Include(x => x.Archives).ThenInclude(x => x.Skin).ThenInclude(x => x.Game)
            .SelectMany(x => x.Archives)
            .ToListAsync(cancellationToken);

        decimal currencyExchangeRate = await _currencyService.GetCurrencyExchangeRateAsync(user, cancellationToken);

        using ExcelPackage package = new();

        ExcelWorksheet? activesWorksheet = package.Workbook.Worksheets.Add("Actives");

        activesWorksheet.Cells["A1"].Value = "Name";
        activesWorksheet.Cells["B1"].Value = "Quantity";
        activesWorksheet.Cells["C1"].Value = "Buy Price";
        activesWorksheet.Cells["D1"].Value = "Buy Sum";
        activesWorksheet.Cells["E1"].Value = "Buy Date";
        activesWorksheet.Cells["F1"].Value = "Current Price";
        activesWorksheet.Cells["G1"].Value = "Current Sum";
        activesWorksheet.Cells["H1"].Value = "Change (%)";
        activesWorksheet.Cells["I1"].Value = "Link";
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
            activesWorksheet.Cells[i, 5].Value = active.BuyDate.ToString(_dateFormat);
            activesWorksheet.Cells[i, 6].Value = active.Skin.CurrentPrice * currencyExchangeRate;
            activesWorksheet.Cells[i, 7].Value = active.Skin.CurrentPrice * active.Count * currencyExchangeRate;
            activesWorksheet.Cells[i, 8].Value =
                active.BuyPrice == 0 ? "N/A" : $"{(active.Skin.CurrentPrice * currencyExchangeRate - active.BuyPrice) / active.BuyPrice * 100:N2}%";
            activesWorksheet.Cells[i, 9].Value =
                _steamApiUrlBuilder.GetSkinMarketUrl(active.Skin.Game.SteamGameId, active.Skin.MarketHashName);
            i++;
        }

        activesWorksheet.Cells[i, 1, i + 1, 1].Merge = true;
        activesWorksheet.Cells[i, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        activesWorksheet.Cells[i, 1].Value = "Total";
        activesWorksheet.Cells[i, 2].Value = "Total Quantity";
        activesWorksheet.Cells[i, 3].Value = "Average Buy Price";
        activesWorksheet.Cells[i, 4].Value = "Total Buy Sum";
        activesWorksheet.Cells[i, 6].Value = "Average Current Price";
        activesWorksheet.Cells[i, 7].Value = "Total Current Value";
        activesWorksheet.Cells[i, 8].Value = "Overall Change";
        activesWorksheet.Cells[i, 2, i, 8].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        int activesTotalCount = actives.Sum(x => x.Count);
        decimal activesBuySum = actives.Sum(x => x.BuyPrice * x.Count);
        decimal activesCurrentPriceSum = actives.Sum(x => x.Skin.CurrentPrice * x.Count);

        decimal activesAverageBuyPrice = activesTotalCount == 0 ? 0 : activesBuySum / activesTotalCount;
        decimal activesAverageCurrentPrice = activesTotalCount == 0
            ? 0
            : activesCurrentPriceSum * currencyExchangeRate / activesTotalCount;
        decimal activesCurrentSum = activesCurrentPriceSum * currencyExchangeRate;
        decimal activesTotalChange = activesBuySum == 0 ? 0 : (activesCurrentSum - activesBuySum) / activesBuySum;

        activesWorksheet.Cells[i + 1, 2].Value = activesTotalCount;
        activesWorksheet.Cells[i + 1, 3].Value = Math.Round(activesAverageBuyPrice, 2);
        activesWorksheet.Cells[i + 1, 4].Value = Math.Round(activesBuySum, 2);
        activesWorksheet.Cells[i + 1, 6].Value = Math.Round(activesAverageCurrentPrice, 2);
        activesWorksheet.Cells[i + 1, 7].Value = Math.Round(activesCurrentSum, 2);
        activesWorksheet.Cells[i + 1, 8].Value = $"{activesTotalChange * 100:N2}%";

        activesWorksheet.Cells[i, 1, i + 1, 8].Style.Font.Bold = true;
        activesWorksheet.Cells[i, 1, i + 1, 8].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        activesWorksheet.Cells[1, 1, i + 1, 8].AutoFitColumns();

        ExcelWorksheet? archiveWorksheet = package.Workbook.Worksheets.Add("Archive");

        archiveWorksheet.Cells["A1"].Value = "Name";
        archiveWorksheet.Cells["B1"].Value = "Quantity";
        archiveWorksheet.Cells["C1"].Value = "Buy Price";
        archiveWorksheet.Cells["D1"].Value = "Buy Sum";
        archiveWorksheet.Cells["E1"].Value = "Buy Date";
        archiveWorksheet.Cells["F1"].Value = "Sell Price";
        archiveWorksheet.Cells["G1"].Value = "Sell Sum";
        archiveWorksheet.Cells["H1"].Value = "Sell Date";
        archiveWorksheet.Cells["I1"].Value = "Change (%)";
        archiveWorksheet.Cells["J1"].Value = "Link";
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
            archiveWorksheet.Cells[j, 5].Value = archive.BuyDate.ToString(_dateFormat);
            archiveWorksheet.Cells[j, 6].Value = archive.SoldPrice;
            archiveWorksheet.Cells[j, 7].Value = archive.SoldPrice * archive.Count;
            archiveWorksheet.Cells[j, 8].Value = archive.SoldDate.ToString(_dateFormat);
            archiveWorksheet.Cells[j, 9].Value =
                archive.BuyPrice == 0 ? "N/A" : $"{(archive.SoldPrice - archive.BuyPrice) / archive.BuyPrice * 100:N2}%";
            archiveWorksheet.Cells[j, 10].Value =
                _steamApiUrlBuilder.GetSkinMarketUrl(archive.Skin.Game.SteamGameId, archive.Skin.MarketHashName);
            j++;
        }

        archiveWorksheet.Cells[j, 1, j + 1, 1].Merge = true;
        archiveWorksheet.Cells[j, 1].Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        archiveWorksheet.Cells[j, 1].Value = "Total";
        archiveWorksheet.Cells[j, 2].Value = "Total Quantity";
        archiveWorksheet.Cells[j, 3].Value = "Average Buy Price";
        archiveWorksheet.Cells[j, 4].Value = "Total Buy Sum";
        archiveWorksheet.Cells[j, 6].Value = "Average Sell Price";
        archiveWorksheet.Cells[j, 7].Value = "Total Sell Sum";
        archiveWorksheet.Cells[j, 9].Value = "Overall Change";
        archiveWorksheet.Cells[j, 2, j, 9].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        int archivesTotalCount = archives.Sum(x => x.Count);
        decimal archivesBuySum = archives.Sum(x => x.BuyPrice * x.Count);
        decimal archivesSoldSum = archives.Sum(x => x.SoldPrice * x.Count);

        decimal archivesAverageBuyPrice = archivesTotalCount == 0 ? 0 : archivesBuySum / archivesTotalCount;
        decimal archivesAverageSoldPrice = archivesTotalCount == 0 ? 0 : archivesSoldSum / archivesTotalCount;
        decimal archivesTotalChange =
            archivesBuySum == 0 ? 1 : (archivesSoldSum - archivesBuySum) / archivesBuySum;

        archiveWorksheet.Cells[j + 1, 2].Value = archivesTotalCount;
        archiveWorksheet.Cells[j + 1, 3].Value = Math.Round(archivesAverageBuyPrice, 2);
        archiveWorksheet.Cells[j + 1, 4].Value = Math.Round(archivesBuySum, 2);
        archiveWorksheet.Cells[j + 1, 6].Value = Math.Round(archivesAverageSoldPrice, 2);
        archiveWorksheet.Cells[j + 1, 7].Value = Math.Round(archivesSoldSum, 2);
        archiveWorksheet.Cells[j + 1, 9].Value = $"{archivesTotalChange * 100:N2}%";

        archiveWorksheet.Cells[j, 1, j + 1, 9].Style.Font.Bold = true;
        archiveWorksheet.Cells[j, 1, j + 1, 9].Style.Border.BorderAround(ExcelBorderStyle.Thin);
        archiveWorksheet.Cells[1, 1, j + 1, 9].AutoFitColumns();

        return await package.GetAsByteArrayAsync(cancellationToken);
    }

    #endregion Methods
}