using ClosedXML.Excel;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Excel;

internal sealed record PatientImportReportRow(int RowNumber, IReadOnlyDictionary<string, string?> RawData, IReadOnlyList<ImportRowError> Errors);

/// <summary>
/// Builds the downloadable .xlsx error report (GET /api/v1/patients/import/{batchId}/report) —
/// every skipped row's original data plus why it was skipped, in the same column layout as the
/// upload template, so a client can fix the flagged cells and re-upload as a fresh batch.
/// </summary>
internal static class PatientImportReportGenerator
{
    public static byte[] Generate(IReadOnlyList<PatientImportReportRow> rows)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Skipped Rows");

        sheet.Cell(1, 1).Value = "Row #";
        var columns = PatientImportColumns.All;
        for (var i = 0; i < columns.Count; i++)
        {
            sheet.Cell(1, i + 2).Value = columns[i].Header;
        }

        sheet.Cell(1, columns.Count + 2).Value = "Reason(s) Skipped";
        sheet.Row(1).Style.Font.Bold = true;

        for (var r = 0; r < rows.Count; r++)
        {
            var reportRow = rows[r];
            var excelRow = r + 2;

            sheet.Cell(excelRow, 1).Value = reportRow.RowNumber;
            for (var i = 0; i < columns.Count; i++)
            {
                reportRow.RawData.TryGetValue(columns[i].Header, out var value);
                sheet.Cell(excelRow, i + 2).Value = value ?? string.Empty;
            }

            sheet.Cell(excelRow, columns.Count + 2).Value =
                string.Join("; ", reportRow.Errors.Select(e => $"{e.Field}: {e.Message}"));
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, Math.Min(rows.Count + 1, 200));

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
