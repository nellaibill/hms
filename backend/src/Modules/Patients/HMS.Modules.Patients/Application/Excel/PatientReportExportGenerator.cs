using ClosedXML.Excel;
using HMS.Modules.Patients.Contracts;

namespace HMS.Modules.Patients.Application.Excel;

/// <summary>Builds the Patient Reports Excel export (GET .../report/export) — mirrors
/// PatientImportTemplateGenerator's static, pure-formatting style (no DB access in here; the
/// caller resolves department names first, since Patients has no bulk-by-ids lookup on
/// IDepartmentService and the set of distinct departments in an export is small).</summary>
internal static class PatientReportExportGenerator
{
    private static readonly string[] Headers =
    [
        "UHID", "Patient Name", "Age", "Gender", "Phone", "Registration Date", "Department", "Last Visit", "Blood Group", "Marital Status",
    ];

    public static byte[] Generate(IReadOnlyList<PatientReportRowResponse> rows, IReadOnlyDictionary<Guid, string> departmentNames)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Patient Report");

        for (var i = 0; i < Headers.Length; i++)
        {
            var cell = sheet.Cell(1, i + 1);
            cell.Value = Headers[i];
            cell.Style.Font.Bold = true;
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var patient = row.Patient;
            var excelRow = r + 2;

            sheet.Cell(excelRow, 1).Value = patient.Uhid;
            sheet.Cell(excelRow, 2).Value = $"{patient.FirstName} {patient.LastName}";
            sheet.Cell(excelRow, 3).Value = patient.Age;
            sheet.Cell(excelRow, 4).Value = patient.Gender.ToString();
            sheet.Cell(excelRow, 5).Value = patient.PrimaryPhone;
            sheet.Cell(excelRow, 6).Value = patient.CreatedAt.ToString("yyyy-MM-dd");
            sheet.Cell(excelRow, 7).Value = row.LastVisitDepartmentId.HasValue && departmentNames.TryGetValue(row.LastVisitDepartmentId.Value, out var name)
                ? name
                : "—";
            sheet.Cell(excelRow, 8).Value = row.LastVisitAt?.ToString("yyyy-MM-dd") ?? "—";
            sheet.Cell(excelRow, 9).Value = patient.BloodGroup.ToString();
            sheet.Cell(excelRow, 10).Value = patient.MaritalStatus.ToString();
        }

        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
