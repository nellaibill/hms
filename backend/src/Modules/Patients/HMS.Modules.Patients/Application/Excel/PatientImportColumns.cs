namespace HMS.Modules.Patients.Application.Excel;

internal sealed record PatientImportColumn(string Header, bool Required);

/// <summary>
/// Single source of truth for the bulk-import Excel column list — the template generator, the
/// row parser, and the report generator all read from this list, so they can never drift apart
/// from each other. Required flags are kept in sync by hand with
/// CreatePatientRequestValidator's NotEmpty rules and the NOT NULL columns in
/// PatientConfiguration/AddressConfiguration (see the "Required Fields" callout this drives on
/// the template's Instructions sheet).
/// </summary>
internal static class PatientImportColumns
{
    public const string Title = "Title";
    public const string FirstName = "First Name";
    public const string LastName = "Last Name";
    public const string DateOfBirth = "Date Of Birth (YYYY-MM-DD)";
    public const string Gender = "Gender";
    public const string BloodGroup = "Blood Group";
    public const string MaritalStatus = "Marital Status";
    public const string PrimaryPhone = "Primary Phone";
    public const string SecondaryPhone = "Secondary Phone";
    public const string Email = "Email";
    public const string Profession = "Profession";
    public const string IdProofType = "ID Proof Type";
    public const string IdProofNumber = "ID Proof Number";
    public const string ModeOfArrivalSource = "Mode Of Arrival Source";
    public const string ModeOfArrivalChannel = "Mode Of Arrival Channel";
    public const string AddressLine1 = "Address Line 1";
    public const string AddressLine2 = "Address Line 2";
    public const string AddressLine3 = "Address Line 3";
    public const string State = "State";
    public const string District = "District";
    public const string Pincode = "Pincode";
    public const string EmergencyContactName = "Emergency Contact Name";
    public const string EmergencyContactPhone = "Emergency Contact Phone";
    public const string EmergencyContactRelationship = "Emergency Contact Relationship";

    public static readonly IReadOnlyList<PatientImportColumn> All =
    [
        new(Title, true),
        new(FirstName, true),
        new(LastName, true),
        new(DateOfBirth, true),
        new(Gender, true),
        new(BloodGroup, true),
        new(MaritalStatus, true),
        new(PrimaryPhone, true),
        new(SecondaryPhone, false),
        new(Email, false),
        new(Profession, false),
        new(IdProofType, false),
        new(IdProofNumber, false),
        new(ModeOfArrivalSource, true),
        new(ModeOfArrivalChannel, false),
        new(AddressLine1, true),
        new(AddressLine2, false),
        new(AddressLine3, false),
        new(State, true),
        new(District, true),
        new(Pincode, true),
        new(EmergencyContactName, true),
        new(EmergencyContactPhone, true),
        new(EmergencyContactRelationship, true),
    ];
}
