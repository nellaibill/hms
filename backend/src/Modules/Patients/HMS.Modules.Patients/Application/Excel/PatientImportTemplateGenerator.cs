using ClosedXML.Excel;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Excel;

/// <summary>
/// Builds the downloadable .xlsx template for bulk patient import (GET
/// /api/v1/patients/import/template). Required columns (see PatientImportColumns) are visually
/// distinct on the sheet itself — not just documented separately — and enum/lookup columns get
/// a real dropdown, so the client can only enter values the import pipeline actually accepts.
/// This is the direct fix for the free-text/garbage-data problems found analyzing a real
/// legacy export earlier: force valid values at entry time instead of guessing them at import
/// time.
/// </summary>
internal static class PatientImportTemplateGenerator
{
    private static readonly XLColor RequiredHeaderFill = XLColor.FromArgb(255, 235, 235);
    private static readonly XLColor RequiredHeaderFont = XLColor.FromArgb(153, 0, 0);

    public static byte[] Generate(IReadOnlyList<string> stateNames)
    {
        using var workbook = new XLWorkbook();

        var lookups = workbook.Worksheets.Add("Lookups");
        var titleRange = WriteLookupColumn(lookups, 1, "Title", Enum.GetNames<Title>());
        var genderRange = WriteLookupColumn(lookups, 2, "Gender", Enum.GetNames<Gender>());
        var bloodGroupRange = WriteLookupColumn(lookups, 3, "Blood Group", Enum.GetNames<BloodGroup>());
        var maritalStatusRange = WriteLookupColumn(lookups, 4, "Marital Status", Enum.GetNames<MaritalStatus>());
        var idProofTypeRange = WriteLookupColumn(lookups, 5, "ID Proof Type", Enum.GetNames<IdProofType>());
        var modeOfArrivalSourceRange = WriteLookupColumn(lookups, 6, "Mode Of Arrival Source", Enum.GetNames<ModeOfArrivalSource>());
        var relationshipRange = WriteLookupColumn(lookups, 7, "Relationship", Enum.GetNames<Relationship>());
        var statesRange = WriteLookupColumn(lookups, 8, "State", stateNames);
        lookups.Visibility = XLWorksheetVisibility.VeryHidden;

        var sheet = workbook.Worksheets.Add("Patients");
        var columns = PatientImportColumns.All;
        for (var i = 0; i < columns.Count; i++)
        {
            var column = columns[i];
            var cell = sheet.Cell(1, i + 1);
            cell.Value = column.Required ? $"{column.Header} *" : column.Header;
            cell.Style.Font.Bold = true;

            if (column.Required)
            {
                cell.Style.Fill.BackgroundColor = RequiredHeaderFill;
                cell.Style.Font.FontColor = RequiredHeaderFont;
            }

            var lookupRange = column.Header switch
            {
                PatientImportColumns.Title => titleRange,
                PatientImportColumns.Gender => genderRange,
                PatientImportColumns.BloodGroup => bloodGroupRange,
                PatientImportColumns.MaritalStatus => maritalStatusRange,
                PatientImportColumns.IdProofType => idProofTypeRange,
                PatientImportColumns.ModeOfArrivalSource => modeOfArrivalSourceRange,
                PatientImportColumns.EmergencyContactRelationship => relationshipRange,
                PatientImportColumns.State => statesRange,
                _ => null,
            };

            if (lookupRange is not null)
            {
                var columnLetter = XLHelper.GetColumnLetterFromNumber(i + 1);
                var dataRange = sheet.Range($"{columnLetter}2:{columnLetter}2000");
                dataRange.SetDataValidation().List(lookupRange, true);
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Columns().AdjustToContents(1, 1);

        AddInstructionsSheet(workbook, columns);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static IXLRange WriteLookupColumn(IXLWorksheet sheet, int columnIndex, string header, IReadOnlyList<string> values)
    {
        sheet.Cell(1, columnIndex).Value = header;
        for (var i = 0; i < values.Count; i++)
        {
            sheet.Cell(i + 2, columnIndex).Value = values[i];
        }

        var columnLetter = XLHelper.GetColumnLetterFromNumber(columnIndex);
        return sheet.Range($"{columnLetter}2:{columnLetter}{values.Count + 1}");
    }

    private static void AddInstructionsSheet(XLWorkbook workbook, IReadOnlyList<PatientImportColumn> columns)
    {
        var sheet = workbook.Worksheets.Add("Instructions");
        sheet.Position = 1;

        sheet.Cell(1, 1).Value = "Bulk Patient Import — Instructions";
        sheet.Cell(1, 1).Style.Font.Bold = true;
        sheet.Cell(1, 1).Style.Font.FontSize = 14;

        sheet.Cell(3, 1).Value = "1. Fill in the \"Patients\" sheet — one row per patient. Do not change the column headers or their order.";
        sheet.Cell(4, 1).Value = "2. Columns marked with * and shaded red are required — a row missing any of them will be skipped and reported as an error.";
        sheet.Cell(5, 1).Value = "3. Date Of Birth must be in YYYY-MM-DD format (e.g. 1990-05-17).";
        sheet.Cell(6, 1).Value = "4. Title, Gender, Blood Group, Marital Status, ID Proof Type, Mode Of Arrival Source, Emergency Contact Relationship, and State are dropdowns — click the cell and choose from the list rather than typing a value.";
        sheet.Cell(7, 1).Value = "5. District must belong to the selected State, or the row will be skipped.";
        sheet.Cell(8, 1).Value = "6. Every patient needs at least one emergency contact (Name, Phone, Relationship).";
        sheet.Cell(9, 1).Value = "7. UHIDs are assigned automatically on import — do not include one.";
        sheet.Cell(10, 1).Value = "8. After you upload, you'll see a summary of what succeeded and a downloadable report of any skipped rows with the reason for each.";

        sheet.Cell(12, 1).Value = "Required Fields";
        sheet.Cell(12, 1).Style.Font.Bold = true;

        var row = 13;
        foreach (var column in columns.Where(c => c.Required))
        {
            sheet.Cell(row, 1).Value = $"• {column.Header}";
            row++;
        }

        sheet.Column(1).Width = 110;
        sheet.Column(1).Style.Alignment.WrapText = false;
    }
}
