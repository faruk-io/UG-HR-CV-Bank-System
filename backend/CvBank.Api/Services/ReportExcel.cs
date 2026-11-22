using System.Globalization;
using ClosedXML.Excel;
using CvBank.Api.Models;

namespace CvBank.Api.Services;

public static class ReportExcel
{
    private const string Brand = "Union Group CV Bank";

    public static byte[] Write(ReportData report, string generatedBy)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(SheetName(report.Title));

        var lastColumn = report.Columns.Count;
        var dataHeader = 6;
        var lastRow = dataHeader + report.Rows.Count;

        sheet.Cell(1, 1).Value = Brand;
        sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16).Font.SetFontColor(XLColor.FromHtml("#16233B"));
        sheet.Cell(2, 1).Value = report.Title;
        sheet.Cell(2, 1).Style.Font.SetBold().Font.SetFontSize(12);
        sheet.Cell(3, 1).Value = report.Description;
        sheet.Cell(4, 1).Value = $"Filters: {report.FilterSummary}";
        sheet.Cell(5, 1).Value = $"Generated {DateTime.Now:dd MMM yyyy HH:mm} by {generatedBy} · {report.Rows.Count} row(s)";

        foreach (var rowNumber in new[] { 1, 2, 3, 4, 5 })
        {
            sheet.Range(rowNumber, 1, rowNumber, lastColumn).Merge().Style
                .Font.SetItalic(rowNumber >= 3)
                .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
        }

        foreach (var rowNumber in new[] { 3, 4, 5 })
        {
            sheet.Range(rowNumber, 1, rowNumber, lastColumn).Style
                .Font.SetFontSize(9).Font.SetFontColor(XLColor.FromHtml("#4B5563"));
        }

        for (var column = 0; column < report.Columns.Count; column++)
        {
            var cell = sheet.Cell(dataHeader, column + 1);
            cell.Value = report.Columns[column].Label;
            cell.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#E8ECF3"));
            cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
        }

        sheet.Range(dataHeader, 1, dataHeader, lastColumn).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);

        for (var rowIndex = 0; rowIndex < report.Rows.Count; rowIndex++)
        {
            for (var column = 0; column < report.Columns.Count; column++)
            {
                WriteCell(sheet.Cell(dataHeader + 1 + rowIndex, column + 1), report.Rows[rowIndex][column], report.Columns[column].Type);
            }
        }

        sheet.Range(dataHeader, 1, lastRow, lastColumn).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        sheet.Range(dataHeader, 1, lastRow, lastColumn).SetAutoFilter();
        sheet.SheetView.FreezeRows(dataHeader);
        sheet.Columns().AdjustToContents(1, lastRow);

        foreach (var (column, index) in report.Columns.Select((value, i) => (value, i)))
        {
            var field = sheet.Column(index + 1);
            field.Width = column.Type switch
            {
                "text" => Math.Min(46, Math.Max(field.Width, 12)),
                "date" => 14,
                "datetime" => 24,
                _ => Math.Max(field.Width, 11)
            };
        }

        if (report.Truncated)
        {
            sheet.Cell(lastRow + 2, 1).Value =
                $"Only the first {Reports.RowLimit} rows are included. Narrow the filters for a complete export.";
            sheet.Cell(lastRow + 2, 1).Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#B00020"));
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static string FileName(ReportData report) =>
        $"{Slug(report.Title)}-{DateTime.Now:yyyyMMdd-HHmm}.xlsx";

    private static void WriteCell(IXLCell cell, object? value, string type)
    {
        switch (value)
        {
            case null:
                cell.Value = "—";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                return;

            case int or long:
                cell.Value = Convert.ToInt32(value);
                cell.Style.NumberFormat.SetNumberFormatId(0);
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                return;

            case decimal amount when type == "money":
                cell.Value = amount;
                cell.Style.NumberFormat.Format = "#,##0";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                return;

            case decimal number:
                cell.Value = number;
                cell.Style.NumberFormat.Format = "0.0";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                return;

            case double average:
                cell.Value = average;
                cell.Style.NumberFormat.Format = "0.0";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Right);
                return;

            case string date when type == "date" && DateOnly.TryParse(date, out var day):
                cell.Value = day.ToDateTime(new TimeOnly());
                cell.Style.NumberFormat.Format = "dd MMM yyyy";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                return;

            case string stamp when type == "datetime" &&
                DateTime.TryParse(stamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out var moment):
                cell.Value = moment;
                cell.Style.NumberFormat.Format = "dd MMM yyyy hh:mm:ss AM/PM";
                cell.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);
                return;

            default:
                cell.Value = Convert.ToString(value);
                return;
        }
    }

    private static string SheetName(string title)
    {
        var name = new string(title.Where(c => char.IsLetterOrDigit(c) || c is ' ' or '-').ToArray()).Trim();
        if (name.Length <= 31)
        {
            return name;
        }

        var cut = name[..31];
        var lastSpace = cut.LastIndexOf(' ');
        return lastSpace > 12 ? cut[..lastSpace] : cut;
    }

    private static string Slug(string title) =>
        string.Join("-", title.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries).Select(SlugPart));

    private static string SlugPart(string word) =>
        new(word.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c)).ToArray());
}
