using ClosedXML.Excel;

namespace HMS.Modules.Patients.Application.Excel;

internal sealed record ParsedImportRow(int RowNumber, IReadOnlyDictionary<string, string?> Values);

/// <summary>Thrown when the uploaded file isn't a workbook ClosedXML can open, or is missing
/// the expected "Patients" sheet/header row — distinct from an individual row being invalid,
/// which is a normal outcome recorded per-row instead.</summary>
internal sealed class PatientImportFileException : Exception
{
    public PatientImportFileException(string message) : base(message)
    {
    }
}

/// <summary>
/// Reads the "Patients" sheet of an uploaded import file into raw string values, keyed by the
/// column headers defined in PatientImportColumns (the " *" required-marker suffix on a header
/// is stripped before matching, so the template's own required-column styling round-trips).
/// Values are not yet validated or mapped to domain types here — that's PatientImportRowMapper.
/// </summary>
internal static class PatientImportExcelParser
{
    private const string SheetName = "Patients";

    public static IReadOnlyList<ParsedImportRow> Parse(Stream fileStream)
    {
        IXLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(fileStream);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new PatientImportFileException("The uploaded file isn't a valid .xlsx workbook.");
        }

        using (workbook)
        {
            if (!workbook.TryGetWorksheet(SheetName, out var sheet))
            {
                throw new PatientImportFileException($"The workbook must contain a sheet named \"{SheetName}\".");
            }

            var headerRow = sheet.Row(1);
            var lastColumn = headerRow.LastCellUsed()?.Address.ColumnNumber ?? 0;
            if (lastColumn == 0)
            {
                throw new PatientImportFileException("The \"Patients\" sheet has no header row.");
            }

            var headerByColumn = new Dictionary<int, string>();
            for (var col = 1; col <= lastColumn; col++)
            {
                var header = headerRow.Cell(col).GetString().Trim();
                if (header.EndsWith('*'))
                {
                    header = header[..^1].Trim();
                }

                if (!string.IsNullOrWhiteSpace(header))
                {
                    headerByColumn[col] = header;
                }
            }

            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            var rows = new List<ParsedImportRow>();

            for (var rowNumber = 2; rowNumber <= lastRow; rowNumber++)
            {
                var row = sheet.Row(rowNumber);
                if (row.IsEmpty())
                {
                    continue;
                }

                var values = new Dictionary<string, string?>();
                foreach (var (col, header) in headerByColumn)
                {
                    var raw = row.Cell(col).GetString().Trim();
                    values[header] = string.IsNullOrWhiteSpace(raw) ? null : raw;
                }

                rows.Add(new ParsedImportRow(rowNumber, values));
            }

            return rows;
        }
    }
}
